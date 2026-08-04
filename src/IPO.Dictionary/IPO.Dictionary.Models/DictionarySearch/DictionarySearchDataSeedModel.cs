using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPO.Dictionary.Models.DictionarySearch
{
    public class DictionarySearchDataSeedModel
    {
        public DictionarySearchDataSeedModel(int version, DictionaryType type, string path)
        {
            Version = version;
            Type = type;
            Path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public int Version { get; set; }
        public DictionaryType Type { get; set; }
        public string Path { get; set; }    
    }
}
