using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectAPI.Data;
using ProjectAPI.Models.Dtos.Responses;
using System.Security.Claims;

namespace ProjectAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requires valid JWT token
    public class MembersController : ControllerBase
    {
        private readonly LibraryDbContext _context;

        public MembersController(LibraryDbContext context)
        {
            _context = context;
        }

        // GET: api/members
        [HttpGet]
        [Authorize(Roles = "Clerk,Admin")]  // Both can access
        public async Task<ActionResult> GetAllMembers()
        {
            try
            {
                // Simple test - just get count first
                var memberCount = await _context.Users.CountAsync();

                // If that works, try the actual query
                var members = await _context.Users
                    .Select(m => MemberResponse.FromMember(m))
                    .ToListAsync();

                return Ok(new
                {
                    count = memberCount,
                    members = members
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }
        /*[HttpGet("debug-token")]
        [Authorize]
        public IActionResult DebugToken()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

            return Ok(new
            {
                claims = claims,
                roles = roles,
                hasClerkRole = User.IsInRole("Clerk")
            });
        }*/
         

    }
}