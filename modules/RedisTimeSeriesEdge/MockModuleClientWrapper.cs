using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Extensions.Logging;

namespace RedisTimeSeriesEdge
{
    public class MockModuleClientWrapper : IModuleClient
    {
        readonly ILogger Log;

        public MockModuleClientWrapper(ILogger log)
        {
            Log = log;
        }

        public Task OpenAsync()
        {
            Log.LogInformation("Opened ModuleClient");
            return Task.CompletedTask;
        }

        public Task SendEventAsync(string outputName, Message message)
        {
            Log.LogInformation($"Message Sent to {outputName}");
            return Task.CompletedTask;
        }

        public Task SetInputMessageHandlerAsync(string inputName, MessageHandler messageHandler)
        {
            Log.LogInformation($"Message Handler Set for {inputName}");
            return Task.CompletedTask;
        }

        public Task SetMethodDefaultHandlerAsync(MethodCallback methodHandler)
        {
            Log.LogInformation("Method Default Handler Set");
            return Task.CompletedTask;
        }

        public Task CloseAsync()
        {
            Log.LogInformation("Closed ModuleClient");
            return Task.CompletedTask;
        }
    }
}