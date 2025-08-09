using System.ComponentModel.DataAnnotations;

namespace BestStoreApi.Models
{
    public class UserProfileUpdateDto
    {
        [Required, MaxLength(100)]
        public string FirstName { get; set; } = "";

        [Required, MaxLength(100)]
        public string LastName { get; set; } = "";

        [Required, EmailAddress, MaxLength(100)]
        public string Email { get; set; } = ""; //making it unique in the db with Index attribute on top of the class

        [MaxLength(20)]
        public string? Phone { get; set; }

        [Required, MaxLength(100)]
        public string Address { get; set; } = "";
    }
}
