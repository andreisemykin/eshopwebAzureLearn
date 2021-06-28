namespace Microsoft.eShopWeb
{
    public class OrderItemsReserverSettings
    {
        public string ServiceBusConnectionString { get; set; }
        public string QueueName { get; set; }
    }
}
