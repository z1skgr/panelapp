using panelapp.Data;
using panelapp.Models;

namespace panelapp.Services
{
    public interface IActivityLogService
    {
        Task LogAsync(
            string entityType,
            int? entityId,
            string actionType,
            string title,
            string? description = null);
    }

    public class ActivityLogService : IActivityLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActivityLogService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(
            string entityType,
            int? entityId,
            string actionType,
            string title,
            string? description = null)
        {
            var session = _httpContextAccessor.HttpContext?.Session;

            var userName = session?.GetString("Username");
            var userRole = session?.GetString("RoleName");

            var log = new ActivityLog
            {
                EntityType = entityType,
                EntityID = entityId,
                ActionType = actionType,
                Title = title,
                Description = description,
                UserName = userName,
                UserRole = userRole,
                CreatedAt = DateTime.Now
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}