using IPO.Dictionary.Models.DictionarySearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPO.Dictionary.Data.Models
{
    public class DictionarySearchScreeningDataSeedingHistory
    {
        public int Id { get; set; }
        public int Version { get; set; }
        public DateTime CreatedOn { get; set; }
        public DictionaryType Type { get; set; }
    }
}
