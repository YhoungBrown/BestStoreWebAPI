using System.ComponentModel.DataAnnotations;

namespace BestStoreApi.Models
{
    public class OrderDto
    {
        [Required]
        public string productIdentifier { get; set; } = "";
        
        [Required, MinLength(30), MaxLength(100)]
        public string deliveryAddress { get; set; } = "";

        [Required]
        public string paymentMethod { get; set; } = "";
    }
}
