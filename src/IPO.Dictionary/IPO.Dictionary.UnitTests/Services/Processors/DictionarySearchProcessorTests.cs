using AwesomeAssertions;
using IPO.Dictionary.Interfaces;
using IPO.Dictionary.Models;
using IPO.Dictionary.Models.DictionarySearch;
using IPO.Dictionary.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPO.Dictionary.UnitTests.Services.Processors
{
    [TestClass]
    public class DictionarySearchProcessorTests
    {
        private readonly DictionarySearchProcessor _dictionarySearchProcessor;
        private readonly IEnumerable<IDocumentProcessor> _documentProcessors;
        public DictionarySearchProcessorTests()
        {
            var docxProcessor = new Mock<IDocumentProcessor>();
            docxProcessor.Setup(o => o.DictionarySearchFileType).Returns(DictionarySearchFileType.Docx).Verifiable();
            docxProcessor.Setup(o => o.SearchDocument(It.IsAny<DictionarySearchProcessModel>()))
                        .Returns(ProcessResults.CreateSuccesfulProcessResultsModel(true,"docx-match"))
                        .Verifiable();
            var odtProcessor = new Mock<IDocumentProcessor>();
            odtProcessor.Setup(o => o.DictionarySearchFileType).Returns(DictionarySearchFileType.Odt).Verifiable();
            odtProcessor.Setup(o => o.SearchDocument(It.IsAny<DictionarySearchProcessModel>()))
                        .Returns(ProcessResults.CreateSuccesfulProcessResultsModel(true, "odt-match"))
                        .Verifiable();
            var pdfProcessor = new Mock<IDocumentProcessor>();
            pdfProcessor.Setup(o => o.DictionarySearchFileType).Returns(DictionarySearchFileType.Pdf).Verifiable();
            pdfProcessor.Setup(o => o.SearchDocument(It.IsAny<DictionarySearchProcessModel>()))
                        .Returns(ProcessResults.CreateSuccesfulProcessResultsModel(true, "pdf-match"))
                        .Verifiable();
            _documentProcessors = new IDocumentProcessor[] { docxProcessor.Object
                                                            , odtProcessor.Object
                                                            , pdfProcessor.Object};


            this._dictionarySearchProcessor = new DictionarySearchProcessor(_documentProcessors);
        }

        [DataRow(DictionarySearchFileType.Docx, "docx-match")]
        [DataRow(DictionarySearchFileType.Odt, "odt-match")]
        [DataRow(DictionarySearchFileType.Pdf, "pdf-match")]
        [TestMethod]
        public void SearchDocumentCallsExpectedProcessor(DictionarySearchFileType dictionarySearchFileType, string expectedMatch)
        {
            // Arrange
            var processModel = new DictionarySearchProcessModel(dictionarySearchFileType,null!,null!);

            // Act
            var result = _dictionarySearchProcessor.SearchDocument(processModel);

            // Assert
            result.Match.Should().Be(expectedMatch);
        }

        [TestMethod]
        public void SearchDocumentWhenDocumentTypeIsInvalidThrowsException()
        {
            // Arrange
            var processModel = new DictionarySearchProcessModel( DictionarySearchFileType.NotSupported, null!, null!);

            // Act
            var resultAction = () => _dictionarySearchProcessor.SearchDocument(processModel);

            // Assert 
            resultAction.Should().Throw<InvalidOperationException>();
        }

        [DataRow("")]
        [DataRow("   ")]
        [DataRow(null)]
        [TestMethod]
        public void GetDictionaryValuesWhenEmptyOrNullReturnsEmptyArray(string dictionaryData)
        {
            // Arrange
            var expectedResult = Array.Empty<DictionaryValue>();

            // Act
            var result = _dictionarySearchProcessor.GetDictionaryValues(dictionaryData);

            // Assert
            result.Should().Equal(expectedResult);

        }

        [DataRow(DictionaryValueType.Word, "test")]
        [DataRow(DictionaryValueType.Phrase, "this is a test phrase")]
        [TestMethod]
        public void GetDictionaryValuesWhenJsonArrayReturnsExpectedArray(DictionaryValueType expectedType, string expectedValue)
        {
            // Arrange 
            var dictionaryValuesList = new string[] { $"{expectedValue}1", $"{expectedValue}2", $"{expectedValue}3" };
            var expectedResult = dictionaryValuesList.Select(o=> new DictionaryValue(o, expectedType));
            string dictionaryData = JsonConvert.SerializeObject(dictionaryValuesList); 

            // Act
            var result = _dictionarySearchProcessor.GetDictionaryValues(dictionaryData);

            // Assert
            result.Select(o=>o.Value).Should().Equal(expectedResult.Select(o=>o.Value));
            result.Select(o => o.Type).Should().AllBeEquivalentTo(expectedType);

        }

        [DataRow(DictionaryValueType.Word, "test")]
        [DataRow(DictionaryValueType.Phrase, "this is a test phrase")]
        [TestMethod]
        public void GetDictionaryValuesWhenCommaSeparatedListReturnsExpectedArray(DictionaryValueType expectedType, string expectedValue)
        {
            // Arrange 
            var dictionaryValuesList = new string[] { $"{expectedValue}1", $"{expectedValue}2", $"{expectedValue}3" };
            var expectedResult = dictionaryValuesList.Select(o => new DictionaryValue(o, expectedType));
            string dictionaryData = string.Join(",", dictionaryValuesList);

            // Act
            var result = _dictionarySearchProcessor.GetDictionaryValues(dictionaryData);

            // Assert
            result.Select(o => o.Value).Should().Equal(expectedResult.Select(o => o.Value));
            result.Select(o => o.Type).Should().AllBeEquivalentTo(expectedType);

        }
    }
}
