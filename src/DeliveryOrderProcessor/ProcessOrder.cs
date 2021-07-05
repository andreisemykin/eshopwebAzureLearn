using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrdersProcessing;

namespace DeliveryOrderProcessor
{
    public static class ProcessOrder
    {
        private const string DatabaseName = "ReservedOrders";
        private const string CollectionName = "Orders";

        [FunctionName(nameof(ProcessOrder))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            OrderAggregate order = JsonConvert.DeserializeObject<OrderAggregate>(requestBody);

            using (var client = new DocumentClient(new Uri(Environment.GetEnvironmentVariable("cosmosDbServiceUri")), Environment.GetEnvironmentVariable("accountKey")))
            {
                await client.CreateDatabaseIfNotExistsAsync(new Database { Id = DatabaseName });

                await client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(DatabaseName), new DocumentCollection { Id = CollectionName });

                await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionName), order);

            }

            return new OkObjectResult("Ok");
        }

        //class OrderAggregate
        //{
        //    public string ShippingAddress { get; set; }
        //    public OrderItem[] OrderItems { get; set; }
        //    public decimal FinalPrice { get; set; }
        //}

    }
}
