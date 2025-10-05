using ProjectAPI.Models.Domain;

public class Hold
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public Book Book { get; set; }
    public string MemberId { get; set; }
    public Member Member { get; set; }
    public DateTime RequestDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? FulfilledDate { get; set; }

    public bool IsActive => FulfilledDate == null &&
                           (ExpiryDate == null || ExpiryDate > DateTime.Now);

    // Constructor with validation
    public Hold(int bookId, string memberId)
    {
        if (bookId <= 0) throw new ArgumentException("Book ID is required.");
        if (string.IsNullOrWhiteSpace(memberId)) throw new ArgumentException("Member ID is required.");

        BookId = bookId;
        MemberId = memberId;
        RequestDate = DateTime.Now;
    }

    public Hold() { }
}
