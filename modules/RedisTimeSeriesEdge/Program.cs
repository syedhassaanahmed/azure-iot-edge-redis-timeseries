using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Newtonsoft.Json;
using NRedisTimeSeries;
using NRedisTimeSeries.DataTypes;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        static readonly string[] TimeSeriesKeys = { "simulated_temperature", "simulated_pressure", "simulated_humidity" };

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
            Console.WriteLine($"Redis Connection Status: {Redis.GetStatus()}");

            var deviceId = Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");
            var labels = !string.IsNullOrWhiteSpace(deviceId) ? 
                new List<TimeSeriesLabel> { new TimeSeriesLabel("deviceId", deviceId) } : null;

            var db = Redis.GetDatabase();
            var tasks = TimeSeriesKeys.Select(x => CreateTimeSeriesIfNotExistsAsync(db, x, labels));
            await Task.WhenAll(tasks);
        }

        static async Task CreateTimeSeriesIfNotExistsAsync(IDatabase db, string timeSeriesName, List<TimeSeriesLabel> labels)
        {            
            var timeSeriesExists = await db.KeyExistsAsync(timeSeriesName);

            if (!timeSeriesExists)
            {
                await db.TimeSeriesCreateAsync(timeSeriesName, labels: labels);
                Console.WriteLine($"TimeSeries {timeSeriesName} created.");
            }
            else
            {
                Console.WriteLine($"TimeSeries {timeSeriesName} already exists.");
            }
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
                throw new InvalidOperationException($"{nameof(userContext)} doesn't contain expected value.");
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
                }
                pipeMessage.Properties.Add("storedInRedisTimeSeries", "true");
                await moduleClient.SendEventAsync("output1", pipeMessage);
            }

            sw.Stop();
            Console.WriteLine($"Message processing took {sw.Elapsed.TotalMilliseconds}ms");

            return MessageResponse.Completed;
        }

        static async Task InsertTimeSeriesAsync(string messageString)
        {
            var messageBody = JsonConvert.DeserializeObject<MessageBody>(messageString);

            var sequence = new List<(string, TimeStamp, double)>(TimeSeriesKeys.Length);
            sequence.Add((TimeSeriesKeys[0], messageBody.TimeCreated, messageBody.Machine.Temperature));
            sequence.Add((TimeSeriesKeys[1], messageBody.TimeCreated, messageBody.Machine.Pressure));
            sequence.Add((TimeSeriesKeys[2], messageBody.TimeCreated, messageBody.Ambient.Humidity));

            await Redis.GetDatabase().TimeSeriesMAddAsync(sequence);

            var webUtcTime = messageBody.TimeCreated.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
            Console.WriteLine($"Message with timestamp {webUtcTime} added to TimeSeries.");
        }

        static async Task<MethodResponse> GetTimeSeriesInfo(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"Invoking direct method {nameof(GetTimeSeriesInfo)}.");
            try{
            var db = Redis.GetDatabase();
            var tasks = TimeSeriesKeys.Select(async x => await db.TimeSeriesInfoAsync(x));
            var serializedResult = JsonConvert.SerializeObject(await Task.WhenAll(tasks));

            Console.WriteLine($"serializedResult: {serializedResult}");
            } catch (Exception ex) {Console.WriteLine(ex);}
            return new MethodResponse(Encoding.UTF8.GetBytes("serializedResult"), 200);
        }
    }
}
