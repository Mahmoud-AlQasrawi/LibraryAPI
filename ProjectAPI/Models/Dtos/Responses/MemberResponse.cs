using ProjectAPI.Models.Domain;

namespace ProjectAPI.Models.Dtos.Responses
{
    public class MemberResponse
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }

        public static MemberResponse FromMember(Member member)
        {
            return new MemberResponse
            {
                Id = member.Id,
                FullName = member.FullName,
                Email = member.Email,
                PhoneNumber = member.PhoneNumber,
                Address = member.Address
            };
        }
    }
}