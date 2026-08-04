using IPO.Dictionary.Models.API;
using IPO.Dictionary.Models.DictionarySearch; 
using Microsoft.AspNetCore.Http;

namespace IPO.Dictionary.Interfaces
{
    public interface IDictionarySearchManagementService
    {
        Task<SearchResult> GetDictionaryResultsAsync(int id);
        Task<SearchDictionaryResult> SearchDictionaryAsync(IFormFile file, DictionaryType dictionaryType, int? maximumFileSizeHeader);
        Task ProcessDictionarySearchMessageAsync(DictionarySearchBusMessage searchMessage);
    }
}