using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;

namespace Microsoft.eShopWeb.ApplicationCore.Entities.OrdersProcessing
{
    public class OrderAggregate
    {
        public Address ShippingAddress { get; set; }
        public OrderInfoItem[] OrderItems { get; set; }
        public decimal FinalPrice { get; set; }
    }
}
