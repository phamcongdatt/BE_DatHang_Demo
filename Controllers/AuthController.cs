using Microsoft.AspNetCore.Mvc;
using QuanLyDatHang.DTOs;
using QuanLyDatHang.Models;
using QuanLyDatHang.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System;
using System.Linq;

namespace QuanLyDatHang.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly IConfiguration _configuration;

        public AuthController(AuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = new User
            {
                Email = registerDto.Email,
                FullName = registerDto.FullName,
                PhoneNumber = registerDto.PhoneNumber,
                Role = registerDto.Role
            };

            var (success, errorMessage) = await _authService.RegisterAsync(user, registerDto.Password);

            if (!success)
            {
                return BadRequest(new { message = errorMessage });
            }

            return Ok(new { message = "Đăng ký tài khoản thành công" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var (success, token, RefreshToken, errorMessage) = await _authService.LoginAsync(loginDto.Email, loginDto.Password);

            if (!success)
            {
                return Unauthorized(new { message = errorMessage });
            }

            return Ok(new { token });
        }
        

        //Loguot  
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var success = await _authService.LogoutAsync(User);
            if (!success) return Unauthorized();

            return Ok(new { message = "Logged out successfully" });
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var user = await _authService.GetUserProfileAsync(userId);

                if (user == null)
                {
                    return NotFound(new { message = "Không tìm thấy thông tin người dùng" });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        user.Id,
                        user.Email,
                        user.FullName,
                        user.PhoneNumber,
                        user.Role,
                        user.CreatedAt,
                        user.IsLocked
                    },
                    message = "Lấy thông tin profile thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi lấy thông tin profile"
                });
            }
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Dữ liệu không hợp lệ",
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var (success, errorMessage) = await _authService.UpdateUserProfileAsync(userId, updateDto.FullName, updateDto.PhoneNumber);

                if (!success)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = errorMessage
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật thông tin profile thành công"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi cập nhật thông tin profile"
                });
            }
        }
    }
}