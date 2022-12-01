using System;
using System.Net.Http;
using System.Text;
using Google.Maps;
using Google.Maps.Direction;
using Google.Maps.DistanceMatrix;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;

namespace TravelTimeCron
{
    public class Function1
    {
        /// <summary>
        /// this is a cron job function which will be triggered every working day in the moring to tell my self the travel time from home to work
        /// </summary>
        private static readonly HttpClient client = new HttpClient();
        private static LatLng homeLocation = new LatLng(52.117589, 4.644064);
        private static LatLng workLocation = new LatLng(51.990251, 4.389423);

        [FunctionName("Function1")]
        public void Run([TimerTrigger("0 20 6 * * 1-5", RunOnStartup = true, UseMonitor = true)] TimerInfo myTimer, ILogger log)
        {
            var logicappKey = Environment.GetEnvironmentVariable("LogicAppKey") == null ? "" : Environment.GetEnvironmentVariable("LogicAppKey");
            var logicappConnection = $"https://prod-171.westeurope.logic.azure.com:443/workflows/a899975a14794f3e8051d5b24ec3f9d5/triggers/manual/paths/invoke?api-version=2016-10-01&sp=%2Ftriggers%2Fmanual%2Frun&sv=1.0&sig={logicappKey}";
            var apikey = Environment.GetEnvironmentVariable("GoogleApiKey") == null ? "" : Environment.GetEnvironmentVariable("GoogleApiKey");
            GoogleSigned.AssignAllServices(new GoogleSigned(apikey));

            var request = new DistanceMatrixRequest();

            request.AddOrigin(homeLocation);
            request.AddDestination(workLocation);
            request.DepartureTime = (int?)DateTimeOffset.Now.ToUnixTimeSeconds();
            request.Units = Units.metric;
            request.Mode = TravelMode.driving;
            
            var response = new DistanceMatrixService().GetResponse(request);

            log.LogInformation("");
            var logicAppPayload = new LogicAppRequest();
            if (response.Status == ServiceResponseStatus.Ok && response.Rows.Length > 0)
            {
                var result = response.Rows[0];
                var duration = result.Elements[0]?.duration;
                var distance = result.Elements[0]?.distance;
                log.LogInformation("Duration:" + duration);
                log.LogInformation("Distance:" + distance);

                // prepate the request to logic app
                logicAppPayload.Totaltime = $"Total time:{duration}, Distance {distance}";
                logicAppPayload.Email = "zheliu@outlook.com";
                logicAppPayload.Task = "TravelTime from home to work now";    

            }
            else
            {
                log.LogError($"Failed to get direction information from Google API with timerjob{myTimer}");
            }

            // send request to logic app
            var retryPolicy = Policy.Handle<Exception>().WaitAndRetry(retryCount: 3, sleepDurationProvider: _ => TimeSpan.FromSeconds(2));

            if (!string.IsNullOrEmpty(logicAppPayload.Totaltime))
            {
                var attempt = 0;
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
}
