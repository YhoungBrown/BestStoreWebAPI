using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace BestStoreApi.Models
{
    [Index("Email", IsUnique = true)]
    public class User
    {
        public static ClaimsIdentity Identity { get; internal set; }
        public int Id { get; set; }

        [MaxLength(100)]
        public string FirstName { get; set; } = "";

        [MaxLength(100)]
        public string LastName { get; set; } = "";

        [MaxLength(100)]
        public string Email { get; set; } = ""; //making it unique in the db with Index attribute on top of the class

        [MaxLength(20)]
        public string Phone { get; set; } = "";

        [MaxLength(100)]
        public string Address { get; set; } = "";

        [MaxLength(100)]
        public string Password { get; set; } = "";

        [MaxLength(20)]
        public string Role { get; set; } = ""; 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
