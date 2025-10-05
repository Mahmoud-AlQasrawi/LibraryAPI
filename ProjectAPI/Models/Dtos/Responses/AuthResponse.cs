namespace ProjectAPI.Models.Dtos.Responses
{
    public class AuthResponse
    {
        public string Message { get; set; }
        public string Token { get; set; }
        public string[] Roles { get; set; }
    }
}
