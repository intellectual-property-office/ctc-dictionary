using IPO.Dictionary.Models.DictionarySearch;
using Microsoft.AspNetCore.Http;

namespace IPO.Dictionary.BDDTests.DictionarySearch.Data
{
    public class DictionarySearchRequestModel
    {
        public DictionarySearchRequestModel(IFormFile file, DictionaryType dictionaryType)
        {
            File = file;
            DictionaryType = dictionaryType;
        }

        public IFormFile File { get; private set; }

        public DictionaryType DictionaryType { get; private set; }
    }
}
