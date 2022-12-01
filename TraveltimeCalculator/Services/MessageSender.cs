using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using Polly;
using System;
using System.Threading.Tasks;
using TraveltimeCalculator.Models;

namespace TraveltimeCalculator.Services
{
    public class MessageSender : IMessageSender
    {   
        private ServiceBusSender sbsender;
        // number of messages to be sent to the queue
        private const int numOfRetries = 3;

        public MessageSender(ServiceBusSender sbsender)
        {
            this.sbsender = sbsender;
        }

        public async Task<bool> SendTravelTimeRequestAsync(TravelTimeRequest request)
        {
            // This is where you handle the form submit from the form post.
            var retryPolicy = Policy.Handle<Exception>().WaitAndRetry(retryCount: numOfRetries, sleepDurationProvider: _ => TimeSpan.FromSeconds(1));
            var isSuccessful = false;
            await retryPolicy.Execute(async () =>
            {
                try
                {

                    var requestPayload = JsonConvert.SerializeObject(request);
                    await this.sbsender.SendMessageAsync(new ServiceBusMessage(requestPayload));
                    isSuccessful = true;
                }
                catch (Exception e)
                {
                    isSuccessful = false;
                    throw e;
                }
            });

            return isSuccessful;

        }
    }
}
