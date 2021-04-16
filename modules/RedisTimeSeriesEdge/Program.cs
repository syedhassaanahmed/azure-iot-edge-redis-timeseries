using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTimeSeriesEdge
{
    class Program
    {
        static ConnectionMultiplexer _redis;

        static ConnectionMultiplexer Redis => _redis ??= ConnectionMultiplexer.Connect(
            Environment.GetEnvironmentVariable("REDIS_HOST_NAME"));

        const string TimeSeriesName = "SimulatedTemperatureSensor";

        static void Main(string[] args)
        {
            Init().Wait();

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            WhenCancelled(cts.Token).Wait();
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => 
            {
                Redis.Close();
                ((TaskCompletionSource<bool>)s).SetResult(true);
            }, tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task Init()
        {
            var mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };

            // Open a connection to the Edge runtime
            var moduleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await moduleClient.OpenAsync();
            Console.WriteLine("IoT Hub module client initialized.");

            await CreateTimeSeriesIfNotExistsAsync();

            // Register callback to be called when a message is received by the module
            await moduleClient.SetInputMessageHandlerAsync("input1", PipeMessage, moduleClient);

            await moduleClient.SetMethodDefaultHandlerAsync(GetTimeSeriesInfo, moduleClient);
        }

        static async Task CreateTimeSeriesIfNotExistsAsync()
        {
            var timeSeriesExists = await Redis.GetDatabase().KeyExistsAsync(TimeSeriesName);

            if (timeSeriesExists)
            {
                Console.WriteLine($"TimeSeries {TimeSeriesName} already exists.");
            }
            else
            {
                var iotEdgedeviceId = Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");
                var deviceId = !string.IsNullOrWhiteSpace(iotEdgedeviceId) ? iotEdgedeviceId : "unknown";

                var createResult = await Redis.GetDatabase().ExecuteAsync("TS.CREATE", TimeSeriesName, "LABELS", "deviceId", deviceId);
                Console.WriteLine($"TimeSeries {TimeSeriesName} created: {createResult}");
            }
        }

        /// <summary>
        /// This method is called whenever the module is sent a message from the EdgeHub. 
        /// It just pipe the messages without any change.
        /// It prints all the incoming messages.
        /// </summary>
        static async Task<MessageResponse> PipeMessage(Message message, object userContext)
        {
            if (!(userContext is ModuleClient moduleClient))
            {
                throw new InvalidOperationException("UserContext doesn't contain " + "expected values");
            }

            var messageBytes = message.GetBytes();
            var messageString = Encoding.UTF8.GetString(messageBytes);

            if (!string.IsNullOrEmpty(messageString))
            {
                await InsertTimeSeriesAsync(messageString);

                using var pipeMessage = new Message(messageBytes);
                foreach (var prop in message.Properties)
                {
                    pipeMessage.Properties.Add(prop.Key, prop.Value);
                    pipeMessage.Properties.Add("storedInRedisTimeSeries", "true");
                }
                await moduleClient.SendEventAsync("output1", pipeMessage);

                Console.WriteLine("Received message sent");
            }

            return MessageResponse.Completed;
        }

        static async Task InsertTimeSeriesAsync(string messageString)
        {
            var messageBody = JsonConvert.DeserializeObject<MessageBody>(messageString);

            var result = await Redis.GetDatabase().ExecuteAsync("TS.ADD", TimeSeriesName, 
                messageBody.TimeCreated.ToUnixTimeMilliseconds(), messageBody.Machine.Temperature);
            
            Console.WriteLine($"Added message to TimeSeries {TimeSeriesName}, redisResult: {result}");
        }

        static async Task<MethodResponse> GetTimeSeriesInfo(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"Invoking direct method {nameof(GetTimeSeriesInfo)}.");
            
            var result = (RedisResult[]) await Redis.GetDatabase().ExecuteAsync("TS.INFO", TimeSeriesName);
            var serializedResult = JsonConvert.SerializeObject(ToDictionary(result));

            return new MethodResponse(Encoding.UTF8.GetBytes(serializedResult), 200);
        }

        static Dictionary<string, string> ToDictionary(RedisResult[] redisResult)
        {
            var keyValueResult = new Dictionary<string, string>();

            for (int i = 0; i < redisResult.Length; i += 2)
            {
                keyValueResult.Add(redisResult[i].ToString(), redisResult[i + 1].ToString());
            }

            return keyValueResult;
        }
    }
}
