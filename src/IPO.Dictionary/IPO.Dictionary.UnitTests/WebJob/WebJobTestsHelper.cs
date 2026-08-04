using Azure.Messaging.ServiceBus;
using IPO.Dictionary.Interfaces;
using IPO.Dictionary.Models.Configuration;
using IPO.Dictionary.Models.DictionarySearch; 
using IPO.Dictionary.WebJob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IPO.Dictionary.UnitTests.WebJob
{
    public class WebJobTestsHelper : Functions
    {
        public WebJobTestsHelper(IDictionarySearchManagementService dictionarySearchManagementService, Settings settings) : base(dictionarySearchManagementService, settings)
        {
        }


        public async Task TestRunTimeFramedProcessDictionarySearchMessageAsync(DictionarySearchBusMessage processDictionarySearchMessage,
                                                Action actionOnSuccess,
                                                Action actionOnTimeOutException,
                                                Action actionOnFinally,
                                                TimeSpan maximumOperationTimeSpan)
        {
            await base.RunTimeFramedProcessDictionarySearchMessageAsync(processDictionarySearchMessage
                                                                    , actionOnSuccess
                                                                    , actionOnTimeOutException
                                                                    , actionOnFinally
                                                                    , maximumOperationTimeSpan);
        }

        public TimeSpan TestGetLockRenewalInterval(DateTimeOffset currentDateTimeOffset, ServiceBusReceivedMessage message)
        {
            return base.GetLockRenewalInterval(currentDateTimeOffset, message);
        }

        public async Task TestRenewMessageLockAsync(TimeSpan timeSpan, Action messageRenewalAction, CancellationToken token)
        {
            await base.RenewMessageLockAsync(timeSpan, messageRenewalAction, token);
        } 
    }
}
