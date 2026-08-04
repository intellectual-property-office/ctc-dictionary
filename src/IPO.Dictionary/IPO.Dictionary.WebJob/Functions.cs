using Azure.Messaging.ServiceBus;
using IPO.Dictionary.Interfaces;
using IPO.Dictionary.Models.Configuration;
using IPO.Dictionary.Models.DictionarySearch; 
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;

namespace IPO.Dictionary.WebJob
{
    public class Functions
    {
        private readonly Settings _settings;

        public IDictionarySearchManagementService DictionarySearchManagementService { get; }

        public Functions(IDictionarySearchManagementService dictionarySearchManagementService, Settings settings)
        {
            DictionarySearchManagementService = dictionarySearchManagementService;
            _settings = settings;
        }

        public async Task ProcessDictionarySearchMessageAsync([ServiceBusTrigger("%ServiceBusTopicName%", "%ServiceBusSubscriptionName%", Connection = "ServiceBusConnectionString")]
                                                            ServiceBusReceivedMessage message,
                                                            ServiceBusMessageActions messageActions,
                                                            ILogger logger)
        {
            var timeMessageOffSetConsumed = DateTimeOffset.UtcNow;
            logger.LogInformation("Message received from service bus to dictionary search:" + message);

            var processDictionarySearchMessage = JsonSerializer.Deserialize<DictionarySearchBusMessage>(message.Body);

            if (processDictionarySearchMessage!.Type != DictionarySearchBusMessage.messageType)
            {
                throw new InvalidOperationException($"The message is not routed correctly, it will eventually will get dead-lettered. Expected type: {DictionarySearchBusMessage.messageType}, received type : {processDictionarySearchMessage.Type}");
            }

            logger.LogInformation($"Dictionary search process started for file:{processDictionarySearchMessage.FileId}");

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(new TimeSpan(0, 0, _settings.MaximumOperationTime));
            var cancellationToken = cancellationTokenSource.Token;
              
            ForgetTimingOperation( Task.Run(async () => await RenewMessageLockAsync(GetLockRenewalInterval(timeMessageOffSetConsumed, message)
                            , () => messageActions.RenewMessageLockAsync(message)
                            , cancellationToken)), (Exception failedException, int fileId) =>
                            {
                                logger.LogError("The dictionary search operation timing process failed. File id: {fileId} {failedException}", fileId, failedException);
                            }, processDictionarySearchMessage.FileId);

            await RunTimeFramedProcessDictionarySearchMessageAsync(
                processDictionarySearchMessage
                , actionOnSuccess: async () => {
                    await messageActions.CompleteMessageAsync(message); 
                }
                , actionOnTimeOutException: () =>
                {
                    logger.LogError("The dictionary search operation timed out after {_settings.MaximumOperationTime}. FileId: {processDictionarySearchMessage.FileId}",
                    _settings.MaximumOperationTime, processDictionarySearchMessage.FileId);
                }
                , actionOnFinally: () =>
                {
                    cancellationTokenSource.Cancel();
                }
                , new TimeSpan(0, 0, _settings.MaximumOperationTime));

            logger.LogInformation($"Dictionary search process completed for file:{processDictionarySearchMessage.FileId}");
        }

        protected void ForgetTimingOperation(Task task, Action<Exception, int> failedOperationAction, int fileId)
        {
            task.ContinueWith(failedTask => { failedOperationAction(failedTask.Exception!, fileId); }, TaskContinuationOptions.OnlyOnFaulted);
        }

        protected async Task RunTimeFramedProcessDictionarySearchMessageAsync(DictionarySearchBusMessage processDictionarySearchMessage,
                                                Action actionOnSuccess,
                                                Action actionOnTimeOutException,
                                                Action actionOnFinally,
                                                TimeSpan maximumOperationTimeSpan)
        {
            try
            {
                var task = Task.Run(async () => await DictionarySearchManagementService.ProcessDictionarySearchMessageAsync(processDictionarySearchMessage));

                await task.WaitAsync(maximumOperationTimeSpan);
                 
                actionOnSuccess();    
            }
            catch (TimeoutException)
            {
                actionOnTimeOutException();
                throw;
            } 
            finally
            {
                actionOnFinally();
            }

        }
        protected TimeSpan GetLockRenewalInterval(DateTimeOffset currentDateTimeOffset, ServiceBusReceivedMessage message)
        {
            var intervalInMs = Convert.ToInt32((message.LockedUntil - currentDateTimeOffset).TotalMilliseconds); 

            if (intervalInMs <= 45000)
                return new TimeSpan(0, 0, 0, 0, 15000);

            return new TimeSpan(0, 0, 0, 0, (intervalInMs - 30000)); 
        }

        protected async Task RenewMessageLockAsync(TimeSpan timeSpan, Action messageRenewalAction, CancellationToken token)
        {
            var messageLockingTimer = new PeriodicTimer(timeSpan);

            while (await messageLockingTimer.WaitForNextTickAsync(token))
            {
                if (!token.IsCancellationRequested)
                {
                    try
                    {
                        messageRenewalAction();
                    }
                    catch (ServiceBusException)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }

    }
}
