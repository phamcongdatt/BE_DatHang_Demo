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
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using System.Net.Mail;
using System.Net;

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
                return (false, "Email đã được sử dụng.");
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

            var sessionId = Guid.NewGuid().ToString("N");
            user.SessionId = sessionId;

            var oldTokens = _context.RefreshTokens.Where(rt => rt.UserId == user.Id);
            _context.RefreshTokens.RemoveRange(oldTokens);

            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
            _context.RefreshTokens.Add(refreshToken);

            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user, sessionId, TimeSpan.FromMinutes(20));

            return (true, token, refreshToken.Token, null);
        }

        public async Task<(bool Success, string Token, string RefreshToken, string ErrorMessage)> LoginGoogleAsync(string idToken)
        {
            try
            {
                var clientIdConfig = _configuration["GoogleAuthSettings:ClientId"];
                if (string.IsNullOrEmpty(clientIdConfig))
                {
                    return (false, null, null, "Google login failed: ClientId is not configured on server.");
                }
                var clientIds = clientIdConfig.Contains(",") ? clientIdConfig.Split(",").Select(x => x.Trim()).ToArray() : new[] { clientIdConfig };
                GoogleJsonWebSignature.Payload payload = null;
                try
                {
                    payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
                    {
                        Audience = clientIds
                    });
                }
                catch (Exception ex)
                {
                    string aud = null;
                    try
                    {
                        var handler = new JwtSecurityTokenHandler();
                        var jwt = handler.ReadJwtToken(idToken);
                        aud = jwt.Audiences != null ? string.Join(",", jwt.Audiences) : "null";
                    }
                    catch { }
                    return (false, null, null, $"Google login failed: {ex.Message}. Server ClientId: {clientIdConfig}, Token aud: {aud}");
                }
                var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == payload.Email);
                if (user == null)
                {
                    user = new User
                    {
                        Id = Guid.NewGuid(),
                        Email = payload.Email,
                        FullName = payload.Name,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()),
                        CreatedAt = DateTime.UtcNow,
                        IsLocked = false
                    };
                    _context.Users.Add(user);
                }
                var sessionId = Guid.NewGuid().ToString("N");
                user.SessionId = sessionId;
                var oldTokens = _context.RefreshTokens.Where(rt => rt.UserId == user.Id);
                _context.RefreshTokens.RemoveRange(oldTokens);
                var refreshToken = new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                };
                _context.RefreshTokens.Add(refreshToken);
                await _context.SaveChangesAsync();
                var token = GenerateJwtToken(user, sessionId, TimeSpan.FromMinutes(20)); // Sửa từ Microseconds thành Minutes
                return (true, token, refreshToken.Token, null);
            }
            catch (Exception ex)
            {
                return (false, null, null, "Google login failed: " + ex.Message);
            }
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
        // Thay đổi mật khẩu
        // Xác thực mật khẩu cũ và cập nhật mật khẩu mới
        // Trả về true nếu thành công, false nếu không thành công (ví dụ:
        // mật khẩu cũ không đúng hoặc người dùng không tồn tại)
        // Trả về false nếu không thành công, true nếu thành công
        public async Task<bool> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }
            if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
            {
                return false; // Old password does not match
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            return true;
        }
        // xử lý khi quên mật khẩu
        public async Task<(bool Success, string ErrorMessage)> ForgotPasswordAsync(string email)
        {
            try
            {
                var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    return (false, "Email không tồn tại trong hệ thống.");
                }

                // Tạo mật khẩu ngẫu nhiên (temporary password)
                string temporaryPassword = GenerateRandomPassword(8); // Độ dài 8 ký tự
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);

                await _context.SaveChangesAsync();

                // Gửi email chứa mật khẩu tạm thời
                await SendPasswordResetEmail(user.Email, temporaryPassword);

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Có lỗi xảy ra khi xử lý yêu cầu: {ex.Message}");
            }
        }
        // Lay ngau nhien Password
        private string GenerateRandomPassword(int length)
        {
            const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            var chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = validChars[random.Next(validChars.Length)];
            }
            return new string(chars);
        }
        // Gửi email chứa mật khẩu tạm thời
                private async Task SendPasswordResetEmail(string email, string temporaryPassword)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var smtpClient = new SmtpClient(emailSettings["SmtpServer"]) // Sử dụng SmtpServer làm hostname
            {
                Port = int.Parse(emailSettings["SmtpPort"]), // Parse port từ chuỗi
                Credentials = new NetworkCredential(emailSettings["SmtpUsername"], emailSettings["SmtpPassword"]),
                EnableSsl = bool.Parse(emailSettings["EnableSsl"]), // Parse EnableSsl
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(emailSettings["SenderEmail"], emailSettings["SenderName"]),
                Subject = "Yêu cầu đặt lại mật khẩu",
                Body = $"Mật khẩu tạm thời của bạn là: {temporaryPassword}. Vui lòng đăng nhập và thay đổi mật khẩu trong trang Profile.",
                IsBodyHtml = true,
            };
            mailMessage.To.Add(email);

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
                Console.WriteLine($"Email sent successfully to {email}");
            }
            catch (SmtpException ex)
            {
                Console.WriteLine($"SmtpException: {ex.Message} - StatusCode: {ex.StatusCode}");
                throw; // Ném lại để xử lý ở cấp cao hơn
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Exception: {ex.Message} - StackTrace: {ex.StackTrace}");
                throw; // Ném lại để xử lý
            }
        }
                        /*  "EmailSettings": {
            "SmtpServer": "smtp.gmail.com",
            "SmtpPort": 587,
            "SmtpUsername": "phamdat12213443@gmail.com",
            "SmtpPassword": "elykdbtlxjxicyao",
            "SenderName": "WebApplication1 Admin",
            "SenderEmail": "phamdat12213443@gmail.com",
            "EnableSsl": true
            },
                */
        // Generates a JWT token for the user
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