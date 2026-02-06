using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommonHall.Api.Controllers;

[Authorize]
public class UsersController : BaseApiController
{
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        // Placeholder - implement with actual user retrieval
        return Ok(new { Message = "Current user endpoint" });
    }

    [HttpGet]
    public IActionResult GetUsers()
    {
        // Placeholder - implement with actual user listing
        return Ok(new { Message = "Users list endpoint" });
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetUser(Guid id)
    {
        // Placeholder - implement with actual user retrieval
        return Ok(new { Message = $"User {id} endpoint" });
    }
}
