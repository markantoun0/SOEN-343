using Microsoft.AspNetCore.Mvc;
using SUMMS.Api.Services.Interfaces;

namespace SUMMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminsController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IMobilityLocationService _mobilityService;

    public AdminsController(
        IAdminService adminService,
        IMobilityLocationService mobilityService)
    {
        _adminService = adminService;
        _mobilityService = mobilityService;
    }

    [HttpPost("signup")]
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SignUp([FromBody] AdminRegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { success = false, message = "Name, email, and password are required." });

        try
        {
            var admin = await _adminService.RegisterAsync(request.Name, request.Email, request.Password);
            return StatusCode(StatusCodes.Status201Created, new
            {
                success = true,
                admin = new { admin.Id, admin.Name, admin.Email }
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
    public async Task<IActionResult> Login([FromBody] AdminLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { success = false, message = "Email and password are required." });

        var admin = await _adminService.LoginAsync(request.Email, request.Password);
        if (admin is null)
            return Unauthorized(new { success = false, message = "Invalid email or password." });

        return Ok(new
        {
            success = true,
            admin = new { admin.Id, admin.Name, admin.Email }
        });
    }
    
    [HttpGet("analytics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnalytics()
    {
        var data = await _mobilityService.GetCityAnalyticsAsync();
        return Ok(data);
    }
}

public class AdminRegisterRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AdminLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
