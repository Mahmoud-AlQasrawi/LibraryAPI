using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectAPI.Data;
using ProjectAPI.Models.Domain;
using ProjectAPI.Models.Dtos.Requests;
using ProjectAPI.Models.Dtos.Responses;
using System.Security.Claims;

namespace ProjectAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]

    public class LibraryOpsController : ControllerBase
    {
        private readonly LibraryDbContext _context;

        public LibraryOpsController(LibraryDbContext context)
        {
            _context = context;
        }
        [HttpGet("mybooks")]
        public async Task<IActionResult> GetMyCheckedOutBooks()
        {
            var memberId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Get the LATEST due date for each book (considering renewals)
            var checkedOutBooks = await _context.Transactions
                .Where(t => t.MemberId == memberId &&
                           t.Type == Transaction.TransactionType.Checkout &&
                           t.ReturnDate == null)
                .Include(t => t.Book)
                .Select(t => new
                {
                    TransactionId = t.Id,
                    BookId = t.Book.Id,
                    BookTitle = t.Book.Title,
                    BookAuthor = t.Book.Author,
                    CheckoutDate = t.TransactionDate,
                    // Get the latest due date (from renewals)
                    DueDate = _context.Transactions
                        .Where(rt => rt.BookId == t.BookId &&
                                    rt.MemberId == memberId &&
                                    (rt.Type == Transaction.TransactionType.Checkout ||
                                     rt.Type == Transaction.TransactionType.Renewal) &&
                                    rt.ReturnDate == null)
                        .Max(rt => rt.DueDate)
                })
                .ToListAsync();

            return Ok(checkedOutBooks);
        }
        [HttpPost("checkout")]
        public async Task<IActionResult> CheckoutBook([FromBody] CheckoutRequest request)
        {
            try
            {
                var memberId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // 1. Find the book
                var book = await _context.Books.FindAsync(request.BookId);
                if (book == null)
                    return NotFound("Book not found");

                // 2. Check if book is available OR reserved for current user
                if (book.State == Book.BookState.CheckedOut || book.State == Book.BookState.Removed)
                    return BadRequest("Book is not available for checkout");

                // 3. Check if book is OnHold but reserved for someone else
                if (book.State == Book.BookState.OnHold && book.ReservedForUserId != memberId)
                    return BadRequest("Book is reserved for another user");

                // 4. Create transaction
                var transaction = new Transaction
                {
                    BookId = request.BookId,
                    MemberId = memberId,
                    Type = Transaction.TransactionType.Checkout,
                    TransactionDate = DateTime.Now,
                    DueDate = DateTime.Now.AddDays(14)
                };

                // 5. Check if book was OnHold BEFORE changing state
                var wasOnHold = book.State == Book.BookState.OnHold;

                // 6. Update book state
                book.State = Book.BookState.CheckedOut;

                // 7. If book was OnHold, mark the hold as fulfilled
                if (wasOnHold)
                {
                    var fulfilledHold = await _context.Holds
                        .FirstOrDefaultAsync(h => h.BookId == request.BookId &&
                                                h.MemberId == memberId &&
                                                h.FulfilledDate == null);
                    if (fulfilledHold != null)
                    {
                        fulfilledHold.FulfilledDate = DateTime.Now;
                    }
                    book.ReservedForUserId = null; // Clear reservation
                }

                // 8. Save changes
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                return Ok(new CheckoutResponse
                {
                    DueDate = transaction.DueDate.Value,
                    TransactionId = transaction.Id
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Checkout failed: {ex.Message}");
            }
        }
        [HttpPost("return")]
        public async Task<IActionResult> ReturnBook([FromBody] ReturnRequest request)
        {
            try
            {
                var memberId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // 1. Find the book
                var book = await _context.Books.FindAsync(request.BookId);
                if (book == null)
                    return NotFound("Book not found");

                // 2. Check if book is actually checked out
                if (book.State != Book.BookState.CheckedOut)
                    return BadRequest("Book is not currently checked out");

                // 3. Find the active checkout transaction
                var checkoutTransaction = await _context.Transactions
                    .Where(t => t.BookId == request.BookId &&
                               t.Type == Transaction.TransactionType.Checkout &&
                               t.ReturnDate == null)
                    .FirstOrDefaultAsync();

                if (checkoutTransaction == null)
                    return BadRequest("No active checkout found for this book");

                // 4. Check if current user is the one who checked it out
                if (checkoutTransaction.MemberId != memberId)
                    return BadRequest("You can only return books that you checked out");

                // 5. Check for active holds
                var nextHold = await _context.Holds
                    .Where(h => h.BookId == request.BookId &&
                               h.FulfilledDate == null &&
                               (h.ExpiryDate == null || h.ExpiryDate > DateTime.Now))
                    .OrderBy(h => h.RequestDate)
                    .FirstOrDefaultAsync();

                // 6. Create return transaction
                var returnTransaction = new Transaction
                {
                    BookId = request.BookId,
                    MemberId = memberId,
                    Type = Transaction.TransactionType.Return,
                    TransactionDate = DateTime.Now,
                    ReturnDate = DateTime.Now
                };

                // 7. Update checkout transaction with return date
                checkoutTransaction.ReturnDate = DateTime.Now;

                if (nextHold != null)
                {
                    // 8. Book has holds - reserve it for the next user
                    book.State = Book.BookState.OnHold;
                    book.ReservedForUserId = nextHold.MemberId;
                    nextHold.ExpiryDate = DateTime.Now.AddDays(1);
                }
                else
                {
                    // 9. No holds - book becomes available to anyone
                    book.State = Book.BookState.Available;
                    book.ReservedForUserId = null;
                }

                // 10. Save ALL changes
                _context.Transactions.Add(returnTransaction);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = nextHold != null
                        ? "Book returned and reserved for next hold user"
                        : "Book returned and available for checkout"
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Return failed: {ex.Message}");
            }
        }
        [HttpPost("cleanup-expired-holds")]
        [Authorize(Roles = "Clerk,Admin")]
        public async Task<IActionResult> CleanupExpiredHolds()
        {
            try
            {
                var expiredHolds = await _context.Holds
                    .Include(h => h.Book)
                    .Where(h => h.ExpiryDate != null &&
                               h.ExpiryDate < DateTime.Now &&
                               h.FulfilledDate == null)
                    .ToListAsync();

                int cleanedCount = 0;

                foreach (var hold in expiredHolds)
                {
                    // Mark hold as fulfilled (expired)
                    hold.FulfilledDate = DateTime.Now;

                    // Create cancellation transaction
                    var transaction = new Transaction
                    {
                        BookId = hold.BookId,
                        MemberId = hold.MemberId,
                        Type = Transaction.TransactionType.HoldCancelled,
                        TransactionDate = DateTime.Now
                    };
                    _context.Transactions.Add(transaction);

                    // Find next hold in queue
                    var nextHold = await _context.Holds
                        .Where(h => h.BookId == hold.BookId &&
                                   h.FulfilledDate == null &&
                                   (h.ExpiryDate == null || h.ExpiryDate > DateTime.Now) &&
                                   h.RequestDate > hold.RequestDate)
                        .OrderBy(h => h.RequestDate)
                        .FirstOrDefaultAsync();

                    if (nextHold != null)
                    {
                        // Reserve for next user
                        hold.Book.State = Book.BookState.OnHold;
                        hold.Book.ReservedForUserId = nextHold.MemberId;
                        nextHold.ExpiryDate = DateTime.Now.AddDays(1);
                    }
                    else
                    {
                        // No more holds - make available
                        hold.Book.State = Book.BookState.Available;
                        hold.Book.ReservedForUserId = null;
                    }

                    cleanedCount++;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Cleanup completed: {cleanedCount} expired holds processed",
                    expiredHoldsCleaned = cleanedCount
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Cleanup failed: {ex.Message}");
            }
        }

        [HttpGet("my-renewable-books")]
        public async Task<IActionResult> GetMyRenewableBooks()
        {
            var memberId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Get user's currently checked out books
            var checkedOutBooks = await _context.Transactions
                .Include(t => t.Book)
                .Where(t => t.MemberId == memberId &&
                           t.Type == Transaction.TransactionType.Checkout &&
                           t.ReturnDate == null) // Only active checkouts
                .Select(t => new
                {
                    TransactionId = t.Id,
                    BookId = t.Book.Id,
                    BookTitle = t.Book.Title,
                    BookAuthor = t.Book.Author,
                    CheckoutDate = t.TransactionDate,
                    DueDate = t.DueDate,
                    DaysUntilDue = (int)(t.DueDate.Value - DateTime.Now).TotalDays,
                    // Check if renewable (no active holds on the book)
                    IsRenewable = !_context.Holds.Any(h => h.BookId == t.BookId &&
                                                          h.FulfilledDate == null &&
                                                          (h.ExpiryDate == null || h.ExpiryDate > DateTime.Now))
                })
                .ToListAsync();

            return Ok(checkedOutBooks);
        }

        [HttpPost("renew")]
        public async Task<IActionResult> RenewBook([FromBody] RenewBookRequest request)
        {
            try
            {
                var memberId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // 1. Find the active checkout by BOOK ID (not transaction ID)
                var checkoutTransaction = await _context.Transactions
                    .Include(t => t.Book)
                    .FirstOrDefaultAsync(t => t.BookId == request.BookId &&
                                             t.MemberId == memberId &&
                                             t.Type == Transaction.TransactionType.Checkout &&
                                             t.ReturnDate == null);

                if (checkoutTransaction == null)
                    return BadRequest("No active checkout found for this book");

                // 2. Check if book has active holds
                var hasActiveHolds = await _context.Holds
                    .AnyAsync(h => h.BookId == checkoutTransaction.BookId &&
                                  h.FulfilledDate == null &&
                                  (h.ExpiryDate == null || h.ExpiryDate > DateTime.Now));

                if (hasActiveHolds)
                    return BadRequest("Cannot renew book - there are active holds on this book");

                // 3. Create renewal transaction
                var renewalTransaction = new Transaction
                {
                    BookId = checkoutTransaction.BookId,
                    MemberId = memberId,
                    Type = Transaction.TransactionType.Renewal,
                    TransactionDate = DateTime.Now,
                    DueDate = DateTime.Now.AddDays(14) // Renew for another 14 days
                };

                // 4. Update the original checkout's due date
                checkoutTransaction.DueDate = renewalTransaction.DueDate;

                // 5. ALSO update the book's state if needed (in case it was marked overdue)
                var book = await _context.Books.FindAsync(checkoutTransaction.BookId);
                if (book != null && book.State == Book.BookState.CheckedOut)

                    // 6. Save changes
                    _context.Transactions.Add(renewalTransaction);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Book renewed successfully",
                    newDueDate = renewalTransaction.DueDate.Value.ToString("yyyy-MM-dd HH:mm"),
                    renewalTransactionId = renewalTransaction.Id
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Renewal failed: {ex.Message}");
            }
        }
    }
}