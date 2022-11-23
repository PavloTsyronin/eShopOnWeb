using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System.Configuration;

namespace DeliveryOrderProcessorApp;

public static class DeliveryOrderProcessingFunction
{
    static IConfiguration _configuration;

    [FunctionName("DeliveryOrderProcessingFunction")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        ILogger log,
        ExecutionContext context)
    {
        _configuration = GetAppConfiguration(context);

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        Order order = JsonConvert.DeserializeObject<Order>(requestBody);

        await CreateOrder(order);

        string responseMessage = $"This HTTP triggered function executed successfully.";

        return new OkObjectResult(responseMessage);
    }

    private static IConfiguration GetAppConfiguration(ExecutionContext context)
    {
        return new ConfigurationBuilder()
            .SetBasePath(context.FunctionAppDirectory)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
    }


    private static async Task CreateOrder(Order order)
    {
        var connectionString = _configuration["CosmosDbConnectionString"];
        var databaseName = "DeliveryOrdersDB";

        CosmosClient client = new CosmosClient(connectionString);
        Database database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
        Container container = await database.CreateContainerIfNotExistsAsync(
            "DeliveryOrders", "/shippingAddress/country", 400);

        dynamic testItem = new
        {
            id = Guid.NewGuid().ToString(),
            shippingAddress = new
            {
                street = order.ShipToAddress.Street,
                city = order.ShipToAddress.City,
                state = order.ShipToAddress.State,
                country = order.ShipToAddress.Country,
                zipCode = order.ShipToAddress.ZipCode,
            },
            listOfItems = order.OrderItems.Select(oi => new
            {
                catalogItemId = oi.ItemOrdered.CatalogItemId,
                units = oi.Units
            }
            ),
            finalPrice = order.Total()
        };
        await container.CreateItemAsync(testItem);
    }
}
