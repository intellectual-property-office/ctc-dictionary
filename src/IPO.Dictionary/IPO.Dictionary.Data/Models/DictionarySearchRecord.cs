using IPO.Dictionary.Models.DictionarySearch;
using System;

namespace IPO.Dictionary.Data.Models
{
    public class DictionarySearchRecord
    {
        public int Id { get; set; }
        public DateTime CreatedOn { get; set; }
        public DictionarySearchFileType FileType { get; set; }
        public DictionaryType DictionaryType { get; set; }
        public Status Status { get; set; }

        public string? BlobName { get; set; }

        public string? Match { get; set; }
    }
}
