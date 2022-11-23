using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Microsoft.eShopWeb.ApplicationCore.Services;
public class ReservationService : IReservationService
{
    private readonly string _serviceBusConnectionString;
    const string queueName = "orderitems";

    public ReservationService(IConfiguration configuration)
    {
        _serviceBusConnectionString = configuration["ReservationServiceBusConnectionString"];
    }

    public async Task ReserveOrder(Order order)
    {
        var reservedItems = order.OrderItems.Select(oi => new ReservedItem
        {
            ItemId = oi.ItemOrdered.CatalogItemId,
            Quantity = oi.Units
        });

        await using var client = new ServiceBusClient(_serviceBusConnectionString);
        await using ServiceBusSender sender = client.CreateSender(queueName);

        foreach (ReservedItem item in reservedItems)
        {
            var serializeditem = JsonSerializer.Serialize(item);

            try
            {
                var message = new ServiceBusMessage(serializeditem);
                await sender.SendMessageAsync(message);
            }
            catch (Exception exception)
            {
                //Process failure
                throw;
            }
        }
    }
}

public class ReservedItem
{
    public int ItemId { get; set; }

    public int Quantity { get; set; }
}
