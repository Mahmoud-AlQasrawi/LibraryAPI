using System.ComponentModel.DataAnnotations;

namespace ProjectAPI.Models.Dtos.Requests
{
    public class CreateBookRequest
    {
        [Required(ErrorMessage = "Title is required")]
        [MinLength(2, ErrorMessage = "Title must be at least 2 characters")]
        [MaxLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Author is required")]
        [MinLength(2, ErrorMessage = "Author must be at least 2 characters")]
        [MaxLength(50, ErrorMessage = "Author cannot exceed 50 characters")]
        public string Author { get; set; }
    }
}