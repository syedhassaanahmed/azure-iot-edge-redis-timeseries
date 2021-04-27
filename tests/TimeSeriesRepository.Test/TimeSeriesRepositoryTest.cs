using Moq;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;
using Xunit;

namespace TimeSeriesRepository.Test
{
    public class TimeSeriesRepositoryTest
    {
        [Fact]
        public void Constructor_NullRedisInstance_ThrowsArgumentNullException()
        {
            // Arrange, Act and Assert
            Assert.Throws<ArgumentNullException>(() => new RedisTimeSeriesEdge.TimeSeriesRepository(redis: null, null, null));
        }

        [Fact]
        public void Constructor_NonNullRedisInstance_DoesNotThrowException()
        {
            // Arrange
            var mockMultiplexer = new Mock<IConnectionMultiplexer>();

            // Act and Assert
            new RedisTimeSeriesEdge.TimeSeriesRepository(redis: mockMultiplexer.Object, null, null);
        }

        [Fact]
        public async Task CreateTimeSeriesIfNotExistsAsync_ChecksIfTimeSeriesExists()
        {
            // Arrange
            var mockMultiplexer = new Mock<IConnectionMultiplexer>();

            var mockDatabase = new Mock<IDatabase>();
            mockDatabase.Setup(x => x.KeyExistsAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<CommandFlags>()
            ));

            mockMultiplexer.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(mockDatabase.Object);

            // Act
            var repository = new RedisTimeSeriesEdge.TimeSeriesRepository(redis: mockMultiplexer.Object, null, null);
            await repository.CreateTimeSeriesIfNotExistsAsync();

            // Assert
            mockDatabase.VerifyAll();
        }

        [Fact]
        public async Task GetTimeSeriesInfoAsync_CallsExecuteAsyncWithTsInfo()
        {
            // Arrange
            var mockMultiplexer = new Mock<IConnectionMultiplexer>();

            var mockDatabase = new Mock<IDatabase>();

            var emptyRedisResult = RedisResult.Create(new RedisValue[0]);
            mockDatabase.Setup(x => x.ExecuteAsync(
                It.Is<string>(y => y.Equals("TS.INFO", StringComparison.OrdinalIgnoreCase)),
                It.IsAny<object[]>()
            )).Returns(Task.FromResult(emptyRedisResult));

            mockMultiplexer.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(mockDatabase.Object);

            // Act
            var repository = new RedisTimeSeriesEdge.TimeSeriesRepository(redis: mockMultiplexer.Object, null, null);
            await repository.GetTimeSeriesInfoAsync();

            // Assert
            mockDatabase.VerifyAll();
        }
    }
}
