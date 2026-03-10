using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Models.DTOs;
using UserService.Services;

namespace UserService.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<UserResponseDto>> Register([FromBody] RegisterRequestDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ValidationProblem(ModelState);
                }

                var createdUser = await _userService.RegisterAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = createdUser.Id }, createdUser);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while registering user.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
        }

        [HttpGet("{id:guid}")]
        [Authorize]
        public async Task<ActionResult<UserResponseDto>> GetById(Guid id)
        {
            try
            {
                var user = await _userService.GetByIdAsync(id);
                if (user is null)
                {
                    return NotFound();
                }

                return Ok(user);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching user {UserId}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
        }

        [HttpPost("token")]
        [AllowAnonymous]
        public async Task<ActionResult<TokenResponseDto>> GetAccessToken([FromBody] LoginRequestDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ValidationProblem(ModelState);
                }

                var accessToken = await _userService.GetAccessTokenAsync(dto);
                return Ok(new TokenResponseDto { AccessToken = accessToken });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while requesting token for {Email}.", dto?.Email);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
        }

        [HttpPost("confirm-signup")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmSignup([FromBody] ConfirmRequestDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ValidationProblem(ModelState);
                }

                var message = await _userService.ConfirmUserAsync(dto);
                return Ok(new { message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while confirming signup for {Email}.", dto?.Email);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred." });
            }
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public ActionResult<string> Health()
        {
            return Ok("Healthy");
        }
    }
}
