namespace ProjectAPI.Models.Dtos.Responses
{
    public class RegisterResponse
    {
        public string Message { get; set; } = "User registered successfully";
        public string UserId { get; set; }
    }
}
