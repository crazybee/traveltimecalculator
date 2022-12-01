using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;
using Polly;
using System;
using TraveltimeCalculator.Models;

namespace TravelTimeFunction
{
    public static class Function1
    {
        /// <summary>
        /// this is the function app to make an api call to azure map api and get the current travel time from home to work or the other way around
        /// in the end, an additional request to auzre logic app will be triggred to send myself an email containing the travel time 
        /// </summary>
        private static readonly HttpClient client = new HttpClient();
        private static string homeLocation = "52.117589,4.644064"; // home coordinates
        private static string workLocation = "51.990251,4.389423"; // work coordinates

        [FunctionName("TraveltimeCalculatorFunc")]
        public static void Run([ServiceBusTrigger("traffictimequeue", Connection = "ServiceBusConnectionString")] TravelTimeRequest requestPayload, ILogger log)
        {
            var requestString = "";
            var logicappKey = Environment.GetEnvironmentVariable("LogicAppKey") == null ? "" : Environment.GetEnvironmentVariable("LogicAppKey");
            var azureMapKey = Environment.GetEnvironmentVariable("AzureMapKey") == null ? "" : Environment.GetEnvironmentVariable("AzureMapKey");

            var logicappConnection = $"https://prod-144.westeurope.logic.azure.com:443/workflows/7b1b3ee5917047b1ae5cb9563015023d/triggers/manual/paths/invoke?api-version=2016-10-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig={logicappKey}";
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {requestPayload}");
            string targetEmail = string.IsNullOrEmpty(requestPayload.Email) ? "zheliu@outlook.com" : requestPayload.Email;
            if (requestPayload.FromHomeToWork)
            {
                requestString = $"https://atlas.microsoft.com/route/directions/json?api-version=1.0&query={homeLocation}:{workLocation}&report=effectiveSettings&subscription-key={azureMapKey}";
            }
            else
            {
                requestString = $"https://atlas.microsoft.com/route/directions/json?api-version=1.0&query={workLocation}:{homeLocation}&report=effectiveSettings&subscription-key={azureMapKey}";
            }

            int timeInMinutes;

            using (var httpclient = new HttpClient())
            {
                var result = client.GetAsync(requestString).GetAwaiter().GetResult();
                var rawStringData = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var rawData = (JObject)JsonConvert.DeserializeObject(rawStringData);
                // get time from object
                var timeInSeconds = rawData["routes"][0]["summary"]["travelTimeInSeconds"];
                timeInMinutes = int.Parse(timeInSeconds.ToString()) / 60;
            }
               
            // prepate the request to logic app
            var logicAppPayload = new LogicAppRequest();
            logicAppPayload.totaltime = timeInMinutes.ToString() + "minutes";
            logicAppPayload.email = targetEmail;
            logicAppPayload.task = requestPayload.FromHomeToWork ? "TravelTime from home to work now" : "TravelTime from work to home now";


            // send request to logic app
            var retryPolicy = Policy.Handle<Exception>().WaitAndRetry(retryCount: 3, sleepDurationProvider: _ => TimeSpan.FromSeconds(2));
           
            var attempt = 0;
            // we need to make sure the call is successful 
            retryPolicy.Execute(() =>
            {
                log.LogInformation($"in retry , retry count {++attempt}");

                using var requestContent = new StringContent(JsonConvert.SerializeObject(logicAppPayload), Encoding.UTF8, "application/json");
                using var response = client.PostAsync(logicappConnection, requestContent).GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception();
                }
                else
                {
                    log.LogInformation($"C# request successfully made toward the logic app with the status code: {response.StatusCode}");
                }
            });
        }
    }
}
