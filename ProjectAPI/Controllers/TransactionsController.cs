using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectAPI.Data;
using ProjectAPI.Models.Domain;
using System.Security.Claims;

namespace ProjectAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly LibraryDbContext _context;

        public TransactionsController(LibraryDbContext context)
        {
            _context = context;
        }

        // GET: /api/transactions
        // GET: /api/transactions?fromDate=2024-01-01&toDate=2024-12-31&type=Checkout
        [HttpGet]
        public async Task<IActionResult> GetMyTransactions(
            [FromQuery] string? fromDate,
            [FromQuery] string? toDate,
            [FromQuery] string? specificDate,
            [FromQuery] string? type)
        {
            var memberId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var query = _context.Transactions
                .Include(t => t.Book)
                .Where(t => t.MemberId == memberId)
                .AsQueryable();

            // NEW: Flexible date parsing method
            DateTime? ParseDate(string dateString)
            {
                if (string.IsNullOrEmpty(dateString)) return null;

                // Try common date formats
                string[] formats = {
            "yyyy-MM-dd",      // 2024-10-04
            "MM/dd/yyyy",      // 10/04/2024
            "dd/MM/yyyy",      // 04/10/2024
            "M/d/yyyy",        // 10/4/2024
            "d/M/yyyy",        // 4/10/2024
            "yyyy.MM.dd",      // 2024.10.04
            "dd-MM-yyyy",      // 04-10-2024
            "MM-dd-yyyy",      // 10-04-2024
        };

                if (DateTime.TryParseExact(dateString, formats,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var result))
                {
                    return result;
                }

                // Fallback to normal parsing
                if (DateTime.TryParse(dateString, out var fallbackResult))
                {
                    return fallbackResult;
                }

                return null;
            }

            // Handle specific date
            if (!string.IsNullOrEmpty(specificDate))
            {
                var specificDateParsed = ParseDate(specificDate);
                if (specificDateParsed.HasValue)
                {
                    var startOfDay = specificDateParsed.Value.Date;
                    var endOfDay = specificDateParsed.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(t => t.TransactionDate >= startOfDay && t.TransactionDate <= endOfDay);
                }
            }
            else
            {
                // Handle fromDate
                if (!string.IsNullOrEmpty(fromDate))
                {
                    var fromDateParsed = ParseDate(fromDate);
                    if (fromDateParsed.HasValue)
                    {
                        query = query.Where(t => t.TransactionDate >= fromDateParsed.Value.Date);
                    }
                }

                // Handle toDate  
                if (!string.IsNullOrEmpty(toDate))
                {
                    var toDateParsed = ParseDate(toDate);
                    if (toDateParsed.HasValue)
                    {
                        var endOfDay = toDateParsed.Value.Date.AddDays(1).AddTicks(-1);
                        query = query.Where(t => t.TransactionDate <= endOfDay);
                    }
                }
            }

            // Transaction type filter
            if (!string.IsNullOrEmpty(type) && Enum.TryParse<Transaction.TransactionType>(type, true, out var transactionType))
            {
                query = query.Where(t => t.Type == transactionType);
            }

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .Select(t => new
                {
                    t.Id,
                    BookId = t.Book.Id,
                    BookTitle = t.Book.Title,
                    BookAuthor = t.Book.Author,
                    TransactionType = t.Type.ToString(),
                    TransactionDate = t.TransactionDate.ToString("yyyy-MM-dd HH:mm"),
                    DueDate = t.DueDate.HasValue ? t.DueDate.Value.ToString("yyyy-MM-dd HH:mm") : null,
                    ReturnDate = t.ReturnDate.HasValue ? t.ReturnDate.Value.ToString("yyyy-MM-dd HH:mm") : null,
                    IsOverdue = t.Type == Transaction.TransactionType.Checkout &&
                               t.ReturnDate == null &&
                               t.DueDate < DateTime.Now
                })
                .ToListAsync();

            return Ok(new
            {
                Count = transactions.Count,
                SpecificDate = specificDate,
                FromDate = fromDate,
                ToDate = toDate,
                Transactions = transactions
            });
        }
    }
}