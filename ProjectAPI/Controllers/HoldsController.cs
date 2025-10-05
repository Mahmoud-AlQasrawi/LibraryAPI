using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectAPI.Data;
using ProjectAPI.Models.Domain;
using ProjectAPI.Models.Dtos.Requests;
using System.Security.Claims;

namespace ProjectAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HoldsController : ControllerBase
    {
        private readonly LibraryDbContext _context;

        public HoldsController(LibraryDbContext context)
        {
            _context = context;
        }
        [HttpPost]
        public async Task<IActionResult> PlaceHold([FromBody] PlaceHoldRequest request)
        {
            var memberId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // 1. Find the book
            var book = await _context.Books.FindAsync(request.BookId);
            if (book == null)
                return NotFound("Book not found");

            // 2. Check if book is checked out (can only hold checked-out books)
            if (book.State != Book.BookState.CheckedOut)
                return BadRequest("Can only place holds on checked-out books");
            // 3. NEW CHECK: User already has this book checked out
            var userHasBookCheckedOut = await _context.Transactions
                .AnyAsync(t => t.BookId == request.BookId &&
                              t.MemberId == memberId &&
                              t.Type == Transaction.TransactionType.Checkout &&
                              t.ReturnDate == null);

            if (userHasBookCheckedOut)
                return BadRequest("You already have this book checked out");

            // 4. Check for existing active hold by same user
            var existingHold = await _context.Holds
                .FirstOrDefaultAsync(h => h.BookId == request.BookId &&
                                        h.MemberId == memberId &&
                                        h.FulfilledDate == null &&
                                        (h.ExpiryDate == null || h.ExpiryDate > DateTime.Now));
            if (existingHold != null)
                return BadRequest("You already have an active hold on this book");

            // 5. Calculate queue position
            var queuePosition = await _context.Holds
                .CountAsync(h => h.BookId == request.BookId &&
                                h.FulfilledDate == null &&
                                (h.ExpiryDate == null || h.ExpiryDate > DateTime.Now));

            // 6. Create hold
            var hold = new Hold(request.BookId, memberId);
            _context.Holds.Add(hold);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Hold placed successfully",
                holdId = hold.Id,
                queuePosition = queuePosition + 1
            });
        }
        [HttpDelete("{holdId}")]
        public async Task<IActionResult> CancelHold(int holdId)
        {
            var memberId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // 1. Find the hold THAT BELONGS TO CURRENT USER
            var hold = await _context.Holds
                .Include(h => h.Book)
                .FirstOrDefaultAsync(h => h.Id == holdId && h.MemberId == memberId);

            if (hold == null)
                return NotFound("Hold not found");

            // 2. Check if hold is already fulfilled or expired
            if (hold.FulfilledDate != null)
                return BadRequest("This hold has already been fulfilled");

            if (hold.ExpiryDate != null && hold.ExpiryDate < DateTime.Now)
                return BadRequest("This hold has already expired");

            // 3. NEW: Check if this hold currently has the book reserved
            var isBookReservedForThisHold = hold.Book.State == Book.BookState.OnHold &&
                                           hold.Book.ReservedForUserId == memberId;

            // 4. Mark as fulfilled (cancelled)
            hold.FulfilledDate = DateTime.Now;

            // 5. Create cancellation transaction
            var transaction = new Transaction
            {
                BookId = hold.BookId,
                MemberId = memberId,
                Type = Transaction.TransactionType.HoldCancelled,
                TransactionDate = DateTime.Now
            };
            _context.Transactions.Add(transaction);

            // 6. NEW: Handle queue advancement if this hold had the book reserved
            if (isBookReservedForThisHold)
            {
                // Find the next active hold in queue
                var nextHold = await _context.Holds
                    .Where(h => h.BookId == hold.BookId &&
                               h.FulfilledDate == null &&
                               (h.ExpiryDate == null || h.ExpiryDate > DateTime.Now) &&
                               h.Id != holdId) // Exclude the one we're cancelling
                    .OrderBy(h => h.RequestDate)
                    .FirstOrDefaultAsync();

                if (nextHold != null)
                {
                    // Reserve book for next user
                    hold.Book.ReservedForUserId = nextHold.MemberId;
                    nextHold.ExpiryDate = DateTime.Now.AddDays(1); // Give them 1 day to collect
                                                                   // Book stays OnHold state - that's correct!
                }
                else
                {
                    // No more holds - make book available
                    hold.Book.State = Book.BookState.Available;
                    hold.Book.ReservedForUserId = null;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Hold cancelled successfully" });
        }
        [HttpGet("myholds")]
        public async Task<IActionResult> GetMyHolds()
        {
            var memberId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var holds = await _context.Holds
                .Where(h => h.MemberId == memberId &&
                           h.FulfilledDate == null &&  // Only not fulfilled
                           (h.ExpiryDate == null || h.ExpiryDate > DateTime.Now))  // Only not expired
                .Include(h => h.Book)
                .Select(h => new
                {
                    h.Id,
                    h.BookId,
                    BookTitle = h.Book.Title,
                    h.RequestDate,
                    h.ExpiryDate,
                    IsActive = true, // Since we filtered, all are active
                    QueuePosition = _context.Holds.Count(other =>
                        other.BookId == h.BookId &&
                        other.FulfilledDate == null &&
                        (other.ExpiryDate == null || other.ExpiryDate > DateTime.Now) &&
                        other.RequestDate <= h.RequestDate)
                })
                .OrderBy(h => h.RequestDate)
                .ToListAsync();

            return Ok(holds);
        }

    }



}
