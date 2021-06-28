using Ardalis.GuardClauses;
using Microsoft.Azure.ServiceBus;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.eShopWeb.ApplicationCore.Services
{
    public class OrderService : IOrderService
    {
        private readonly IAsyncRepository<Order> _orderRepository;
        private readonly IUriComposer _uriComposer;
        private readonly OrderItemsReserverSettings _orderItemsReserverSettings;
        private readonly IAsyncRepository<Basket> _basketRepository;
        private readonly IAsyncRepository<CatalogItem> _itemRepository;
        private readonly HttpClient _httpClient;

        public OrderService(IAsyncRepository<Basket> basketRepository,
            IAsyncRepository<CatalogItem> itemRepository,
            IAsyncRepository<Order> orderRepository,
            IUriComposer uriComposer,
            HttpClient httpClient,
            IOptions<OrderItemsReserverSettings> orderItemsReserverSettings)
        {
            _orderRepository = orderRepository;
            _uriComposer = uriComposer;
            _orderItemsReserverSettings = orderItemsReserverSettings.Value;
            _basketRepository = basketRepository;
            _itemRepository = itemRepository;
            _httpClient = httpClient;
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

            await SendRserverOrderMessageAsync(items);
        }

        private async Task SendRserverOrderMessageAsync(List<OrderItem> items)
        {
            var orderDetails = items.Select(item => new { ItemId = item.Id, Quantity = item.Units }).ToArray();
            byte[] messageBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(orderDetails));
            var queueClient = new QueueClient(_orderItemsReserverSettings.ServiceBusConnectionString, _orderItemsReserverSettings.QueueName);

            var message = new Message(messageBytes);

            await queueClient.SendAsync(message);

            await queueClient.CloseAsync();
        }
    }
}
