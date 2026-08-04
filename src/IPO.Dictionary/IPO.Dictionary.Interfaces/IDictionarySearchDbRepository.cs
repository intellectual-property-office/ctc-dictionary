using IPO.Dictionary.Models;
using IPO.Dictionary.Models.DictionarySearch;

namespace IPO.Dictionary.Interfaces
{
    public interface IDictionarySearchDbRepository
    {
        Task<int> CreateDictionarySearchRecordAsync(string blobName, DictionarySearchFileType dictionarySearchFileType, DictionaryType dictionaryType);
        Task<DictionarySearchData?> GetDictionarySearchDataAsync(int fileId);
        Task UpdateDictionarySearchStatusAsync(int fileId, Status status);
        Task<string?> GetDictionaryDataAsync(DictionaryType dictionaryType);
        Task UpdateDictionarySearchProcessResultsAsync(int fileId, ProcessResults processResults);
        Task DeleteDictionarySearchRecordAsync(int id); 
        void SeedDictionarySearchData(DirectoryInfo directory);
        DirectoryInfo GetDictionarySeedDataDirectory(string solutionPath);
    }
}