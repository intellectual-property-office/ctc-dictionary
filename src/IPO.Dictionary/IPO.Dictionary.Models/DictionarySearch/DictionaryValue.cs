using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPO.Dictionary.Models.DictionarySearch
{
    public class DictionaryValue
    {
        public DictionaryValue(string value, DictionaryValueType type)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Type = type;
        }

        public string Value { get; set; }   
        public DictionaryValueType Type { get; set; }
    }
}
