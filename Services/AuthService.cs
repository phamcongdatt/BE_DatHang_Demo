
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.IdentityModel.Tokens;
    using QuanLyDatHang.Data;
    using QuanLyDatHang.Models;
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
    using System.Threading.Tasks;
    using BCrypt.Net;
    using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace QuanLyDatHang.Services
    {
        public class AuthService
        {
            private readonly ApplicationDbContext _context;
            private readonly IConfiguration _configuration;

            public AuthService(ApplicationDbContext context, IConfiguration configuration)
            {
                _context = context;
                _configuration = configuration;
            }

            public async Task<(bool Success, string ErrorMessage)> RegisterAsync(User user, string password)
            {
                if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                {
                    return (false, "Email already exists.");
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return (true, null);
            }

            public async Task<(bool Success, string Token, string RefreshToken, string ErrorMessage)> LoginAsync(string email, string password)
            {
                var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email);

                if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    return (false, null, null, "Invalid email or password.");
                }

                if (user.IsLocked)
                {
                    return (false, null, null, "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
                }

                // Tạo sessionId mới và cập nhật vào database
                var sessionId = Guid.NewGuid().ToString("N");
                user.SessionId = sessionId;

                // Xóa các refresh token cũ
                var oldTokens = _context.RefreshTokens.Where(rt => rt.UserId == user.Id);
                _context.RefreshTokens.RemoveRange(oldTokens);

                // Tạo refresh token mới
                var refreshToken = new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                };
                _context.RefreshTokens.Add(refreshToken);

                await _context.SaveChangesAsync();

                // Tạo JWT token với thời hạn 15 phút
                var token = GenerateJwtToken(user, sessionId, TimeSpan.FromMinutes(15));

                return (true, token, refreshToken.Token, null);
            }

            public async Task<User> GetUserProfileAsync(Guid userId)
            {
                return await _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => new User
                    {
                        Id = u.Id,
                        Email = u.Email,
                        FullName = u.FullName,
                        PhoneNumber = u.PhoneNumber,
                        Role = u.Role,
                        CreatedAt = u.CreatedAt,
                        IsLocked = u.IsLocked
                    })
                    .FirstOrDefaultAsync();
            }

            public async Task<(bool Success, string ErrorMessage)> UpdateUserProfileAsync(Guid userId, string fullName, string phoneNumber)
            {
                try
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user == null)
                    {
                        return (false, "Không tìm thấy người dùng");
                    }

                    user.FullName = fullName;
                    user.PhoneNumber = phoneNumber;
                    user.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    return (true, null);
                }
                catch (Exception ex)
                {
                    return (false, "Có lỗi xảy ra khi cập nhật thông tin: " + ex.Message);
                }
            }

            // Logout 
                    public async Task<bool> LogoutAsync(ClaimsPrincipal userClaims)
                {
                    var userId = userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId)) return false;

                    var user = await _context.Users.FindAsync(Guid.Parse(userId));
                    if (user == null) return false;

                    user.SessionId = Guid.NewGuid().ToString();
                    await _context.SaveChangesAsync();

                    return true;
                }
            private string GenerateJwtToken(User user, string sessionId, TimeSpan expiry)
            {
                var jwtSettings = _configuration.GetSection("JwtSettings");
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                var expires = DateTime.Now.Add(expiry);

                var claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Role, user.Role.ToString()),
                    new Claim("sessionId", sessionId),
                    // Thêm claim userId đúng chuẩn cho middleware
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
                };

                var token = new JwtSecurityToken(
                    issuer: jwtSettings["Issuer"],
                    audience: jwtSettings["Audience"],
                    claims: claims,
                    expires: expires,
                    signingCredentials: creds
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }


            


        }
    }

