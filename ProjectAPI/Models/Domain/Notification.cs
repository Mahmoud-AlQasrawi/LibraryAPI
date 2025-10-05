namespace ProjectAPI.Models.Domain
{
    public class Notification
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public Member User { get; set; }
        public string Message { get; set; }
        public NotificationType Type { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
        public int? RelatedBookId { get; set; }
        public Book RelatedBook { get; set; }

        public enum NotificationType
        {
            DueDateReminder = 1,
            HoldAvailable = 2,
            HoldExpiryWarning = 3,
            OverdueNotice = 4
        }
    }
}