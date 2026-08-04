using IPO.Dictionary.Interfaces;
using IPO.Dictionary.Models;
using IPO.Dictionary.Models.Configuration;
using IPO.Dictionary.Models.DictionarySearch;
using IPO.Dictionary.Models.DictionarySearch.Processing;
using IPO.Dictionary.Services.DictionarySearch.Processing;
using Spire.Pdf;
using Spire.Pdf.Texts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IPO.Dictionary.Services
{
    public class PdfProcessor : IDocumentProcessor
    {
        public DictionarySearchFileType DictionarySearchFileType => DictionarySearchFileType.Pdf;

        private readonly Settings _settings; 

        public PdfProcessor(Settings settings)
        {
            _settings = settings;
        }   

        public ProcessResults SearchDocument(DictionarySearchProcessModel processModel)
        {
            var result = TryLoadFile(processModel.Data, out var document);

            if (result != null)
                return ProcessResults.CreateFailedProcessResultsModel(result.Value);

            foreach(PdfPageBase page in document.Pages)
            {
                var matchResult = SearchPageForMatch(page, processModel.DictionaryData);

                if(matchResult.HasMatch)
                {
                    return ProcessResults.CreateSuccesfulProcessResultsModel(true, matchResult.Match!);
                }
            }

            return ProcessResults.CreateSuccesfulProcessResultsModel(false);
        }

        private MatchingResult SearchPageForMatch(PdfPageBase page, IEnumerable<DictionaryValue> dictionaryData)
        {
            var extractor = new PdfTextExtractor(page);

            var extractOptions = new PdfTextExtractOptions() {
                IsSimpleExtraction = true
            };

            var pageText = extractor.ExtractText(extractOptions);

            if (string.IsNullOrWhiteSpace(this._settings.ValidationSettings!.PdfLibraryLicenseKey) || this._settings.ValidationSettings.PdfLibraryLicenseKey == "test")
            {
                pageText = pageText
                              .Replace("Evaluation Warning : The document was created with Spire.PDF for .NET.", "")
                              .Replace("Evaluation Warning : The document was created with Spire.PDF for .NET.", "");
            }
            pageText = pageText.Replace("\r\n","");

            var match = dictionaryData.FirstOrDefault(value =>( TextFunctions.TextContainsValue(pageText, value)));

            if (match != null)
            {
                return MatchingResult.CreateMatchResultWithMatch(match.Value);
            } 

            return MatchingResult.CreateMatchResultWithoutMatch();
        }

        private ErrorType? TryLoadFile(Stream data, out PdfDocument document)
        {
            try
            {
                document = new PdfDocument();
                document.LoadFromStream(data);

                return null;
            }
            catch (Exception ex)
            {
                document = null!;
                if (ex.Message.Equals("can not open an encrypted document. The password is invalid.", StringComparison.InvariantCultureIgnoreCase))
                {
                    return ErrorType.FileEncrypted;
                } 
                return ErrorType.FileCannotBeLoaded;
            }
        }
    }
}