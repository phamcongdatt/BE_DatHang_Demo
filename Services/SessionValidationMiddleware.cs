using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QuanLyDatHang.Data;
using System.Threading.Tasks;

namespace QuanLyDatHang.Services
{
    public class SessionValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            // Nếu endpoint có [AllowAnonymous] => bỏ qua xác thực
            if (endpoint?.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>() != null)
            {
                await _next(context);
                return;
            }
            // Nếu endpoint KHÔNG có [Authorize] => bỏ qua xác thực
            if (endpoint?.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.IAuthorizeData>() == null)
            {
                await _next(context);
                return;
            }

            var db = context.RequestServices.GetRequiredService<ApplicationDbContext>();
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Authorization header missing or invalid.");
                return;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;      
            var tokenSessionId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sessionId")?.Value;

            // Thêm log để kiểm tra giá trị sessionId
            Console.WriteLine($"[SessionValidation] userId: {userId}, tokenSessionId: {tokenSessionId}");

            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(tokenSessionId))
            {
                var user = await db.Users.FindAsync(Guid.Parse(userId));
                Console.WriteLine($"[SessionValidation] DB sessionId: {user?.SessionId}");
                if (user == null || user.SessionId != tokenSessionId)
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Session expired or invalid.");
                    Console.WriteLine($"[SessionValidation] Token sessionId: {tokenSessionId}, DB sessionId: {user?.SessionId} => INVALID");
                    return;
                }
                else
                {
                    Console.WriteLine($"[SessionValidation] Token sessionId: {tokenSessionId}, DB sessionId: {user?.SessionId} => VALID");
                }
            }

            await _next(context);
        }
    }
}