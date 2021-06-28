using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace OrderItemsReserver
{
    public static class OrderItemsReserver
    {
        private const int RetryCount = 3;
        private const string RetryDelayInterval = "00:01:02";

        [FunctionName("OrderItemsReserver")]
        [FixedDelayRetry(RetryCount, RetryDelayInterval)]
        public static async Task Run([ServiceBusTrigger("itemreserver", Connection = "ServiceBusConnectionString")]string queueItem, ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {queueItem}");

            var data = JsonConvert.DeserializeObject<OrderDetail[]>(queueItem);
            
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("BlobStorageConnectionString"));
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(Environment.GetEnvironmentVariable("BlobContainerName"));
            CloudBlockBlob blockBlob = container.GetBlockBlobReference($"{Guid.NewGuid()}.json");
            await blockBlob.UploadTextAsync(JsonConvert.SerializeObject(data));
        }

        internal class OrderDetail
        {
            public int ItemId { get; set; }
            public int Quantity { get; set; }
        }
    }
}
