using System.ComponentModel.DataAnnotations;

namespace JwtAuth.Core.Dto
{
    public class UpdatePermissionsDto
    {
        [Required(ErrorMessage = "UserName is Required")]
        public string UserName { get; set; }
    }
}
