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

            Task.WaitAll(Task.Run(async () => await app.SendBatchRequest()));

            Console.ReadLine();
        }

        public async Task<HttpResponseMessage> SendBatchRequest()
        {
            JObject record = new JObject();

            string accessToken = await GetAccessToken();

            var appSettings = ConfigurationManager.AppSettings;
            string apiUrl = appSettings["apiUrl"];

            HttpClient client = new HttpClient();

            //Init Batch
            string batchName = $"batch_{Guid.NewGuid()}";
            MultipartContent batchContent = new MultipartContent("mixed", batchName);

            string changesetName = $"changeset_{Guid.NewGuid()}";
            MultipartContent changesetContent = new MultipartContent("mixed", changesetName);

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
            messageContent.Headers.Add("Content-ID", "1");
            
            changesetContent.Add(messageContent);

            //Create second request - Create new Contact
            record = new JObject();
            record.Add("firstname", "John");
            record.Add("lastname", "Doe");

            requestMessage = new HttpRequestMessage(HttpMethod.Post, apiUrl + "contacts");
            messageContent = new HttpMessageContent(requestMessage);
            messageContent.Headers.Remove("Content-Type");
            messageContent.Headers.Add("Content-Type", "application/http");
            messageContent.Headers.Add("Content-Transfer-Encoding", "binary");

            stringContent = new StringContent(record.ToString());
            stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json;type=entry");
            requestMessage.Content = stringContent;
            messageContent.Headers.Add("Content-ID", "2");

            changesetContent.Add(messageContent);

            batchContent.Add(changesetContent);

            //Create third request - Retrieve contacts
            requestMessage = new HttpRequestMessage(HttpMethod.Get, apiUrl + "contacts?$select=firstname, lastname&$filter=firstname eq 'Jane' or firstname eq 'John'");

            messageContent = new HttpMessageContent(requestMessage);
            messageContent.Headers.Remove("Content-Type");
            messageContent.Headers.Add("Content-Type", "application/http");
            messageContent.Headers.Add("Content-Transfer-Encoding", "binary");

            requestMessage.Headers.Add("Accept", "application/json");

            batchContent.Add(messageContent);

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
            Console.WriteLine($"Batch Request Result:\n********************************************************\n {await response.Content.ReadAsStringAsync()}");

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