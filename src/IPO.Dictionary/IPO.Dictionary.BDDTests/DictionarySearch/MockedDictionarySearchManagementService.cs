using AutoFixture;
using IPO.Dictionary.Models.API;
using IPO.Dictionary.Models.DictionarySearch; 
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace IPO.Dictionary.BDDTests.DictionarySearch
{
    internal class MockedDictionarySearchManagementService
    {
        public Task<SearchResult> GetDictionaryResults(int id)
        {
            return Task.FromResult(new Fixture().Build<SearchResult>().With(o=>o.ResultsId , id).Create());
        }

        public Task ProcessDictionarySearchMessageAsync(DictionarySearchBusMessage searchMessage)
        {
            throw new System.NotImplementedException();
        }

        public Task<SearchDictionaryResult> SearchDictionary(IFormFile file, DictionaryType dictionaryType, int? maximumFileSizeHeader)
        {
            throw new System.NotImplementedException();
        }
    }
}