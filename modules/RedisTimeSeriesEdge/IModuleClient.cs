using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;

namespace RedisTimeSeriesEdge
{
    public interface IModuleClient
    {
        Task OpenAsync();
        Task SendEventAsync(string outputName, Message message);
        Task SetInputMessageHandlerAsync(string inputName, MessageHandler messageHandler);
        Task SetMethodDefaultHandlerAsync(MethodCallback methodHandler);
        Task CloseAsync();
    }
}