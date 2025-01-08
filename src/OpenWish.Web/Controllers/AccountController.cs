using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenWish.Data.Entities;

namespace OpenWish.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController(UserManager<ApplicationUser> userManager) : Controller
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    [HttpGet("user")]
    public async Task<IActionResult> GetUser()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            user.UserName,
            user.Email
        });
    }
}
