using Microsoft.Azure.Devices.Client;
using System.Threading.Tasks;

namespace RedisTimeSeriesEdge
{
    public class ModuleClientWrapper : IModuleClient
    {
        readonly ModuleClient ModuleClient;

        public ModuleClientWrapper(ModuleClient moduleClient)
        {
            ModuleClient = moduleClient;
        }

        public async Task OpenAsync()
        {
            await ModuleClient.OpenAsync();
        }

        public async Task SendEventAsync(string outputName, Message message)
        {
            await ModuleClient.SendEventAsync(outputName, message);
        }

        public async Task SetInputMessageHandlerAsync(string inputName, MessageHandler messageHandler)
        {
            await ModuleClient.SetInputMessageHandlerAsync(inputName, messageHandler, ModuleClient);
        }

        public async Task SetMethodDefaultHandlerAsync(MethodCallback methodHandler)
        {
            await ModuleClient.SetMethodDefaultHandlerAsync(methodHandler, ModuleClient);
        }

        public async Task CloseAsync()
        {
            await ModuleClient.CloseAsync();
        }
    }
}