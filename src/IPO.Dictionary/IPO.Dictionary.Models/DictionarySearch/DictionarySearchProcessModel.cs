using IPO.Dictionary.Models.DictionarySearch;
using System.Collections.Generic;
using System.IO;

namespace IPO.Dictionary.Models
{
    public class DictionarySearchProcessModel
    {
        public DictionarySearchProcessModel(DictionarySearchFileType fileType, Stream data, IEnumerable<DictionaryValue> dictionaryData)
        {
            FileType = fileType;
            Data = data;
            DictionaryData = dictionaryData;
        }

        public DictionarySearchFileType FileType { get; }
        public Stream Data { get; }
        public IEnumerable<DictionaryValue> DictionaryData { get; }
    }
}