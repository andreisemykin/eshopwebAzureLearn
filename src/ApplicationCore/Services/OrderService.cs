using Ardalis.GuardClauses;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrdersProcessing;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OrderItem = Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate.OrderItem;

namespace Microsoft.eShopWeb.ApplicationCore.Services
{
    public class OrderService : IOrderService
    {
        private readonly IAsyncRepository<Order> _orderRepository;
        private readonly IUriComposer _uriComposer;
        private readonly IAsyncRepository<Basket> _basketRepository;
        private readonly IAsyncRepository<CatalogItem> _itemRepository;
        private readonly HttpClient _httpClient;
        private readonly OrderItemsDeliveryServiceSettings _orderItemsDeliveryServiceSettings;

        public OrderService(IAsyncRepository<Basket> basketRepository,
            IAsyncRepository<CatalogItem> itemRepository,
            IAsyncRepository<Order> orderRepository,
            IUriComposer uriComposer,
            HttpClient httpClient,
            IOptions<OrderItemsDeliveryServiceSettings> orderItemsDeliveryServiceSettings)
        {
            _orderRepository = orderRepository;
            _uriComposer = uriComposer;
            _basketRepository = basketRepository;
            _itemRepository = itemRepository;
            _httpClient = httpClient;
            _orderItemsDeliveryServiceSettings = orderItemsDeliveryServiceSettings.Value;
        }

        public async Task CreateOrderAsync(int basketId, Address shippingAddress)
        {
            var basketSpec = new BasketWithItemsSpecification(basketId);
            var basket = await _basketRepository.FirstOrDefaultAsync(basketSpec);

            Guard.Against.NullBasket(basketId, basket);
            Guard.Against.EmptyBasketOnCheckout(basket.Items);

            var catalogItemsSpecification = new CatalogItemsSpecification(basket.Items.Select(item => item.CatalogItemId).ToArray());
            var catalogItems = await _itemRepository.ListAsync(catalogItemsSpecification);

            var items = basket.Items.Select(basketItem =>
            {
                var catalogItem = catalogItems.First(c => c.Id == basketItem.CatalogItemId);
                var itemOrdered = new CatalogItemOrdered(catalogItem.Id, catalogItem.Name, _uriComposer.ComposePicUri(catalogItem.PictureUri));
                var orderItem = new OrderItem(itemOrdered, basketItem.UnitPrice, basketItem.Quantity);
                return orderItem;
            }).ToList();

            var order = new Order(basket.BuyerId, shippingAddress, items);

            await _orderRepository.AddAsync(order);

            var orderDelivery = new OrderAggregate {
                ShippingAddress = order.ShipToAddress,
                OrderItems = order.OrderItems.Select(x => new OrderInfoItem 
                {
                    UnitPrice = x.UnitPrice,
                    Units = x.Units
                }).ToArray(),
                FinalPrice = order.Total() 
            };
            string content = JsonSerializer.Serialize(orderDelivery);
            using (HttpContent jsonContent = new StringContent(content, Encoding.UTF8, "application/json"))
            {
                var result = await _httpClient.PostAsync(_orderItemsDeliveryServiceSettings.OrderItemsDeliveryServiceUrl, jsonContent);
            }
        }
    }
}
