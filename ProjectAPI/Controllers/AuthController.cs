using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectAPI.Data;
using ProjectAPI.Models.Domain;
using ProjectAPI.Models.Dtos.Requests;
using ProjectAPI.Models.Dtos.Responses;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ProjectAPI.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<Member> _userManager;
        private readonly IConfiguration _configuration;
        private readonly LibraryDbContext _context;

        private string GenerateJwtToken(Member member, IList<string> roles)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, member.Id),
        new Claim(ClaimTypes.Email, member.Email)
    };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(2),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        public AuthController(UserManager<Member> userManager, IConfiguration configuration, LibraryDbContext context)
        {
            _userManager = userManager;
            _configuration = configuration;
            _context = context; // Add this line
        }
        [HttpPost("setup")]
        public async Task<IActionResult> SetupApplication()
        {
            var roleManager = HttpContext.RequestServices.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<Member>>();

            // Create THREE roles
            string[] roleNames = { "Member", "Clerk", "Admin" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new IdentityRole(roleName);
                    var createRoleResult = await roleManager.CreateAsync(role);
                    if (!createRoleResult.Succeeded)
                    {
                        return BadRequest($"Failed to create role {roleName}: {string.Join(", ", createRoleResult.Errors)}");
                    }
                }
            }

            // Create default ADMIN (not clerk)
            var adminEmail = "admin@library.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new Member("Library", "Admin", adminEmail, "555-0001", "Library Address");
                var result = await userManager.CreateAsync(admin, "AdminPassword123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }

            return Ok(new { message = "Application setup complete with Admin role" });
        }
        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] RegisterRequest request)
        {
            var member = new Member(request.FirstName, request.LastName, request.Email, request.PhoneNumber, request.Address);

            var result = await _userManager.CreateAsync(member, request.Password);

            if (result.Succeeded)
            {
                // Auto-assign role
                await _userManager.AddToRoleAsync(member, "Member");

                // Use helper method for auto-login
                var roles = await _userManager.GetRolesAsync(member);
                var tokenString = GenerateJwtToken(member, roles);

                return Ok(new AuthResponse
                {
                    Message = "Registration successful",
                    Token = tokenString,
                    Roles = roles.ToArray()
                });
            }

            return BadRequest(result.Errors);
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var member = await _userManager.FindByEmailAsync(request.Email);
            if (member == null)
                return Unauthorized("Invalid email or password");

            var isPasswordValid = await _userManager.CheckPasswordAsync(member, request.Password);
            if (!isPasswordValid)
                return Unauthorized("Invalid email or password");

            // Use helper method
            var roles = await _userManager.GetRolesAsync(member);
            var tokenString = GenerateJwtToken(member, roles);

            return Ok(new AuthResponse
            {
                Message = "Login successful",
                Token = tokenString,
                Roles = roles.ToArray()
            });
        }
    }
}
