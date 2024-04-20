
using System.ComponentModel.DataAnnotations;

namespace RegistrationApi.Models
{
    public class User :EntityAbstact

    {
        [Required]
        public string Name { get; set; }
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }

        public User()
        {
            this.Id = Guid.NewGuid();
        }
    }
}
