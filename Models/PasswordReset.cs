using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BestStoreApi.Models
{
    [Index("Email", IsUnique = true)]
    public class PasswordReset
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public string Email { get; set; } = "";

        [MaxLength(100)]
        public string Token { get; set; } = "";

        public DateTime ExpirationDate { get; set; } = DateTime.UtcNow;
    }
}
