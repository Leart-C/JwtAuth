using System.ComponentModel.DataAnnotations;

namespace JwtAuth.Core.Dto
{
    public class Login
    {
        [Required(ErrorMessage = "UserName is Required")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Password is Required")]
        public string Password { get; set; }
    }
}
