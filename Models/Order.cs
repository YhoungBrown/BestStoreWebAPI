using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BestStoreApi.Models
{
    public class Order
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public DateTime CreatedAt { get; set; }

        [Precision(18, 2)]
        public decimal shippingFee { get; set; }

        [MaxLength(100)]
        public string deliveryAddress { get; set; } = "";

        [MaxLength(30)]
        public string paymentMethod { get; set; } = "";

        [MaxLength(30)]
        public string paymentStatus { get; set; } = "";

        [MaxLength(30)]
        public string orderStatus { get; set; } = "";

        //navigational Properties

        public User User { get; set; } = null!;

        public List<OrderItem> OrderItems { get; set; } = new ();
    }
}
