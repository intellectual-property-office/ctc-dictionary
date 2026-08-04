using IPO.Dictionary.Interfaces;
using IPO.Dictionary.Models;
using IPO.Dictionary.Models.DictionarySearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace IPO.Dictionary.BDDTests.DictionarySearch
{
    public class MockedDictionarySearchProcessor : IDictionarySearchProcessor
    {
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

        public ProcessResults SearchDocument(DictionarySearchProcessModel processModel)
        {
            return ProcessResults.CreateSuccesfulProcessResultsModel(true);
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