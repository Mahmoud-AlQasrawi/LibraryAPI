namespace ProjectAPI.Models.Dtos.Responses
{
    public class HoldResponse
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string BookTitle { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int QueuePosition { get; set; }
    }
}