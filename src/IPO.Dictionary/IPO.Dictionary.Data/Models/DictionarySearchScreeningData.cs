using IPO.Dictionary.Models.DictionarySearch;

namespace IPO.Dictionary.Data.Models
{
    public class DictionarySearchScreeningData
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? DictionaryData { get; set; }
        public DictionaryType DictionaryType { get; set; }
    }
}
