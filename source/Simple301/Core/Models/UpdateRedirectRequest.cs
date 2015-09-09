using System.ComponentModel.DataAnnotations;

namespace Simple301.Core.Models
{
    public class UpdateRedirectRequest
    {
        [Required]
        public Redirect Redirect { get; set; }
    }
}
