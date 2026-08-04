using IPO.Dictionary.Interfaces;
using System.Threading.Tasks;

namespace IPO.Dictionary.BDDTests.DictionarySearch
{
    internal class MockedDictionarySearchTopicGateway : IDictionarySearchTopicGateway
    {
        public Task SendMessageToSearchAsync(int fileId)
        {
            return Task.CompletedTask;
        }
    }
}