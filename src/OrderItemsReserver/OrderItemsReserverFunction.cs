using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace OrderItemsReserver
{
    public static class OrderItemsReserverFunction
    {
        [FunctionName("OrderItemsReserver")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation("C# HTTP trigger function processed a request.");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var data = JsonConvert.DeserializeObject<OrderDetail[]>(requestBody);
                if (data.Length == 0)
                {
                    return new BadRequestObjectResult("No data in request body");
                }
                string responseMessage = JsonConvert.SerializeObject(data);

                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("StorageConnectionString"));
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer container = blobClient.GetContainerReference(Environment.GetEnvironmentVariable("ContainerName"));
                CloudBlockBlob blockBlob = container.GetBlockBlobReference($"{Guid.NewGuid()}.json");
                await blockBlob.UploadTextAsync(responseMessage);

                return new OkObjectResult(responseMessage);
            }
            catch (Exception ex)
            {
                return new OkObjectResult(ex.GetType() + " " + ex.Message);
            }
        }

        internal class OrderDetail
        {
            public int ItemId { get; set; }
            public int Quantity { get; set; }
        }
    }
}
