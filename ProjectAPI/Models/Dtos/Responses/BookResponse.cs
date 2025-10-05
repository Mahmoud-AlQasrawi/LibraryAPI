using ProjectAPI.Models.Domain;

namespace ProjectAPI.Models.Dtos.Responses
{
    public class BookResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string State { get; set; } // "Available", "CheckedOut", "Removed"
        public string ReservedForUserId { get; set; }

        public static BookResponse FromBook(Book book)
        {
            return new BookResponse
            {
                Id = book.Id,
                Title = book.Title,
                Author = book.Author,
                State = book.State.ToString(),
                ReservedForUserId = book.ReservedForUserId
            };
        }
    }
}