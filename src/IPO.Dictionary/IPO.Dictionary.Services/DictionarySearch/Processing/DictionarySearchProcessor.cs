using IPO.Dictionary.Interfaces;
using IPO.Dictionary.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using IPO.Dictionary.Models.DictionarySearch;

namespace IPO.Dictionary.Services
{
    public class DictionarySearchProcessor : IDictionarySearchProcessor
    {
        private readonly IEnumerable<IDocumentProcessor> _documentProcessors;

        public DictionarySearchProcessor(IEnumerable<IDocumentProcessor> documentProcessors)
        {
            _documentProcessors = documentProcessors;
        }

        public ProcessResults SearchDocument(DictionarySearchProcessModel processModel)
        {
             return this._documentProcessors.First(o=>o.DictionarySearchFileType == processModel.FileType).SearchDocument(processModel);
        }

        public IEnumerable<DictionaryValue> GetDictionaryValues(string dictionaryData)
        {  

            if (string.IsNullOrWhiteSpace(dictionaryData))
                return Array.Empty<DictionaryValue>();
             
            dictionaryData = dictionaryData.Trim(); 

            if (dictionaryData.StartsWith("[") && dictionaryData.EndsWith("]"))
            {
                return JsonSerializer.Deserialize<string[]>(dictionaryData)!.Distinct().Select(o => GetDictionaryValue(o));
            }
            else
            { 
                return dictionaryData.Split(',').Distinct().Select(o => GetDictionaryValue(o));
            }  
        }

        private DictionaryValue GetDictionaryValue(string value)
        {
            value = value.Trim();

            if (value.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Length > 1)
                return new DictionaryValue(value, DictionaryValueType.Phrase);

            return new DictionaryValue(value, DictionaryValueType.Word);
        }
    }
}