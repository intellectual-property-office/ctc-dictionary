using Aspose.Words;
using Aspose.Words.Loading;
using IPO.Dictionary.Interfaces;
using IPO.Dictionary.Models;
using IPO.Dictionary.Models.DictionarySearch;
using IPO.Dictionary.Models.DictionarySearch.Processing;
using IPO.Dictionary.Services.DictionarySearch.Processing;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IPO.Dictionary.Services
{
    public class OdtProcessor : IDocumentProcessor
    { 
        public DictionarySearchFileType DictionarySearchFileType => DictionarySearchFileType.Odt;

        public ProcessResults SearchDocument(DictionarySearchProcessModel processModel)
        {
            var result = TryLoadFile(processModel.Data, LoadFormat.Odt, out var document);

            if (result != null)
                return ProcessResults.CreateFailedProcessResultsModel(result.Value);

            foreach (var node in document.GetChildNodes(NodeType.Paragraph, true))
            {
                var matchResult = SearchNodeForMatch(node, processModel.DictionaryData);

                if (matchResult.HasMatch)
                {
                    return ProcessResults.CreateSuccesfulProcessResultsModel(true, matchResult.Match!);
                }
            }

            return ProcessResults.CreateSuccesfulProcessResultsModel(false);
        }

        private static MatchingResult SearchNodeForMatch(Node node, IEnumerable<DictionaryValue> dictionaryData)
        {
            var nodeText = node.GetText();
            var match = dictionaryData.FirstOrDefault(word => TextFunctions.TextContainsValue(nodeText, word));

            if (match != null)
            {
                return MatchingResult.CreateMatchResultWithMatch(match.Value);
            } 

            return MatchingResult.CreateMatchResultWithoutMatch();
        }

        private static ErrorType? TryLoadFile(Stream data, LoadFormat loadFormat, out Document document)
        {
            document = null!;
            try
            {
                _ = FileFormatUtil.DetectFileFormat(data);

                document = new Document(data, loadOptions: new LoadOptions() { LoadFormat = loadFormat });
                return null;
            }
            catch (IncorrectPasswordException)
            {
                return ErrorType.FileEncrypted;
            }
            catch
            { 
                return ErrorType.FileCannotBeLoaded;
            }
        }
    }
}