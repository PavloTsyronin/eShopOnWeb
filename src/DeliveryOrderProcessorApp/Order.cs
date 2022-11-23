using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeliveryOrderProcessorApp;
public class Order
{
    public string BuyerId { get; set; }
    public DateTimeOffset OrderDate { get; set; }
    public Address ShipToAddress { get; set; }

    public List<OrderItem> OrderItems { get; set; }

    public decimal Total()
    {
        var total = 0m;
        foreach (var item in OrderItems)
        {
            total += item.UnitPrice * item.Units;
        }
        return total;
    }
}

public class OrderItem
{
    public CatalogItemOrdered ItemOrdered { get; set; }
    public decimal UnitPrice { get; set; }
    public int Units { get; set; }
}

public class CatalogItemOrdered
{
    public int CatalogItemId { get; set; }
    public string ProductName { get; set; }
    public string PictureUri { get; set; }
}
