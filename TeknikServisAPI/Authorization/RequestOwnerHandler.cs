using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TeknikServisAPI.Constants;
using TeknikServisAPI.Data;

namespace TeknikServisAPI.Authorization
{
    public sealed class RequestOwnerHandler : AuthorizationHandler<RequestOwnerRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _context;

        public RequestOwnerHandler(IHttpContextAccessor httpContextAccessor, ApplicationDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            RequestOwnerRequirement requirement)
        {
            var user = context.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return;
            }

            // Kullanıcının rolünü al
            var userRole = user.FindFirst(ClaimTypes.Role)?.Value;

            // Rol yoksa reddet
            if (string.IsNullOrEmpty(userRole))
            {
                context.Fail();
                return;
            }

            // Bilinmeyen rolleri reddet (güvenlik: en düşük yetkiyle sınırla)
            if (!AppRoles.AllRoles.Contains(userRole))
            {
                context.Fail();
                return;
            }

            // İzinli roller (Admin, Proje, Teknik, Kullanici) sahiplik kontrolünden muaf
            if (AppRoles.PrivilegedRoles.Contains(userRole))
            {
                context.Succeed(requirement);
                return;
            }

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return;
            }

            if (!httpContext.Request.RouteValues.TryGetValue("id", out var idValue))
            {
                return;
            }

            if (!int.TryParse(idValue?.ToString(), out var requestId))
            {
                return;
            }

            var userIdValue = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdValue, out var userId))
            {
                return;
            }

            var isOwner = await _context.Requests
                .AsNoTracking()
                .AnyAsync(r => r.Id == requestId && r.CreatedByUserId == userId);

            if (isOwner)
            {
                context.Succeed(requirement);
            }
        }
    }
}
