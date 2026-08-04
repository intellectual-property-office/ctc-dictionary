using AwesomeAssertions;
using IPO.Dictionary.Models;
using IPO.Dictionary.Models.DictionarySearch;
using IPO.Dictionary.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPO.Dictionary.UnitTests.Services.Processors
{
    [TestClass]
    public class PdfProcessorTests
    {
        private readonly PdfProcessor _processor;

        public PdfProcessorTests()
        {
            this._processor = new PdfProcessor(DictionaryCheckSettingsBuilder.Build());
        }

        [TestMethod]
        public void SearchDocumentWhenFileEncryptedReturnsFileEncryptedError()
        {
            // Arrange
            var expectedResult = ProcessResults.CreateFailedProcessResultsModel(ErrorType.FileEncrypted);
            var processModel = new DictionarySearchProcessModel(DictionarySearchFileType.Pdf, DocumentBuilder.CreatePdfWithEncryption(), Array.Empty<DictionaryValue>());

            // Act
            var result = _processor.SearchDocument(processModel);

            // Assert
            result.Should().NotBeNull();
            var processResult = result.As<ProcessResults>();
            processResult.Match.Should().Be(expectedResult.Match);
            processResult.HasMatch.Should().Be(expectedResult.HasMatch);
            processResult.ErrorType.Should().Be(expectedResult.ErrorType);
            processResult.HasError.Should().Be(expectedResult.HasError);
        }

        [TestMethod]
        public void SearchDocumentWhenFileCannotBeLoadedReturnsFileCannotBeLoadedError()
        {
            // Arrange
            var expectedResult = ProcessResults.CreateFailedProcessResultsModel(ErrorType.FileCannotBeLoaded);
            var processModel = new DictionarySearchProcessModel(DictionarySearchFileType.Pdf, null!, Array.Empty<DictionaryValue>());

            // Act
            var result = _processor.SearchDocument(processModel);

            // Assert
            result.Should().NotBeNull();
            var processResult = result.As<ProcessResults>();
            processResult.Match.Should().Be(expectedResult.Match);
            processResult.HasMatch.Should().Be(expectedResult.HasMatch);
            processResult.ErrorType.Should().Be(expectedResult.ErrorType);
            processResult.HasError.Should().Be(expectedResult.HasError);
        }

        [DataRow(null)]
        [DataRow("test")]
        [TestMethod]
        public void SearchDocumentWhenFileContainsMatchReturnsCorrectMatch(string libraryKey)
        {
            // Arrange
            var match = Guid.NewGuid().ToString();
            var dictionaryData = new DictionaryValue[] { new DictionaryValue(match, DictionaryValueType.Word), new DictionaryValue(Guid.NewGuid().ToString(), DictionaryValueType.Word), new DictionaryValue(Guid.NewGuid().ToString(), DictionaryValueType.Word) };
            var expectedResult = ProcessResults.CreateSuccesfulProcessResultsModel(true, match);
            var processModel = new DictionarySearchProcessModel(DictionarySearchFileType.Pdf, DocumentBuilder.CreatePdf(match: match), dictionaryData);
            var settings = DictionaryCheckSettingsBuilder.Build();
            settings.ValidationSettings!.PdfLibraryLicenseKey = libraryKey;

            // Act
            var result = new PdfProcessor(settings).SearchDocument(processModel);

            // Assert
            result.Should().NotBeNull();
            var processResult = result.As<ProcessResults>();
            processResult.Match.Should().Be(expectedResult.Match);
            processResult.HasMatch.Should().Be(expectedResult.HasMatch);
            processResult.HasError.Should().Be(expectedResult.HasError);
        }

        [TestMethod]
        public void SearchDocumentWhenFileNotContainsMatchReturnsNonMatchResult()
        {
            // Arrange
            var match = Guid.NewGuid().ToString();
            var dictionaryData = new DictionaryValue[] { new DictionaryValue( Guid.NewGuid().ToString() + "one", DictionaryValueType.Word), new DictionaryValue( Guid.NewGuid().ToString() + "two", DictionaryValueType.Word), new DictionaryValue( Guid.NewGuid().ToString() + "three", DictionaryValueType.Word) };
            var expectedResult = ProcessResults.CreateSuccesfulProcessResultsModel(false);
            var processModel = new DictionarySearchProcessModel(DictionarySearchFileType.Pdf, DocumentBuilder.CreatePdf(match: match), dictionaryData);

            // Act
            var result = _processor.SearchDocument(processModel);

            // Assert
            result.Should().NotBeNull();
            var processResult = result.As<ProcessResults>();
            processResult.Match.Should().Be(expectedResult.Match);
            processResult.HasMatch.Should().Be(expectedResult.HasMatch);
            processResult.HasError.Should().Be(expectedResult.HasError);
        }

        [TestMethod]
        public void DictionarySearchFileTypeReturnsCorrectType()
        {
            // Arrange
            var expectedType = DictionarySearchFileType.Pdf;

            // Act
            var result = this._processor.DictionarySearchFileType;

            // Assert
            result.Should().Be(expectedType);
        }
    }
}
