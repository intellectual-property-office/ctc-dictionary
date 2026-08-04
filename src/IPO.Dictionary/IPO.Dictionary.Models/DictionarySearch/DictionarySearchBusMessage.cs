namespace IPO.Dictionary.Models.DictionarySearch
{
    public class DictionarySearchBusMessage
    {
        public DictionarySearchBusMessage(int fileId)
        {
            FileId = fileId;
            Type = messageType;
        }

        public int FileId { get; set; }

        public string Type { get; set; }

        public static readonly string messageType = "SearchDocument";
    }
}
