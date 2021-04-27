using IoTEdgeLogger;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Diagnostics;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RedisTimeSeriesEdge
{
    class Program
    {
        const string RedisHostNameKey = "REDIS_HOST_NAME";

        static ILogger Log;

        static TimeSeriesRepository Repository;

        static void Main(string[] args)
        {
            Logger.SetLogLevel("debug");
            Log = Logger.Factory.CreateLogger<string>();

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
                Repository?.Close();
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
            Log.LogInformation("IoT Hub module client initialized.");

            await InitTimeSeriesRepositoryAsync();

            // Register callback to be called when a message is received by the module
            await moduleClient.SetInputMessageHandlerAsync("input1", PipeMessage, moduleClient);

            await moduleClient.SetMethodDefaultHandlerAsync(GetTimeSeriesInfo, moduleClient);
        }

        static async Task InitTimeSeriesRepositoryAsync()
        {
            var redisHostName = Environment.GetEnvironmentVariable(RedisHostNameKey);
            if (string.IsNullOrWhiteSpace(redisHostName))
            {
                throw new ArgumentException($"{RedisHostNameKey} not configured.");
            }

            var redis = ConnectionMultiplexer.Connect(redisHostName);
            Log.LogInformation($"Redis Connection Status: {redis.GetStatus()}");

            var iotEdgeDeviceId = Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");
            Repository = new TimeSeriesRepository(redis, Log, iotEdgeDeviceId);
            await Repository.CreateTimeSeriesIfNotExistsAsync();
        }

        /// <summary>
        /// This method is called whenever the module is sent a message from the EdgeHub. 
        /// It just pipe the messages without any change.
        /// It prints all the incoming messages.
        /// </summary>
        static async Task<MessageResponse> PipeMessage(Message message, object userContext)
        {
            var sw = new Stopwatch();
            sw.Start();

            if (!(userContext is ModuleClient moduleClient))
            {
                var errorMessage = $"{nameof(userContext)} doesn't contain expected value.";
                Log.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            var messageBytes = message.GetBytes();
            var messageString = Encoding.UTF8.GetString(messageBytes);
            if (!string.IsNullOrEmpty(messageString))
            {
                var messageBody = JsonConvert.DeserializeObject<MessageBody>(messageString);
                await Repository.InsertTimeSeriesAsync(messageBody.TimeCreated, 
                    messageBody.Machine.Temperature,
                    messageBody.Machine.Pressure,
                    messageBody.Ambient.Humidity);

                using var pipeMessage = new Message(messageBytes);
                foreach (var prop in message.Properties)
                {
                    pipeMessage.Properties.Add(prop.Key, prop.Value);                    
                }
                pipeMessage.Properties.Add("storedInRedisTimeSeries", "true");
                await moduleClient.SendEventAsync("output1", pipeMessage);
            }

            sw.Stop();
            Log.LogDebug($"Message processing took {sw.Elapsed.TotalMilliseconds}ms");

            return MessageResponse.Completed;
        }

        static async Task<MethodResponse> GetTimeSeriesInfo(MethodRequest methodRequest, object userContext)
        {
            Log.LogInformation($"Invoking direct method {nameof(GetTimeSeriesInfo)}.");

            var timeSeriesInfo = await Repository.GetTimeSeriesInfoAsync();
            var serializedResult = JsonConvert.SerializeObject(timeSeriesInfo);

            return new MethodResponse(Encoding.UTF8.GetBytes(serializedResult), 200);
        }
    }
}
