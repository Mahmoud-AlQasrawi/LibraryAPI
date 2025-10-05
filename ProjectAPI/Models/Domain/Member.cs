using Microsoft.AspNetCore.Identity;

namespace ProjectAPI.Models.Domain
{
    public class Member : IdentityUser
    {
        // REMOVED: MemberNumber, DisplayId - just use the built-in Id
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }

        public string FullName => $"{FirstName} {LastName}";

        // Constructor with validation
        public Member(string firstName, string lastName, string email, string phoneNumber, string address)
        {
            if (string.IsNullOrWhiteSpace(firstName))
                throw new ArgumentException("First name is required.", nameof(firstName));
            if (string.IsNullOrWhiteSpace(lastName))
                throw new ArgumentException("Last name is required.", nameof(lastName));
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required.", nameof(email));
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Phone number is required.", nameof(phoneNumber));

            FirstName = firstName.Trim();
            LastName = lastName.Trim();
            Email = email.Trim();
            UserName = email.Trim();
            PhoneNumber = phoneNumber.Trim();
            Address = address?.Trim();
        }

        // Parameterless constructor for EF
        public Member() { }
    }
}
