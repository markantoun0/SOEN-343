using Microsoft.AspNetCore.Mvc;
using SUMMS.Api.Services.Interfaces;

namespace SUMMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger      = logger;
    }

    [HttpPost("signup")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)  ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { success = false, message = "Name, email, and password are required." });

        try
        {
            var user = await _userService.SignUpAsync(request.Name, request.Email, request.Password);
            return StatusCode(StatusCodes.Status201Created, new
            {
                success = true,
                user    = ToUserDto(user)
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { success = false, message = "Email and password are required." });

        var user = await _userService.LoginAsync(request.Email, request.Password);
        if (user is null)
            return Unauthorized(new { success = false, message = "Invalid email or password." });

        return Ok(new
        {
            success = true,
            user    = ToUserDto(user)
        });
    }

    [HttpGet("{userId:int}/preferences")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreferences(int userId)
    {
        var user = await _userService.GetByIdAsync(userId);
        if (user is null)
            return NotFound(new { success = false, message = $"No user found with Id={userId}." });

        return Ok(new
        {
            success = true,
            preferences = new
            {
                preferredCity = user.PreferredCity,
                preferredMobilityType = user.PreferredMobilityType
            }
        });
    }

    [HttpPatch("{userId:int}/preferences")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePreferences(int userId, [FromBody] UpdatePreferencesRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PreferredCity) ||
            string.IsNullOrWhiteSpace(request.PreferredMobilityType))
        {
            return BadRequest(new { success = false, message = "Preferred city and mobility type are required." });
        }

        try
        {
            var updated = await _userService.UpdatePreferencesAsync(
                userId,
                request.PreferredCity,
                request.PreferredMobilityType
            );

            if (!updated)
                return NotFound(new { success = false, message = $"No user found with Id={userId}." });

            var user = await _userService.GetByIdAsync(userId);
            return Ok(new
            {
                success = true,
                preferences = new
                {
                    preferredCity = user?.PreferredCity,
                    preferredMobilityType = user?.PreferredMobilityType
                }
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    private static object ToUserDto(YourProject.Models.User user)
    {
        return new
        {
            user.Id,
            user.Name,
            user.Email,
            preferredCity = user.PreferredCity,
            preferredMobilityType = user.PreferredMobilityType
        };
    }
}

public class SignUpRequest
{
    public string Name     { get; set; } = string.Empty;
    public string Email    { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Email    { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UpdatePreferencesRequest
{
    public string PreferredCity { get; set; } = string.Empty;
    public string PreferredMobilityType { get; set; } = string.Empty;
}
