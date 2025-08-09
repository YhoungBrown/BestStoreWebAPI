using Microsoft.EntityFrameworkCore;

namespace BestStoreApi.Models
{
    public class OrderItem
    {
        public int id { get; set; } 

        public int OrderId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        [Precision(18, 2)]
        public decimal UnitPrice { get; set; }

        //navigation Property

        public Order Order { get; set; } = null!;

        public Product Product { get; set; } = null!;
    }
}
