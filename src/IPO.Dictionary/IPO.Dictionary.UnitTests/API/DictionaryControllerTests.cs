using AutoFixture;
using AwesomeAssertions;
using IPO.Dictionary.API.Controllers;
using IPO.Dictionary.Interfaces;
using IPO.Dictionary.Models.API;
using IPO.Dictionary.Models.DictionarySearch;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace IPO.Dictionary.UnitTests.API
{
    [TestClass]
    public class DictionaryControlerTests
    {
        private readonly Mock<IDictionarySearchManagementService> _mockDictionarySearchManagementService;
        private readonly Fixture _fixture;

        public DictionaryControlerTests()
        {
            this._mockDictionarySearchManagementService = new Mock<IDictionarySearchManagementService>();
            this._fixture = new Fixture();
        }

        [DataRow(DictionaryType.Profanity)]
        [DataRow(DictionaryType.Military)]
        [TestMethod]
        public async Task SearchDictionaryReturnsAcceptedAndCorrectResults(DictionaryType dictionaryType)
        {
            //Arrange 
            var testResults = _fixture.Build<SearchDictionaryResult>()
                                      .Create(); 

            _mockDictionarySearchManagementService
                .Setup(s => s.SearchDictionaryAsync(It.IsAny<IFormFile>(), It.IsAny<DictionaryType>(), It.IsAny<int?>()))
                .ReturnsAsync(testResults)
                .Verifiable();

            var dictionarySearchApi = new DictionarySearchController(_mockDictionarySearchManagementService.Object);
            var fileName = "text.pdf";
            var fileSize = 1024;
            var documentToBeSearched = DocumentBuilder.CreateDocument(fileName, fileSize);

            // Act 
            var dictionarySearchRequest = await dictionarySearchApi.SearchDictionary( new SearchDictionaryModel() { 
             dictionaryType = dictionaryType,
              file = documentToBeSearched,
               MaximumFileSize = 1024
            });
            var dictionarySearchResult = (AcceptedResult)dictionarySearchRequest.Result!;
            var results = (SearchDictionaryResult)dictionarySearchResult.Value!;

            // Assert
            results.Should().Be(testResults);
            dictionarySearchResult.StatusCode.Should().NotBeNull();
            dictionarySearchResult.StatusCode.Should().Be((int)HttpStatusCode.Accepted);
            _mockDictionarySearchManagementService.Verify();
        }

        [TestMethod]
        public async Task SearchResultsReturnsOkAndCorrectDictionarySearchResults()
        {
            //Arrange
            var fileId = new Random().Next(1, int.MaxValue);

            var testResults = new SearchResult(Status.Completed, fileId, _fixture.Create<SearchDetails>()); 

            _mockDictionarySearchManagementService
                .Setup(s => s.GetDictionaryResultsAsync(It.IsAny<int>()))
                .ReturnsAsync(testResults)
                .Verifiable();

            var dictionarySearchApi = new DictionarySearchController(_mockDictionarySearchManagementService.Object);

            // Act 
            var getDictionarySearchResultsRequest = await dictionarySearchApi.SearchResults(fileId);
            var getDictionarySearchResultsRequestResult = (OkObjectResult)getDictionarySearchResultsRequest!.Result!;
            var results = (SearchResult)getDictionarySearchResultsRequestResult!.Value!;

            // Assert
            results.Should().Be(testResults);
            getDictionarySearchResultsRequestResult.StatusCode.Should().NotBeNull();
            getDictionarySearchResultsRequestResult.StatusCode.Should().Be((int)HttpStatusCode.OK);
            _mockDictionarySearchManagementService.Verify();
        }
    }
}
