using Azure.Messaging.ServiceBus;
using IPO.Dictionary.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.ServiceBus; 
using IPO.Dictionary.WebJob;
using AwesomeAssertions;
using System.Text.Json;
using IPO.Dictionary.Models.Configuration;
using System.Threading;
using IPO.Dictionary.Models.DictionarySearch;

namespace IPO.Dictionary.UnitTests.WebJob
{
    [TestClass]
    public class WebJobTests
    {
        private readonly Mock<IDictionarySearchManagementService> _mockSearchManagementService;
        private readonly Mock<ILogger> _mockLogger;
        private readonly Settings _settings;

        public WebJobTests()
        {
            this._mockSearchManagementService = new Mock<IDictionarySearchManagementService>();
            this._mockLogger = new Mock<ILogger>();
            _settings = DictionaryCheckSettingsBuilder.Build();
        } 

        [TestMethod]
        public void ProcessDictionarySearchMessageWhenMessageBodyIsInvalidThrowsJsonException()
        {
            // Arrange 
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                                                new BinaryData(Guid.NewGuid().ToByteArray()));
            var mockMessageActions = new Mock<ServiceBusMessageActions>(); 

            var webJob = new Functions(this._mockSearchManagementService.Object, this._settings); 

            // Act
            var resultAction = async() => await webJob.ProcessDictionarySearchMessageAsync(message, mockMessageActions.Object, this._mockLogger.Object);

            // Assert
            resultAction.Should().ThrowAsync<JsonException>();
        }

        [TestMethod]
        public async Task ProcessDictionarySearchMessageWhenMessageTypeIsInvalidThrowsInvalidOperationException()
        {
            // Arrange 
            var type = "test-type";
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                                                new BinaryData(new DictionarySearchBusMessage(1) { Type = type }));
            var mockMessageActions = new Mock<ServiceBusMessageActions>();

            var webJob = new Functions(this._mockSearchManagementService.Object, this._settings); 

            // Act
            var resultAction = async () => await webJob.ProcessDictionarySearchMessageAsync(message, mockMessageActions.Object, this._mockLogger.Object);

            // Assert
            await resultAction.Should().ThrowAsync<InvalidOperationException>().WithMessage($"The message is not routed correctly, it will eventually will get dead-lettered. Expected type: {DictionarySearchBusMessage.messageType}, received type : {type}"); 
        }
         

        [TestMethod]
        public async Task ProcessDictionarySearchMessageCompletesSuccesfully()
        {
            // Arrange  
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                                                new BinaryData(new DictionarySearchBusMessage(1))
                                                , lockedUntil: DateTimeOffset.Now.AddMinutes(1));
            var mockMessageActions = new Mock<ServiceBusMessageActions>();

            var webJob = new Functions(this._mockSearchManagementService.Object, this._settings);
            this._mockSearchManagementService.Setup(o=>o.ProcessDictionarySearchMessageAsync(It.IsAny<DictionarySearchBusMessage>()))
                                            .Returns(Task.FromResult(message))
                                            .Verifiable();

            // Act
            var resultAction = async () => await webJob.ProcessDictionarySearchMessageAsync(message, mockMessageActions.Object, this._mockLogger.Object);

            // Assert
            await resultAction.Should().NotThrowAsync<Exception>();
            this._mockSearchManagementService.Verify(o=> o.ProcessDictionarySearchMessageAsync(It.IsAny<DictionarySearchBusMessage>()), Times.Once); 
        }

        [TestMethod]
        public async Task RunTimeFramedProcessDictionarySearchMessageAsyncThrowsTimeOutException()
        {
            // Arrange   
            var message = new DictionarySearchBusMessage(1);
            var wasActionOnTimeOutExceptionCalled = false;
            var wasActionOnFinallyCalled = false;
            var wasActionOnSuccessCalled = false;

            var actionOnTimeOutException = () => { wasActionOnTimeOutExceptionCalled = true; };
            var actionOnFinally = () => { wasActionOnFinallyCalled = true; };
            var actionOnSuccess = () => { wasActionOnSuccessCalled = true; };
            var operationTimeSpan = new TimeSpan(0, 0, 0, 0, 100);


            this._mockSearchManagementService.Setup(o => o.ProcessDictionarySearchMessageAsync(It.IsAny<DictionarySearchBusMessage>()))
                                            .Returns(Task.FromResult(message))
                                            .Callback(() => { Task.Delay(300).Wait(); })
                                            .Verifiable();


            // Act
            var webJob = new WebJobTestsHelper(this._mockSearchManagementService.Object, this._settings);
            var actionResult = () => webJob.TestRunTimeFramedProcessDictionarySearchMessageAsync(new DictionarySearchBusMessage(1),
                                                                                                actionOnSuccess,
                                                                                                actionOnTimeOutException,
                                                                                                actionOnFinally,
                                                                                                operationTimeSpan);

            // Assert
            await actionResult.Should().ThrowExactlyAsync<TimeoutException>();
            wasActionOnTimeOutExceptionCalled.Should().BeTrue();
            wasActionOnFinallyCalled.Should().BeTrue();
            wasActionOnSuccessCalled.Should().BeFalse();
        }

        [TestMethod]
        public async Task RunTimeFramedProcessDictionarySearchMessageAsyncCompletesSuccesfully()
        {
            // Arrange   
            var message = new DictionarySearchBusMessage(1);
            var wasActionOnTimeOutExceptionCalled = false;
            var wasActionOnFinallyCalled = false;
            var wasActionOnSuccessCalled = false;

            var actionOnTimeOutException = () => { wasActionOnTimeOutExceptionCalled = true; };
            var actionOnFinally = () => { wasActionOnFinallyCalled = true; };
            var actionOnSuccess = () => { wasActionOnSuccessCalled = true; };
            var operationTimeSpan = new TimeSpan(0, 0, 0, 0, 15);

            this._mockSearchManagementService.Setup(o => o.ProcessDictionarySearchMessageAsync(It.IsAny<DictionarySearchBusMessage>()))
                                            .Returns(Task.FromResult(message)) 
                                            .Verifiable();


            // Act
            var webJob = new WebJobTestsHelper(this._mockSearchManagementService.Object, this._settings);
            var actionResult = () => webJob.TestRunTimeFramedProcessDictionarySearchMessageAsync(new DictionarySearchBusMessage(1),
                                                                                                actionOnSuccess,
                                                                                                actionOnTimeOutException,
                                                                                                actionOnFinally,
                                                                                                operationTimeSpan);

            // Assert
            await actionResult.Should().NotThrowAsync();
            wasActionOnTimeOutExceptionCalled.Should().BeFalse();
            wasActionOnFinallyCalled.Should().BeTrue();
            wasActionOnSuccessCalled.Should().BeTrue();
        }

        [TestMethod]
        public async Task RunTimeFramedProcessDictionarySearchMessageAsyncThrowsAnyOtherException()
        {
            // Arrange    
            var wasActionOnTimeOutExceptionCalled = false;
            var wasActionOnFinallyCalled = false;
            var wasActionOnSuccessCalled = false;

            var actionOnTimeOutException = () => { wasActionOnTimeOutExceptionCalled = true; };
            var actionOnFinally = () => { wasActionOnFinallyCalled = true; };
            var actionOnSuccess = () => { wasActionOnSuccessCalled = true; };
            var operationTimeSpan = new TimeSpan(0, 0, 0, 0, 15);

            this._mockSearchManagementService.Setup(o => o.ProcessDictionarySearchMessageAsync(It.IsAny<DictionarySearchBusMessage>()))
                                            .ThrowsAsync(new DivideByZeroException()) 
                                            .Verifiable();


            // Act
            var webJob = new WebJobTestsHelper(this._mockSearchManagementService.Object, this._settings);
            var actionResult = () => webJob.TestRunTimeFramedProcessDictionarySearchMessageAsync(new DictionarySearchBusMessage(1),
                                                                                                actionOnSuccess,
                                                                                                actionOnTimeOutException,
                                                                                                actionOnFinally,
                                                                                                operationTimeSpan);

            // Assert
            await actionResult.Should().ThrowExactlyAsync<DivideByZeroException>();
            wasActionOnTimeOutExceptionCalled.Should().BeFalse();
            wasActionOnFinallyCalled.Should().BeTrue();
            wasActionOnSuccessCalled.Should().BeFalse();
        }

        [DataRow(45000, 15000)]
        [DataRow(30000, 15000)]
        [DataRow(240000, 210000)]
        [DataRow(300000, 270000)]
        [TestMethod]
        public void GetLockRenewalIntervalReturnsCorrectInterval(int intervalUntilNextArrival, int expectedRenewalInterval)
        {
            // Arrange
            var currentDateTimeOffset = DateTimeOffset.UtcNow;
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                                               new BinaryData(new DictionarySearchBusMessage(1))
                                               , lockedUntil: currentDateTimeOffset.AddMilliseconds(intervalUntilNextArrival));
              
            // Act
            var webJob = new WebJobTestsHelper(this._mockSearchManagementService.Object, this._settings);
            var result = webJob.TestGetLockRenewalInterval(currentDateTimeOffset, message);

            // Assert
            result.TotalMilliseconds.Should().Be(expectedRenewalInterval);
        }

        [TestMethod]
        public async Task RenewMessageLockAsyncCompletesSuccesfully()
        {
            // Arrange
            var intervalTimeSpan = new TimeSpan(0, 0, 0, 0, 200);
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                                               new BinaryData(new DictionarySearchBusMessage(1)));
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(new TimeSpan(0, 0, 0, 0, 800));
            var cancellationToken = cancellationTokenSource.Token;

            var mockMessageActions = new Mock<ServiceBusMessageActions>();
            mockMessageActions.Setup(o=>o.RenewMessageLockAsync(It.IsAny<ServiceBusReceivedMessage>()
                                                                , It.IsAny<CancellationToken>()))
                                    .Returns(Task.CompletedTask)
                                    .Verifiable();    
             
            // Act
            var webJob = new WebJobTestsHelper(this._mockSearchManagementService.Object, this._settings);
            ForgetTimingOperation(webJob.TestRenewMessageLockAsync(intervalTimeSpan
                                                                    , ()=> mockMessageActions.Object.RenewMessageLockAsync(message)
                                                                    , cancellationToken));
            await Task.Delay(1000);
            // Assert  
            mockMessageActions.Verify(o => o.RenewMessageLockAsync(It.IsAny<ServiceBusReceivedMessage>()
                                                                , It.IsAny<CancellationToken>()), Times.AtLeast(2));
        }

        [TestMethod]
        public async Task RenewMessageLockAsyncThrowsServiceBusException()
        {
            // Arrange
            var intervalTimeSpan = new TimeSpan(0, 0, 0, 0, 200);
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                                               new BinaryData(new DictionarySearchBusMessage(1)));
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(new TimeSpan(0, 0, 0, 0, 600));
            var cancellationToken = cancellationTokenSource.Token;

            var mockMessageActions = new Mock<ServiceBusMessageActions>();
            mockMessageActions.Setup(o => o.RenewMessageLockAsync(It.IsAny<ServiceBusReceivedMessage>()
                                                                , It.IsAny<CancellationToken>()))
                                    .ThrowsAsync(new ServiceBusException())
                                    .Verifiable();

            var wasServiceBusExceptionThrown = false;

            // Act

            var webJob = new WebJobTestsHelper(this._mockSearchManagementService.Object, this._settings);
            try
            {
                ForgetTimingOperation(webJob.TestRenewMessageLockAsync(intervalTimeSpan, () => mockMessageActions.Object.RenewMessageLockAsync(message)
                                                                        , cancellationToken));
            }
            catch(ServiceBusException)
            {
                wasServiceBusExceptionThrown = true;
            }

            await Task.Delay(1000);

            // Assert 
            wasServiceBusExceptionThrown.Should().BeFalse();   
        }


        [TestMethod]
        public async Task RenewMessageLockAsyncWhenCancelledRequestedThenItCompletedSuccesfully()
        {
            // Arrange
            var intervalTimeSpan = new TimeSpan(0, 0, 0, 0, 200);
            var message = ServiceBusModelFactory.ServiceBusReceivedMessage(
                                               new BinaryData(new DictionarySearchBusMessage(1)));
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(new TimeSpan(0, 0, 0, 0, 2000));
            var cancellationToken = cancellationTokenSource.Token;

            var mockMessageActions = new Mock<ServiceBusMessageActions>();
            mockMessageActions.Setup(o => o.RenewMessageLockAsync(It.IsAny<ServiceBusReceivedMessage>()
                                                                , It.IsAny<CancellationToken>()))
                                    .Returns(Task.CompletedTask)
                                    .Verifiable();

            // Act
            var webJob = new WebJobTestsHelper(this._mockSearchManagementService.Object, this._settings);
            ForgetTimingOperation(webJob.TestRenewMessageLockAsync(intervalTimeSpan
                                                                    , () => mockMessageActions.Object.RenewMessageLockAsync(message)
                                                                    , cancellationToken));
            cancellationTokenSource.Cancel();
            await Task.Delay(400);

            // Assert  

            mockMessageActions.Verify(o => o.RenewMessageLockAsync(It.IsAny<ServiceBusReceivedMessage>()
                                                                , It.IsAny<CancellationToken>()), Times.AtMost(1));
        }

        private static void ForgetTimingOperation(Task task)
        {
            task.ContinueWith(failedTask => { throw failedTask.Exception!; }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
