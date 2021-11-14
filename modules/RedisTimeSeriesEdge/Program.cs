using IoTEdgeLogger;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Runtime.Loader;
using System.Text;

namespace RedisTimeSeriesEdge
{
    class Program
    {
        const string DefaultLogLevel = "debug";
        const string RedisAddress = "REDIS_ADDRESS";

        static ILogger? Log;
        static ModuleClient? ModuleClient;
        static TimeSeriesRepository? Repository;

        static async Task Main()
        {
            Logger.SetLogLevel(DefaultLogLevel);
            Log = Logger.Factory.CreateLogger<string>();

            await InitAsync();

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            await WhenCancelled(cts.Token);

            await Repository!.CloseAsync();
            await ModuleClient!.CloseAsync();
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s =>
            {
                (s as TaskCompletionSource<bool>)!.SetResult(true);
            }, tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task InitAsync()
        {
            await InitModuleClientAsync();
            await InitTimeSeriesRepositoryAsync();

            await ModuleClient!.SetInputMessageHandlerAsync("input1", PipeMessage, ModuleClient);
            await ModuleClient!.SetMethodDefaultHandlerAsync(GetTimeSeriesInfo, ModuleClient);
        }

        static async Task InitModuleClientAsync()
        {
            var mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };

            ModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);

            await ModuleClient.OpenAsync();
            Log!.LogInformation("IoT Hub module client initialized.");
        }

        static async Task InitTimeSeriesRepositoryAsync()
        {
            var redisAddress = Environment.GetEnvironmentVariable(RedisAddress);
            if (string.IsNullOrWhiteSpace(redisAddress))
            {
                throw new ArgumentNullException(RedisAddress);
            }

            var redisConfig = new ConfigurationOptions();

            if (redisAddress.EndsWith(".sock", true, CultureInfo.InvariantCulture))
            {
                if (!File.Exists(redisAddress))
                {
                    throw new ArgumentException($"File {redisAddress} not found.");
                }

                redisConfig.EndPoints.Add(new UnixDomainSocketEndPoint(redisAddress));
            }
            else
            {
                redisConfig.EndPoints.Add(redisAddress);
            }

            var redis = ConnectionMultiplexer.Connect(redisConfig);
            Log!.LogInformation($"Redis Connection Status: {redis.GetStatus()}");

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

            if (userContext is not ModuleClient moduleClient)
            {
                throw new InvalidOperationException($"{nameof(userContext)} doesn't contain expected value.");
            }

            var messageBytes = message.GetBytes();
            var messageString = Encoding.UTF8.GetString(messageBytes);
            if (!string.IsNullOrEmpty(messageString))
            {
                var messageBody = JsonConvert.DeserializeObject<MessageBody>(messageString);
                await Repository!.InsertTimeSeriesAsync(messageBody.TimeCreated,
                    messageBody.Machine!.Temperature,
                    messageBody.Machine!.Pressure,
                    messageBody.Ambient!.Humidity);

                using var pipeMessage = new Message(messageBytes);
                foreach (var prop in message.Properties)
                {
                    pipeMessage.Properties.Add(prop.Key, prop.Value);
                }
                pipeMessage.Properties.Add("storedInRedisTimeSeries", "true");
                await moduleClient.SendEventAsync("output1", pipeMessage);
            }

            sw.Stop();
            Log!.LogDebug($"Message processing took {sw.Elapsed.TotalMilliseconds}ms");

            return MessageResponse.Completed;
        }

        static async Task<MethodResponse> GetTimeSeriesInfo(MethodRequest methodRequest, object userContext)
        {
            Log!.LogInformation($"Invoking direct method {nameof(GetTimeSeriesInfo)}.");

            var timeSeriesInfo = await Repository!.GetTimeSeriesInfoAsync();
            var serializedResult = JsonConvert.SerializeObject(timeSeriesInfo);

            return new MethodResponse(Encoding.UTF8.GetBytes(serializedResult), 200);
        }
    }
}
