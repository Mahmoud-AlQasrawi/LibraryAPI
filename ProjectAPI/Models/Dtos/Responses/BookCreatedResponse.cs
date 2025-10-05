namespace ProjectAPI.Models.Dtos.Responses
{
    public class BookCreatedResponse
    {
        public string Message { get; set; } = "Book added successfully";
        public BookResponse Book { get; set; }
        public DateTime AddedDate { get; set; } = DateTime.UtcNow;
    }
}
