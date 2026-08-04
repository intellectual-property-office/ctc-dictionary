using IPO.Dictionary.Models;
using IPO.Dictionary.Models.DictionarySearch;

namespace IPO.Dictionary.Interfaces
{
    public interface IDictionarySearchProcessor
    {
        IEnumerable<DictionaryValue> GetDictionaryValues(string dictionaryData);
        ProcessResults SearchDocument(DictionarySearchProcessModel processModel);
    }
}