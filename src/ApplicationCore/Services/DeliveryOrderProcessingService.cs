using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Microsoft.eShopWeb.ApplicationCore.Services;
public class DeliveryOrderProcessingService : IDeliveryOrderProcessingService
{
    private readonly string _processingFunctionUri;

    public DeliveryOrderProcessingService(IConfiguration configuration)
    {
        _processingFunctionUri = configuration["DeliveryOrderProcessingFunctionUri"];
    }

    public async Task ProcessDeliveryOrder(Order order)
    {
        var serializedOrder = JsonSerializer.Serialize(order);

        var httpClient = new HttpClient();
        var content = new StringContent(serializedOrder, Encoding.UTF8, "application/json");
        try
        {
            var result = await httpClient.PostAsync(_processingFunctionUri, content);
        }
        catch (Exception)
        {
            //Process failure
            throw;
        }
    }
}
