using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectAPI.Data;
using ProjectAPI.Models.Domain;
using ProjectAPI.Models.Dtos.Responses;
using ProjectAPI.Models.Dtos.Requests;
using static ProjectAPI.Models.Domain.Book;

namespace ProjectAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BooksController : ControllerBase
    {
        private readonly LibraryDbContext _context;

        public BooksController(LibraryDbContext context)
        {
            _context = context;
        }

        // Anyone can view books
        [HttpGet]
        public async Task<ActionResult<List<BookResponse>>> GetBooks()
        {
            var books = await _context.Books
                .Where(b => b.State != Book.BookState.Removed)
                .Select(b => BookResponse.FromBook(b))
                .ToListAsync();
            return Ok(books);
        }

        // Only Clerks can add books
        [HttpPost]
        [Authorize(Roles = "Clerk,Admin")]  // Both can access
        public async Task<ActionResult<BookCreatedResponse>> AddBook([FromBody] CreateBookRequest request)
        {
            try
            {
                var book = new Book
                {
                    Title = request.Title,
                    Author = request.Author,
                    State = Book.BookState.Available
                };

                _context.Books.Add(book);
                await _context.SaveChangesAsync();

                return Ok(new BookCreatedResponse { Book = BookResponse.FromBook(book) });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = "Failed to create book", details = ex.Message });
            }
        }
        // Only Clerks can remove books
        [HttpDelete("{id}")]
        [Authorize(Roles = "Clerk,Admin")]  // Both can access
        public async Task<IActionResult> DeleteBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null || book.State == BookState.Removed) // ← Already removed?
                return NotFound();

            book.State = BookState.Removed; // ← SOFT DELETE (change state)
            await _context.SaveChangesAsync();

            return Ok(new BookDeletedResponse { BookId = book.Id });
        }

        [HttpGet("{id}")]

        public async Task<ActionResult<BookResponse>> GetBookById(int id)
        {
            var book = await _context.Books
                .Where(b => b.Id == id && b.State != Book.BookState.Removed)
                .FirstOrDefaultAsync();

            if (book == null)
                return NotFound();

            return Ok(BookResponse.FromBook(book));
        }
    }
}