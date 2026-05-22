using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using panelapp.Data;
using panelapp.Extensions;
using panelapp.Helpers;
using panelapp.Security;

namespace panelapp.Controllers
{
    [SessionAuthorize]
    public class ActivityLogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        private const int DefaultPageSize = 20;
        private static readonly int[] AllowedPageSizes = { 10, 20, 50, 100 };

        public ActivityLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = DefaultPageSize)
        {
            var username = HttpContext.Session.GetString("Username");
            var isAdmin = HttpContext.IsAdmin();

            var query = _context.ActivityLogs.AsNoTracking().AsQueryable();

            if (!AllowedPageSizes.Contains(pageSize))
            {
                pageSize = DefaultPageSize;
            }

            if (!isAdmin)
            {
                query = query.Where(x => x.UserName == username);
            }

            var totalCount = await query.CountAsync();
            var totalPages = PaginationHelper.GetTotalPages(totalCount, pageSize);

            page = PaginationHelper.NormalizePage(page, totalPages);

            var logs = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.IsAdmin = isAdmin;
            ViewBag.PageSize = pageSize;

            return View(logs);
        }
    }
}