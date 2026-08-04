using AutoFixture;
using Azure;
using AwesomeAssertions;
using IPO.Common.Infrastructure;
using IPO.Dictionary.Interfaces;
using IPO.Dictionary.Models;
using IPO.Dictionary.Models.API;
using IPO.Dictionary.Models.DictionarySearch; 
using IPO.Dictionary.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPO.Dictionary.UnitTests.Services
{
    [TestClass]
    public class DictionarySearchManagementServiceTests
    {
        private readonly Mock<IDictionarySearchBlobStorageGateway> _mockDictionarySearchBlobStorageGateway;
        private readonly Mock<IDictionarySearchTopicGateway> _mockDictionarySearchTopicGateway;
        private readonly Mock<IDictionarySearchProcessor> _mockDictionarySearchProcessingService;
        private readonly Mock<IDictionarySearchValidator> _mockDictionarySearchValidationService;
        private readonly Mock<IDictionarySearchDbRepository> _mockDictionarySearchRepositoryService;
        private readonly Mock<ILogger<DictionarySearchManagementService>> _mockLogger;
        private readonly Fixture _fixture;

        public DictionarySearchManagementServiceTests()
        {
            this._mockDictionarySearchBlobStorageGateway = new Mock<IDictionarySearchBlobStorageGateway>();
            this._mockDictionarySearchTopicGateway = new Mock<IDictionarySearchTopicGateway>();
            this._mockDictionarySearchProcessingService = new Mock<IDictionarySearchProcessor>();
            this._mockDictionarySearchValidationService = new Mock<IDictionarySearchValidator>();
            this._mockDictionarySearchRepositoryService = new Mock<IDictionarySearchDbRepository>();
            this._mockLogger = new Mock<ILogger<DictionarySearchManagementService>>();
            this._fixture = new Fixture();
        }

        [DataRow(DictionaryType.Profanity)]
        [DataRow(DictionaryType.Military)]
        [TestMethod]
        public async Task SearchDictionaryAsyncReturnsCorrectResults(DictionaryType dictionaryType)
        {
            // Arrange
            var dictionarySearchResults = new SearchDictionaryResult(101);
            var file = DocumentBuilder.CreateDocument("test.docx", 1024);
            var validationResult = new DictionarySearchValidationResult() { Code = 200 };
            var blobName = Guid.NewGuid().ToString();

            this._mockDictionarySearchValidationService.Setup(o => o.Validate(It.IsAny<Stream>(),
                                                                            It.IsAny<string>(),
                                                                            It.IsAny<string>(),
                                                                            It.IsAny<int?>()))
                                                        .Returns(validationResult)
                                                        .Verifiable();


            this._mockDictionarySearchBlobStorageGateway.Setup(o => o.UploadFileAsync(It.IsAny<Stream>()))
                                                        .ReturnsAsync(blobName)
                                                        .Verifiable();

            this._mockDictionarySearchRepositoryService.Setup(o => o.CreateDictionarySearchRecordAsync(It.IsAny<string>(), It.IsAny<DictionarySearchFileType>(), It.IsAny<DictionaryType>()))
                                                        .ReturnsAsync(dictionarySearchResults.ResultsId)
                                                        .Verifiable();

            this._mockDictionarySearchTopicGateway.Setup(o => o.SendMessageToSearchAsync(It.IsAny<int>()))
                                                  .Returns(Task.CompletedTask)
                                                  .Verifiable();

            // Act
            var dictionarySearchManagementService = new DictionarySearchManagementService(this._mockDictionarySearchBlobStorageGateway.Object
                                                                                        , this._mockDictionarySearchTopicGateway.Object
                                                                                        , this._mockDictionarySearchProcessingService.Object
                                                                                        , this._mockDictionarySearchValidationService.Object
                                                                                        , this._mockDictionarySearchRepositoryService.Object
                                                                                        , this._mockLogger.Object);

            var result = await dictionarySearchManagementService.SearchDictionaryAsync(file, dictionaryType, (int?)(file.Length - 1));

            // Assert
            result.Should().NotBeNull();
            result.ResultsId.Should().Be(dictionarySearchResults.ResultsId);
        }


        [TestMethod]
        public async Task SearchDictionaryAsyncWhenValidationResultCodeIsNotOkThrowsStatusCodeException()
        {
            // Arrange 
            var file = DocumentBuilder.CreateDocument("test.docx", 1024);
            var validationResult = new DictionarySearchValidationResult() { Code = 415 };  

            this._mockDictionarySearchValidationService.Setup(o => o.Validate(It.IsAny<Stream>(),
                                                                            It.IsAny<string>(),
                                                                            It.IsAny<string>(),
                                                                            It.IsAny<int?>()
                                                                            ))
                                                        .Returns(validationResult)
                                                        .Verifiable();

            // Act
            var dictionarySearchManagementService = new DictionarySearchManagementService(this._mockDictionarySearchBlobStorageGateway.Object
                                                                                        , this._mockDictionarySearchTopicGateway.Object
                                                                                        , this._mockDictionarySearchProcessingService.Object
                                                                                        , this._mockDictionarySearchValidationService.Object
                                                                                        , this._mockDictionarySearchRepositoryService.Object
                                                                                        , this._mockLogger.Object);

            var resultAction = async () => await dictionarySearchManagementService.SearchDictionaryAsync(file, DictionaryType.Military, (int?)(file.Length - 1));

            // Assert
            var exception = await resultAction.Should().ThrowAsync<StatusCodeException>();
            exception.Subject.First().StatusCode.Should().Be(validationResult.Code);
        }

        [TestMethod]
        public async Task GetDictionaryResultsAsyncWhenDictionarySearchRecordIsNullThrowsStatusCodeException()
        {
            // Arrange
            var dictionSearchRecordId = 101;

            this._mockDictionarySearchRepositoryService.Setup(o => o.GetDictionarySearchDataAsync(It.IsAny<int>()))
                                                        .ReturnsAsync((DictionarySearchData)null!)
                                                        .Verifiable();


            // Act
            var dictionarySearchManagementService = new DictionarySearchManagementService(this._mockDictionarySearchBlobStorageGateway.Object
                                                                                        , this._mockDictionarySearchTopicGateway.Object
                                                                                        , this._mockDictionarySearchProcessingService.Object
                                                                                        , this._mockDictionarySearchValidationService.Object
                                                                                        , this._mockDictionarySearchRepositoryService.Object
                                                                                        , this._mockLogger.Object);

            var resultAction = async () => await dictionarySearchManagementService.GetDictionaryResultsAsync(dictionSearchRecordId);

            // Assert
            await resultAction.Should().ThrowAsync<StatusCodeException>();
        }

        [DataRow(Status.Failed)]
        [DataRow(Status.InProgress)]
        [DataRow(Status.Uploaded)]
        [TestMethod]
        public async Task GetDictionaryResultsAsyncWhenDictionarySearchStatusIsNotCompletedThrowsStatusCodeException(Status status)
        {
            // Arrange 

            DictionarySearchData dictionarySearchData = new DictionarySearchData()
            {
                Status = status,
                BlobName = Guid.NewGuid().ToString(),
                CreatedOn = DateTime.UtcNow,
                DictionaryType = DictionaryType.Profanity,
                FileType = DictionarySearchFileType.Docx,
                Id = 101,
                Match = null
            };


            this._mockDictionarySearchRepositoryService.Setup(o => o.GetDictionarySearchDataAsync(It.IsAny<int>()))
                                                        .ReturnsAsync(dictionarySearchData)
                                                        .Verifiable();



            // Act
            var dictionarySearchManagementService = new DictionarySearchManagementService(this._mockDictionarySearchBlobStorageGateway.Object
                                                                                        , this._mockDictionarySearchTopicGateway.Object
                                                                                        , this._mockDictionarySearchProcessingService.Object
                                                                                        , this._mockDictionarySearchValidationService.Object
                                                                                        , this._mockDictionarySearchRepositoryService.Object
                                                                                        , this._mockLogger.Object);

            var result = await dictionarySearchManagementService.GetDictionaryResultsAsync(dictionarySearchData.Id);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(status);
            result.Results.Should().BeNull();
            result.ResultsId.Should().Be(dictionarySearchData.Id);
        }

        [TestMethod]
        public async Task GetDictionaryResultsAsyncWhenDictionarySearchStatusIsCompletedThrowsStatusCodeException()
        {
            // Arrange 

            var dictionarySearchData = new DictionarySearchData()
            {
                Status = Status.Completed,
                BlobName = Guid.NewGuid().ToString(),
                CreatedOn = DateTime.UtcNow,
                DictionaryType = DictionaryType.Profanity,
                FileType = DictionarySearchFileType.Docx,
                Id = 101,
                Match =  "test-match"
            };


            this._mockDictionarySearchRepositoryService.Setup(o => o.GetDictionarySearchDataAsync(It.IsAny<int>()))
                                                        .ReturnsAsync(dictionarySearchData)
                                                        .Verifiable();

            this._mockDictionarySearchRepositoryService.Setup(o => o.DeleteDictionarySearchRecordAsync(It.IsAny<int>()))
                                                        .Returns(Task.CompletedTask)
                                                        .Verifiable();

            // Act
            var dictionarySearchManagementService = new DictionarySearchManagementService(this._mockDictionarySearchBlobStorageGateway.Object
                                                                                        , this._mockDictionarySearchTopicGateway.Object
                                                                                        , this._mockDictionarySearchProcessingService.Object
                                                                                        , this._mockDictionarySearchValidationService.Object
                                                                                        , this._mockDictionarySearchRepositoryService.Object
                                                                                        , this._mockLogger.Object);

            var result = await dictionarySearchManagementService.GetDictionaryResultsAsync(dictionarySearchData.Id);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(Status.Completed);
            result.Results.Should().NotBeNull();
            result.Results.As<SearchDetails>().IsMatch.Should().BeTrue();
            result.Results.As<SearchDetails>().Match.Should().Be(dictionarySearchData.Match);
            result.ResultsId.Should().Be(dictionarySearchData.Id);
            this._mockDictionarySearchRepositoryService.Verify(o=>o.DeleteDictionarySearchRecordAsync(It.IsAny<int>()), Times.Once);
        }

        [TestMethod]
        public async Task ProcessDictionarySearchMessageAsyncWhenDictionarySearchRecordIsNullReturnsWithoutAnyFurtherActions()
        {
            // Arrange
            var dictionarySearchRecordId = 101;
            var searchMessage = new DictionarySearchBusMessage(dictionarySearchRecordId);

            this._mockDictionarySearchRepositoryService.Setup(o => o.GetDictionarySearchDataAsync(It.IsAny<int>()))
                                                               .ReturnsAsync((DictionarySearchData)null!)
                                                               .Verifiable();

            //Act
            var dictionarySearchManagementService = new DictionarySearchManagementService(this._mockDictionarySearchBlobStorageGateway.Object
                                                                                        , this._mockDictionarySearchTopicGateway.Object
                                                                                        , this._mockDictionarySearchProcessingService.Object
                                                                                        , this._mockDictionarySearchValidationService.Object
                                                                                        , this._mockDictionarySearchRepositoryService.Object
                                                                                        , this._mockLogger.Object);

            await dictionarySearchManagementService.ProcessDictionarySearchMessageAsync(searchMessage);

            // Assert

            this._mockDictionarySearchRepositoryService.Verify(o => o.UpdateDictionarySearchStatusAsync(It.IsAny<int>(), It.IsAny<Status>()), Times.Never); 
        }


        [TestMethod]
        public async Task ProcessDictionarySearchMessageAsyncWhenProcessResultsHasErrorUpdatesStatusToFailedAndReturns()
        {
            // Arrange
            var dictionarySearchRecordId = 101;
            var searchMessage = new DictionarySearchBusMessage(dictionarySearchRecordId);
            var dictionarySearchData = new DictionarySearchData()
            {
                Status = Status.Uploaded,
                BlobName = Guid.NewGuid().ToString(),
                CreatedOn = DateTime.UtcNow,
                DictionaryType = DictionaryType.Profanity,
                FileType = DictionarySearchFileType.Docx,
                Id = 101,
                Match = null
            };

            var dictionaryData = String.Join(", ", this._fixture.CreateMany<string>(10));

            var blobFile = new StorageFile(1024
                                        , "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                        , new MemoryStream(Guid.NewGuid().ToByteArray()) );

            var processResults = ProcessResults.CreateFailedProcessResultsModel(ErrorType.FileEncrypted);

            this._mockDictionarySearchRepositoryService.Setup(o => o.GetDictionarySearchDataAsync(It.IsAny<int>()))
                                                                .ReturnsAsync(dictionarySearchData)
                                                                .Verifiable();

            this._mockDictionarySearchRepositoryService.Setup(o => o.UpdateDictionarySearchStatusAsync(It.IsAny<int>(), It.IsAny<Status>()))
                                                                .Returns(Task.CompletedTask)
                                                                .Verifiable();

            this._mockDictionarySearchBlobStorageGateway.Setup(o => o.GetUploadedBlobAsync(It.IsAny<string>()))
                                                        .ReturnsAsync(blobFile)
                                                        .Verifiable();

            this._mockDictionarySearchRepositoryService.Setup(o => o.GetDictionaryDataAsync(It.IsAny<DictionaryType>()))
                                                                .ReturnsAsync(dictionaryData)
                                                                .Verifiable();

            this._mockDictionarySearchProcessingService.Setup(o => o.SearchDocument(It.IsAny<DictionarySearchProcessModel>()))
                                                        .Returns(processResults)
                                                        .Verifiable(); 

            //Act
            var dictionarySearchManagementService = new DictionarySearchManagementService(this._mockDictionarySearchBlobStorageGateway.Object
                                                                                        , this._mockDictionarySearchTopicGateway.Object
                                                                                        , this._mockDictionarySearchProcessingService.Object
                                                                                        , this._mockDictionarySearchValidationService.Object
                                                                                        , this._mockDictionarySearchRepositoryService.Object
                                                                                        , this._mockLogger.Object);

            await dictionarySearchManagementService.ProcessDictionarySearchMessageAsync(searchMessage);

            // Assert

            this._mockDictionarySearchRepositoryService.Verify(o => o.UpdateDictionarySearchStatusAsync(It.IsAny<int>(), It.IsAny<Status>()), Times.Exactly(2));
            this._mockDictionarySearchBlobStorageGateway.Verify(o=> o.DeleteBlobAsync(It.IsAny<string>()) ,  Times.Never );
        } 

        [TestMethod]
        public async Task ProcessDictionarySearchMessageAsyncWhenDeleteBlobThrowsARequestFailedException()
        {
            // Arrange
            var dictionarySearchRecordId = 101;
            var searchMessage = new DictionarySearchBusMessage(dictionarySearchRecordId);
            var dictionarySearchData = new DictionarySearchData()
            {
                Status = Status.Completed,
                BlobName = Guid.NewGuid().ToString(),
                CreatedOn = DateTime.UtcNow,
                DictionaryType = DictionaryType.Profanity,
                FileType = DictionarySearchFileType.Docx,
                Id = 101,
                Match = "test-match"
            };

            var dictionaryData = String.Join(", ", this._fixture.CreateMany<string>(10));

            var blobFile = new StorageFile(1024
                                        , "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                        , new MemoryStream(Guid.NewGuid().ToByteArray()));

            var processResults = ProcessResults.CreateSuccesfulProcessResultsModel(true, "test-match");

            this._mockDictionarySearchRepositoryService.Setup(o => o.GetDictionarySearchDataAsync(It.IsAny<int>()))
                                                                .ReturnsAsync(dictionarySearchData)
                                                                .Verifiable();

            this._mockDictionarySearchRepositoryService.Setup(o => o.UpdateDictionarySearchStatusAsync(It.IsAny<int>(), It.IsAny<Status>()))
                                                                .Returns(Task.CompletedTask)
                                                                .Verifiable();

            this._mockDictionarySearchBlobStorageGateway.Setup(o => o.GetUploadedBlobAsync(It.IsAny<string>()))
                                                        .ReturnsAsync(blobFile)
                                                        .Verifiable();

            this._mockDictionarySearchRepositoryService.Setup(o => o.GetDictionaryDataAsync(It.IsAny<DictionaryType>()))
                                                                .ReturnsAsync(dictionaryData)
                                                                .Verifiable();

            this._mockDictionarySearchProcessingService.Setup(o => o.SearchDocument(It.IsAny<DictionarySearchProcessModel>()))
                                                        .Returns(processResults)
                                                        .Verifiable();

            this._mockDictionarySearchBlobStorageGateway.Setup(o => o.DeleteBlobAsync(It.IsAny<string>()))
                                                        .ThrowsAsync(new RequestFailedException("test error message."))
                                                        .Verifiable();

            //Act
            var dictionarySearchManagementService = new DictionarySearchManagementService(this._mockDictionarySearchBlobStorageGateway.Object
                                                                                        , this._mockDictionarySearchTopicGateway.Object
                                                                                        , this._mockDictionarySearchProcessingService.Object
                                                                                        , this._mockDictionarySearchValidationService.Object
                                                                                        , this._mockDictionarySearchRepositoryService.Object
                                                                                        , this._mockLogger.Object);

            var actionResult = async () => await dictionarySearchManagementService.ProcessDictionarySearchMessageAsync(searchMessage);

            // Assert
            await actionResult.Should().NotThrowAsync<RequestFailedException>();
        }

        [TestMethod]
        public async Task ProcessDictionarySearchMessageAsyncCompletesSuccesfully()
        {
            // Arrange
            var dictionarySearchRecordId = 101;
            var searchMessage = new DictionarySearchBusMessage(dictionarySearchRecordId);
            var dictionarySearchData = new DictionarySearchData()
            {
                Status = Status.Uploaded,
                BlobName = Guid.NewGuid().ToString(),
                CreatedOn = DateTime.UtcNow,
                DictionaryType = DictionaryType.Profanity,
                FileType = DictionarySearchFileType.Docx,
                Id = 101,
                Match = null
            };

            var dictionaryData = String.Join(", ", this._fixture.CreateMany<string>(10));

            var blobFile = new StorageFile(1024
                                        , "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                        , new MemoryStream(Guid.NewGuid().ToByteArray()));

            var processResults = ProcessResults.CreateSuccesfulProcessResultsModel(true, "test-match");

            this._mockDictionarySearchRepositoryService.Setup(o => o.GetDictionarySearchDataAsync(It.IsAny<int>()))
                                                                .ReturnsAsync(dictionarySearchData)
                                                                .Verifiable();

            this._mockDictionarySearchRepositoryService.Setup(o => o.UpdateDictionarySearchStatusAsync(It.IsAny<int>(), It.IsAny<Status>()))
                                                                .Returns(Task.CompletedTask)
                                                                .Verifiable();

            this._mockDictionarySearchBlobStorageGateway.Setup(o => o.GetUploadedBlobAsync(It.IsAny<string>()))
                                                        .ReturnsAsync(blobFile)
                                                        .Verifiable();

            this._mockDictionarySearchRepositoryService.Setup(o => o.GetDictionaryDataAsync(It.IsAny<DictionaryType>()))
                                                                .ReturnsAsync(dictionaryData)
                                                                .Verifiable();

            this._mockDictionarySearchProcessingService.Setup(o => o.SearchDocument(It.IsAny<DictionarySearchProcessModel>()))
                                                        .Returns(processResults)
                                                        .Verifiable();

            this._mockDictionarySearchBlobStorageGateway.Setup(o => o.DeleteBlobAsync(It.IsAny<string>()))
                                                        .ReturnsAsync(true)
                                                        .Verifiable();

            //Act
            var dictionarySearchManagementService = new DictionarySearchManagementService(this._mockDictionarySearchBlobStorageGateway.Object
                                                                                        , this._mockDictionarySearchTopicGateway.Object
                                                                                        , this._mockDictionarySearchProcessingService.Object
                                                                                        , this._mockDictionarySearchValidationService.Object
                                                                                        , this._mockDictionarySearchRepositoryService.Object
                                                                                        , this._mockLogger.Object);

            await dictionarySearchManagementService.ProcessDictionarySearchMessageAsync(searchMessage);

            // Assert
            this._mockDictionarySearchBlobStorageGateway.Verify(o=>o.DeleteBlobAsync(It.IsAny<string>()), Times.Once);
        }


        [TestMethod]
        public async Task ProcessDictionarySearchMessageAsyncWhenMessageAlreadyCompletedReturns()
        {
            // Arrange
            var dictionarySearchRecordId = 101;
            var searchMessage = new DictionarySearchBusMessage(dictionarySearchRecordId);
            var dictionarySearchData = new DictionarySearchData()
            {
                Status = Status.Completed,
                BlobName = Guid.NewGuid().ToString(),
                CreatedOn = DateTime.UtcNow,
                DictionaryType = DictionaryType.Profanity,
                FileType = DictionarySearchFileType.Docx,
                Id = 101,
                Match = "test-match"
            }; 

            this._mockDictionarySearchRepositoryService.Setup(o => o.GetDictionarySearchDataAsync(It.IsAny<int>()))
                                                                .ReturnsAsync(dictionarySearchData)
                                                                .Verifiable();

            this._mockDictionarySearchRepositoryService.Setup(o => o.UpdateDictionarySearchStatusAsync(It.IsAny<int>(), It.IsAny<Status>()))
                                                                .Returns(Task.CompletedTask)
                                                                .Verifiable();

             

            //Act
            var dictionarySearchManagementService = new DictionarySearchManagementService(this._mockDictionarySearchBlobStorageGateway.Object
                                                                                        , this._mockDictionarySearchTopicGateway.Object
                                                                                        , this._mockDictionarySearchProcessingService.Object
                                                                                        , this._mockDictionarySearchValidationService.Object
                                                                                        , this._mockDictionarySearchRepositoryService.Object
                                                                                        , this._mockLogger.Object);

            await dictionarySearchManagementService.ProcessDictionarySearchMessageAsync(searchMessage);

            // Assert

            this._mockDictionarySearchRepositoryService.Verify(o => o.UpdateDictionarySearchStatusAsync(It.IsAny<int>(), It.IsAny<Status>()), Times.Exactly(0)); 
        }
    }
}
