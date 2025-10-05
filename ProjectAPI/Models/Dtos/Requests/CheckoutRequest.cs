using System.ComponentModel.DataAnnotations;

namespace ProjectAPI.Models.Dtos.Requests
{
    public class CheckoutRequest
    {
        [Required(ErrorMessage = "Book ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Book ID must be a positive number")]
        public int BookId { get; set; }
    }
}