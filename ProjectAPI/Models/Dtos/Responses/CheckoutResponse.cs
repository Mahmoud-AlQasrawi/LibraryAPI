namespace ProjectAPI.Models.Dtos.Responses
{
    public class CheckoutResponse
    {
        public string Message { get; set; } = "Book checked out successfully";
        public DateTime DueDate { get; set; } // Non-nullable - we guarantee this is set
        public int TransactionId { get; set; }
    }
}
