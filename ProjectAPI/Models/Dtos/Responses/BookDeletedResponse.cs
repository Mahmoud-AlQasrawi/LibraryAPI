namespace ProjectAPI.Models.Dtos.Responses
{
    public class BookDeletedResponse
    {
        public string Message { get; set; } = "Book removed from collection";
        public int BookId { get; set; }
        public DateTime RemovedDate { get; set; } = DateTime.UtcNow;
    }
}
