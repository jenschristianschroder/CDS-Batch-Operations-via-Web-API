using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;

namespace WebApiExample
{
    class BatchRequest
    {

        static void Main(string[] args)
        {
            BatchRequest app = new BatchRequest();

            Task.WaitAll(Task.Run(async () => await app.SendBatchRequest(1)));
            Task.WaitAll(Task.Run(async () => await app.SendSingleRequests(1)));


            Console.WriteLine("numberOfRequests; Batch; Single");
            int numberOfRequests = 1;
            while (numberOfRequests <= 500)
            {
                Task.WaitAll(Task.Run(async () => await app.SendBatchRequest(numberOfRequests)));
                Task.WaitAll(Task.Run(async () => await app.SendSingleRequests(numberOfRequests)));
                if (numberOfRequests < 10)
                    numberOfRequests++;
                else if (numberOfRequests < 50)
                    numberOfRequests = numberOfRequests + 5;
                else if (numberOfRequests < 100)
                    numberOfRequests = numberOfRequests + 10;
                else if (numberOfRequests < 200)
                    numberOfRequests = numberOfRequests + 20;
                else
                    numberOfRequests = numberOfRequests + 50;
            }
            Console.ReadLine();
        }

        public async Task<HttpResponseMessage> SendBatchRequest(int numberOfRequests)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();


            string accessToken = await GetAccessToken();

            var appSettings = ConfigurationManager.AppSettings;
            string apiUrl = appSettings["apiUrl"];

            HttpClient client = new HttpClient();

            //Init Batch
            string batchName = $"batch_{Guid.NewGuid()}";
            MultipartContent batchContent = new MultipartContent("mixed", batchName);

            string changesetName = $"changeset_{Guid.NewGuid()}";
            MultipartContent changesetContent = new MultipartContent("mixed", changesetName);

            HttpRequestMessage requestMessage;
            HttpMessageContent messageContent;

            for (int i = 0; i < numberOfRequests; i++)
            {
                JObject record = new JObject();

                //Create first request - Create new Contact
                record.Add("firstname", "Jane");
                record.Add("lastname", "Doe");

                requestMessage = new HttpRequestMessage(HttpMethod.Post, apiUrl + "contacts");
                messageContent = new HttpMessageContent(requestMessage);
                messageContent.Headers.Remove("Content-Type");
                messageContent.Headers.Add("Content-Type", "application/http");
                messageContent.Headers.Add("Content-Transfer-Encoding", "binary");

                StringContent stringContent = new StringContent(record.ToString());
                stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json;type=entry");
                requestMessage.Content = stringContent;
                messageContent.Headers.Add("Content-ID", (i + 1).ToString());

                changesetContent.Add(messageContent);
            }

            batchContent.Add(changesetContent);

            //Create batch request
            HttpRequestMessage batchRequest = new HttpRequestMessage(HttpMethod.Post, apiUrl + "$batch");

            batchRequest.Content = batchContent;
            batchRequest.Headers.Add("Prefer", "odata.include-annotations=\"OData.Community.Display.V1.FormattedValue\"");
            batchRequest.Headers.Add("OData-MaxVersion", "4.0");
            batchRequest.Headers.Add("OData-Version", "4.0");
            batchRequest.Headers.Add("Accept", "application/json");
            batchRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            //Execute Batch request
            HttpResponseMessage response = await client.SendAsync(batchRequest);

            MultipartMemoryStreamProvider body = await response.Content.ReadAsMultipartAsync();

            //Output result
            //Console.WriteLine($"Batch Request Result:\n********************************************************\n {await response.Content.ReadAsStringAsync()}");

            sw.Stop();
            //Output result
            Console.Write($"\n{numberOfRequests}; {sw.ElapsedMilliseconds}");
            return response;
        }

        public async Task<HttpResponseMessage> SendSingleRequests(int numberOfRequests)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            string accessToken = await GetAccessToken();

            var appSettings = ConfigurationManager.AppSettings;
            string apiUrl = appSettings["apiUrl"];

            HttpClient client = new HttpClient();
            HttpResponseMessage response = null;
            for (int i = 0; i < numberOfRequests; i++)
            {
                JObject record = new JObject();

                //Create first request - Create new Contact
                record.Add("firstname", "Jane");
                record.Add("lastname", "Doe");

                HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, apiUrl + "contacts");
                HttpMessageContent messageContent = new HttpMessageContent(requestMessage);
                messageContent.Headers.Remove("Content-Type");
                messageContent.Headers.Add("Content-Type", "application/http");
                messageContent.Headers.Add("Content-Transfer-Encoding", "binary");
                StringContent stringContent = new StringContent(record.ToString());
                stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json;type=entry");
                requestMessage.Content = stringContent;

                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                await client.SendAsync(requestMessage);
            }
            sw.Stop();
            //Output result
            Console.Write($"; {sw.ElapsedMilliseconds} ms");

            return response;
        }

        private static async Task<string> GetAccessToken()
        {
            var appSettings = ConfigurationManager.AppSettings;

            String clientId = appSettings["clientId"];
            String secret = appSettings["secret"];
            String tenantId = appSettings["tenantId"];
            String resourceUrl = appSettings["resourceUrl"];

            var credentials = new ClientCredential(clientId, secret);
            var authContext = new AuthenticationContext("https://login.microsoftonline.com/" + tenantId);
            var result = await authContext.AcquireTokenAsync(resourceUrl, credentials);

            return result.AccessToken;
        }
    }
}