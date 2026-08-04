using Azure.Messaging.ServiceBus;
using IPO.Dictionary.Interfaces;
using IPO.Dictionary.Models.DictionarySearch; 
using System.Text.Json;
using System.Threading.Tasks;

namespace IPO.Dictionary.Gateways
{
    public class DictionarySearchTopicGateway : IDictionarySearchTopicGateway
    {
        private readonly ServiceBusSender _serviceBusSender;

        public DictionarySearchTopicGateway(ServiceBusSender serviceBusSender)
        {
            this._serviceBusSender = serviceBusSender;
        }
        public async Task SendMessageToSearchAsync(int fileId)
        {
            await SendMessageAsync(new DictionarySearchBusMessage(fileId));
        }

        protected virtual async Task SendMessageAsync<T>(T obj) where T : DictionarySearchBusMessage
        {
            var payload = JsonSerializer.Serialize(obj);
            var message = new ServiceBusMessage(payload);
            message.ApplicationProperties.Add("messageType", obj.Type);
            await _serviceBusSender.SendMessageAsync(message);
        }
    }
}
