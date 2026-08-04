using IPO.Dictionary.Models;
using IPO.Dictionary.Models.DictionarySearch;

namespace IPO.Dictionary.Interfaces
{
    public interface IDocumentProcessor
    {
        DictionarySearchFileType DictionarySearchFileType { get; }
        ProcessResults SearchDocument(DictionarySearchProcessModel processModel);
    }
}