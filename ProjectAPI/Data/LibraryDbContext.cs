using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ProjectAPI.Models.Domain;

namespace ProjectAPI.Data
{
    public class LibraryDbContext(DbContextOptions<LibraryDbContext> options)
        : IdentityDbContext<Member>(options)
    {
        public DbSet<Book> Books { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Hold> Holds { get; set; }
        public DbSet<Notification> Notifications { get; set; } // JUST ADD THIS LINE BACK


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Book>()
                .HasOne(b => b.ReservedForUser)
                .WithMany()
                .HasForeignKey(b => b.ReservedForUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction); // ← ADD THIS LINE
        }
    }
}