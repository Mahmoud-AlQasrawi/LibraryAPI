namespace ProjectAPI.Models.Dtos.Responses
{
    public class ReturnResponse
    {
        public string Message { get; set; } = "Book returned successfully";
        public DateTime ReturnDate { get; set; }
    }
}
