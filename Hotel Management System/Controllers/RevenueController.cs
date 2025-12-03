using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hotel_Management_System.Data;
using Hotel_Management_System.Models;


namespace Hotel_Management_System.Controllers
{
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Revenue()
        {
            // Total revenue from all receipts
            var totalRevenue = await _context.Receipts
                .SumAsync(r => (decimal?)r.TotalAmount) ?? 0;

            // Total bookings count
            var totalBookings = await _context.Bookings.CountAsync();

            // Count per status (enum)
            var confirmedCount = await _context.Bookings
                .CountAsync(b => b.BookingStatus == BookingStatus.Confirmed);

            var cancelledCount = await _context.Bookings
                .CountAsync(b => b.BookingStatus == BookingStatus.Cancelled);

            var completedCount = await _context.Bookings
                .CountAsync(b => b.BookingStatus == BookingStatus.Completed);

            // Revenue by month
            var revenueByMonth = await _context.Receipts
                .GroupBy(r => new { r.DateCreated.Year, r.DateCreated.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(x => x.TotalAmount)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            // Revenue by room type
            var revenueByRoomType = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.Receipt)
                .Where(b => b.Receipt != null)
                .GroupBy(b => b.Room.RoomType)
                .Select(g => new
                {
                    RoomType = g.Key,
                    Revenue = g.Sum(x => x.Receipt.TotalAmount)
                })
                .ToListAsync();

            // Pass data to the View
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalBookings = totalBookings;

            ViewBag.Confirmed = confirmedCount;
            ViewBag.Cancelled = cancelledCount;
            ViewBag.Completed = completedCount;

            ViewBag.RevenueByMonth = revenueByMonth;
            ViewBag.RevenueByRoomType = revenueByRoomType;

            return View();
        }


    }
}
