namespace Microsoft.eShopWeb
{
    public class OrderItemReserverSettings
    {
        public string ServiceBusConnectionString { get; set; }
        public string QueueName { get; set; }
    }
}
