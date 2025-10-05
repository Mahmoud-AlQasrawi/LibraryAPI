using System.ComponentModel.DataAnnotations;

namespace ProjectAPI.Models.Domain
{
    public class Transaction
    {
        public int Id { get; set; }                    // Just use this as primary key
        public int BookId { get; set; }
        public Book Book { get; set; }                 // Navigation property

        public string MemberId { get; set; }
        public Member Member { get; set; }             // Navigation property

        public TransactionType Type { get; set; }
        public DateTime TransactionDate { get; set; }  // When it happened
        public DateTime? DueDate { get; set; }         // For checkouts
        public DateTime? ReturnDate { get; set; }      // For returns

        // REMOVED: TransactionNumber, DisplayId

        public enum TransactionType
        {
            Checkout = 1,
            Return = 2,
            Renewal = 3,
            HoldPlaced = 4,
            HoldCancelled = 5
        }
    }
}