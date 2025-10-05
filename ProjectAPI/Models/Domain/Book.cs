namespace ProjectAPI.Models.Domain
{
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public BookState State { get; set; } = BookState.Available; // Default value
        public string ReservedForUserId { get; set; }
        public Member ReservedForUser { get; set; }

        // ONLY empty constructor needed
        public Book() { }

        public enum BookState
        {
            Available = 1,
            CheckedOut = 2,
            Removed = 3,
            OnHold = 4
        }
    }
}