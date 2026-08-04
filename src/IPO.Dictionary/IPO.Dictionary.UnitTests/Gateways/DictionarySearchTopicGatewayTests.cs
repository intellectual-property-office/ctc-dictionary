using Azure.Messaging.ServiceBus;
using AwesomeAssertions;
using IPO.Dictionary.Gateways;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IPO.Dictionary.UnitTests.Gateways
{
    [TestClass]
    public class DictionarySearchTopicGatewayTests
    {
        private readonly Mock<ServiceBusSender> _mockServiceBusSender;

        public DictionarySearchTopicGatewayTests()
        {
            this._mockServiceBusSender = new Mock<ServiceBusSender>();
        }

        [TestMethod]
        public async Task SendMessageToSearchAsyncWhenServiceBusSendMessageAsyncFailsThenThrowsServiceBusException()
        {
            // Arrange
            var fileId = 101;
            this._mockServiceBusSender.Setup(o=>o.SendMessageAsync(It.IsAny<ServiceBusMessage>()
                                                ,It.IsAny<CancellationToken>()))
                                      .ThrowsAsync(new ServiceBusException())
                                      .Verifiable();
                                      
            var dictionarySearchTopicGateway = new DictionarySearchTopicGateway(this._mockServiceBusSender.Object);

            // Act
            var resultAction = async() => await dictionarySearchTopicGateway.SendMessageToSearchAsync(fileId);

            // Assert
            await resultAction.Should().ThrowAsync<ServiceBusException>();
        }

        [TestMethod]
        public async Task SendMessageToSearchAsyncCompletesSuccesfully()
        {
            // Arrange
            var fileId = 101;
            this._mockServiceBusSender.Setup(o => o.SendMessageAsync(It.IsAny<ServiceBusMessage>()
                                                , It.IsAny<CancellationToken>()))
                                      .Returns(Task.CompletedTask)
                                      .Verifiable();

            var dictionarySearchTopicGateway = new DictionarySearchTopicGateway(this._mockServiceBusSender.Object);

            // Act
            await dictionarySearchTopicGateway.SendMessageToSearchAsync(fileId);

            // Assert
            this._mockServiceBusSender.Verify(o => o.SendMessageAsync(It.IsAny<ServiceBusMessage>()
                                                , It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
