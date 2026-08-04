using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using AwesomeAssertions;
using IPO.Dictionary.Gateways;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IPO.Dictionary.UnitTests.Gateways
{
    [TestClass]
    public class DictionarySearchBlobStorageGatewayTests
    {
        private readonly Mock<BlobContainerClient> _mockContainerClient;
        public DictionarySearchBlobStorageGatewayTests()
        {
            this._mockContainerClient = new Mock<BlobContainerClient>();
        }

        [TestMethod]
        public async Task UploadFileAsyncReturnsBlobName()
        {
            // Arrange
            var stream = new MemoryStream(Guid.NewGuid().ToByteArray());
            var blobContentResponse = new Mock<Response<BlobContentInfo>>(); 
            this._mockContainerClient.Setup(o => o.UploadBlobAsync(It.IsAny<string>()
                                            , It.IsAny<Stream>()
                                            , It.IsAny<CancellationToken>()))
                                            .ReturnsAsync(blobContentResponse.Object)
                                            .Verifiable();

            var dictionarySearchBlobStorageGateway = new DictionarySearchBlobStorageGateway(this._mockContainerClient.Object);

            // Act
            var result =  await dictionarySearchBlobStorageGateway.UploadFileAsync(stream);

            // Assert
            result.Should().NotBeNullOrWhiteSpace();
            Guid.TryParse(result, out _).Should().BeTrue(); 

            this._mockContainerClient.Verify(o => o.UploadBlobAsync(It.IsAny<string>()
                                            , It.IsAny<Stream>()
                                            , It.IsAny<CancellationToken>()),
                                            Times.Once);
        }

        [TestMethod]
        public async Task GetUploadedBlobAsyncWhenSuccessfulReturnsBlobFile()
        {
            // Arrange
            var contentType = "testType/testType";
            var contentLength = 1024;
            var stream = new MemoryStream(Guid.NewGuid().ToByteArray());
            var mockBlobClient = new Mock<BlobClient>();
            var blobProperties = BlobsModelFactory.BlobProperties(contentType: contentType, contentLength: contentLength);
            var mockBlobPropertiesResponse = new Mock<Response<BlobProperties>>();
            mockBlobPropertiesResponse.Setup(o => o.Value).Returns(blobProperties).Verifiable();

            var mockAzureHttpResponse = new Mock<Response>();
            mockAzureHttpResponse.Setup(o => o.ContentStream).Returns(stream).Verifiable();

            this._mockContainerClient.Setup(o => o.GetBlobClient(It.IsAny<string>()))
                                    .Returns(mockBlobClient.Object)
                                    .Verifiable();

            mockBlobClient.Setup(o => o.GetPropertiesAsync(
                            It.IsAny<BlobRequestConditions>()
                            , It.IsAny<CancellationToken>()))
                            .ReturnsAsync(mockBlobPropertiesResponse.Object)
                            .Verifiable();

            mockBlobClient.Setup(o => o.DownloadToAsync(It.IsAny<Stream>()))
                            .ReturnsAsync(mockAzureHttpResponse.Object)
                            .Verifiable();

            var dictionarySearchBlobStorageGateway = new DictionarySearchBlobStorageGateway(this._mockContainerClient.Object);

            // Act
            var result = await dictionarySearchBlobStorageGateway.GetUploadedBlobAsync(Guid.NewGuid().ToString());

            // Assert
            result.Should().NotBeNull();
            result.ContentLength.Should().Be(contentLength);
            result.ContentType.Should().Be(contentType);
            result.Data.Should().NotBeNull();
            this._mockContainerClient.Verify(o=> o.GetBlobClient(It.IsAny<string>()), Times.Once);
            mockBlobClient.Verify(o => o.GetPropertiesAsync(
                            It.IsAny<BlobRequestConditions>()
                            , It.IsAny<CancellationToken>()), Times.Once);
            mockBlobClient.Verify(o => o.DownloadToAsync(It.IsAny<Stream>()), Times.Once);

        }

        [DataRow(true,true)]
        [DataRow(false,false)]
        [TestMethod]
        public async Task DeleteBlobAsyncWhenFileExistsReturnsExpectedResult(bool responseValue, bool expectedResult)
        {
            // Arrange
            var blobName = Guid.NewGuid().ToString();
            var mockAzureHttpResponse = new Mock<Response<bool>>();
            mockAzureHttpResponse.Setup(o => o.Value).Returns(responseValue).Verifiable();

            this._mockContainerClient.Setup(o=>o.DeleteBlobIfExistsAsync(It.IsAny<string>()
                                            ,It.IsAny<DeleteSnapshotsOption>()
                                            ,It.IsAny<BlobRequestConditions>()
                                            , It.IsAny<CancellationToken>()))
                                            .ReturnsAsync(mockAzureHttpResponse.Object)
                                            .Verifiable();

            var dictionarySearchBlobStorageGateway = new DictionarySearchBlobStorageGateway(this._mockContainerClient.Object);

            // Act
            var result = await dictionarySearchBlobStorageGateway.DeleteBlobAsync(blobName);


            // Assert
            result.Should().Be(expectedResult);
            this._mockContainerClient.Verify(o => o.DeleteBlobIfExistsAsync(It.IsAny<string>()
                                            , It.IsAny<DeleteSnapshotsOption>()
                                            , It.IsAny<BlobRequestConditions>()
                                            , It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
