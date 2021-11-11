using Microsoft.Extensions.Logging;
using NRedisTimeSeries;
using NRedisTimeSeries.DataTypes;
using StackExchange.Redis;

namespace RedisTimeSeriesEdge
{
    public class TimeSeriesRepository
    {
        readonly IConnectionMultiplexer Redis;
        readonly ILogger? Log;
        readonly string? DeviceId;

        static readonly string[] TimeSeriesKeys = { "simulated_temperature", "simulated_pressure", "simulated_humidity" };

        public TimeSeriesRepository(IConnectionMultiplexer redis, ILogger? log, string? deviceId)
        {
            if (redis == null)
            {
                throw new ArgumentNullException(nameof(redis));
            }

            Redis = redis;
            Log = log;
            DeviceId = deviceId;
        }

        public async Task CreateTimeSeriesIfNotExistsAsync()
        {
            var labels = !string.IsNullOrWhiteSpace(DeviceId) ?
                new List<TimeSeriesLabel> { new TimeSeriesLabel("deviceId", DeviceId) } : null;

            var db = Redis.GetDatabase();
            var tasks = TimeSeriesKeys.Select(x => CreateTimeSeriesIfNotExistsAsync(db, x, labels));
            await Task.WhenAll(tasks);
        }

        async Task CreateTimeSeriesIfNotExistsAsync(IDatabase db, string timeSeriesName, List<TimeSeriesLabel>? labels)
        {
            var timeSeriesExists = await db.KeyExistsAsync(timeSeriesName);

            if (!timeSeriesExists)
            {
                await db.TimeSeriesCreateAsync(timeSeriesName, labels: labels);
                Log?.LogInformation($"TimeSeries {timeSeriesName} created.");
            }
            else
            {
                Log?.LogInformation($"TimeSeries {timeSeriesName} already exists.");
            }
        }

        public async Task InsertTimeSeriesAsync(DateTimeOffset timeCreated, double temperature, double pressure, double humidity)
        {
            var unixTimestamp = timeCreated.ToUnixTimeMilliseconds();

            var sequence = new List<(string, TimeStamp, double)>(TimeSeriesKeys.Length);
            sequence.Add((TimeSeriesKeys[0], unixTimestamp, temperature));
            sequence.Add((TimeSeriesKeys[1], unixTimestamp, pressure));
            sequence.Add((TimeSeriesKeys[2], unixTimestamp, humidity));

            await Redis.GetDatabase().TimeSeriesMAddAsync(sequence);

            var webUtcTime = timeCreated.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
            Log?.LogDebug($"Message with timestamp {webUtcTime} added to TimeSeries.");
        }

        public async Task<IDictionary<string, string>[]> GetTimeSeriesInfoAsync()
        {
            var db = Redis.GetDatabase();
            var tasks = TimeSeriesKeys.Select(async x => ToDictionary(x, await db.ExecuteAsync("TS.INFO", x)));
            return await Task.WhenAll(tasks);
        }

        IDictionary<string, string> ToDictionary(string timeSeriesName, RedisResult redisResult)
        {
            Log?.LogInformation($"Info obtained for TimeSeries: {timeSeriesName}");

            var redisResults = (RedisResult[])redisResult;

            var keyValueResult = new Dictionary<string, string> { { "timeSeriesName", timeSeriesName } };
            for (var i = 0; i < redisResults.Length; i += 2)
            {
                keyValueResult.Add(redisResults[i].ToString()!, redisResults[i + 1].ToString()!);
            }

            return keyValueResult;
        }

        public async Task CloseAsync()
        {
            await Redis.CloseAsync();
        }
    }
}