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
    public class NotificationsController : ControllerBase
    {
        private readonly LibraryDbContext _context;

        public NotificationsController(LibraryDbContext context)
        {
            _context = context;
        }
        [HttpPost("send-due-reminders")]
        [Authorize(Roles = "Clerk,Admin")]
        public async Task<IActionResult> SendDueDateReminders()
        {
            try
            {
                var notificationsCreated = 0;

                // 1. DUE DATE REMINDERS - FIXED: Books due TOMORROW (1 day left)
                var tomorrow = DateTime.Now.AddDays(1).Date;
                var dueTransactions = await _context.Transactions
                    .Where(t => t.DueDate.HasValue &&
                               t.DueDate.Value.Date == tomorrow &&  // ← CHANGED to tomorrow
                               t.ReturnDate == null &&
                               t.Type == Transaction.TransactionType.Checkout)
                    .Include(t => t.Book)
                    .Include(t => t.Member)
                    .ToListAsync();

                foreach (var transaction in dueTransactions)
                {
                    var existingNotification = await _context.Notifications
                        .AnyAsync(n => n.UserId == transaction.MemberId &&
                                      n.RelatedBookId == transaction.BookId &&
                                      n.Type == Notification.NotificationType.DueDateReminder &&
                                      EF.Functions.Like(n.Message, $"%Transaction #{transaction.Id}%"));

                    if (!existingNotification)
                    {
                        var notification = new Notification
                        {
                            UserId = transaction.MemberId,
                            Message = $"REMINDER: Your book '{transaction.Book.Title}' is due tomorrow! (Transaction #{transaction.Id})",
                            Type = Notification.NotificationType.DueDateReminder,
                            RelatedBookId = transaction.BookId
                        };

                        _context.Notifications.Add(notification);
                        notificationsCreated++;
                    }
                }

                // 2. HOLD AVAILABLE NOTIFICATIONS (unchanged)
                var booksOnHold = await _context.Books
                    .Where(b => b.State == Book.BookState.OnHold &&
                               b.ReservedForUserId != null)
                    .Include(b => b.ReservedForUser)
                    .ToListAsync();

                foreach (var book in booksOnHold)
                {
                    var activeHold = await _context.Holds
                        .FirstOrDefaultAsync(h => h.BookId == book.Id &&
                                                h.MemberId == book.ReservedForUserId &&
                                                h.FulfilledDate == null &&
                                                (h.ExpiryDate == null || h.ExpiryDate > DateTime.Now));

                    if (activeHold != null)
                    {
                        var existingNotification = await _context.Notifications
                            .AnyAsync(n => n.UserId == book.ReservedForUserId &&
                                          n.RelatedBookId == book.Id &&
                                          n.Type == Notification.NotificationType.HoldAvailable &&
                                          EF.Functions.Like(n.Message, $"%Hold #{activeHold.Id}%"));

                        if (!existingNotification)
                        {
                            var notification = new Notification
                            {
                                UserId = book.ReservedForUserId,
                                Message = $"HOLD AVAILABLE: '{book.Title}' is now reserved for you! Collect within 1 day. (Hold #{activeHold.Id})",
                                Type = Notification.NotificationType.HoldAvailable,
                                RelatedBookId = book.Id
                            };

                            _context.Notifications.Add(notification);
                            notificationsCreated++;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Created {notificationsCreated} notifications",
                    totalNotifications = notificationsCreated
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to send due reminders: {ex.Message}");
            }
        }
        [HttpGet("my-notifications")]
        public async Task<IActionResult> GetMyNotifications()
        {
            var memberId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // 1. Get unread notifications FOR CURRENT USER
            var notifications = await _context.Notifications
                .Where(n => n.UserId == memberId && !n.IsRead)
                .OrderByDescending(n => n.CreatedDate)
                .Select(n => new
                {
                    n.Id,
                    n.Message,
                    Type = n.Type.ToString(),
                    n.CreatedDate,
                    n.RelatedBookId,
                    BookTitle = n.RelatedBook.Title
                })
                .ToListAsync();

            // 2. Mark ONLY CURRENT USER'S notifications as read
            var dbNotifications = await _context.Notifications
                .Where(n => n.UserId == memberId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in dbNotifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return Ok(notifications);
        }
    }
}