using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hotel_Management_System.Data;
using Hotel_Management_System.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace Hotel_Management_System.Controllers
{
    public class RevenueController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RevenueController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Revenue(int? month, int? year)
        {
            await PrepareRevenueData(month, year);
            return View();
        }

        public async Task<IActionResult> RevenuePartial(int? month, int? year)
        {
            await PrepareRevenueData(month, year);
            return PartialView("RevenuePartial");
        }

        public async Task<IActionResult> RevenuePdf(int? month, int? year)
        {
            var receipts = await GetFilteredReceipts(month, year);

            var totalRevenue = receipts.Sum(r => r.TotalAmount);

            var revenueByMonth = receipts
                .GroupBy(r => new { r.DateCreated.Year, r.DateCreated.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Revenue = g.Sum(x => x.TotalAmount)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();

            var revenueByRoomType = receipts
                .GroupBy(r => r.Booking.Room.RoomType)
                .Select(g => new
                {
                    RoomType = g.Key,
                    Revenue = g.Sum(x => x.TotalAmount)
                })
                .ToList();

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Header().Text("Revenue Report").FontSize(20).Bold().AlignCenter();

                    page.Content().Stack(stack =>
                    {
                        stack.Item().Text($"Total Revenue: {totalRevenue} USD").Bold();

                        stack.Item().PaddingVertical(10);
                        stack.Item().Text("Revenue by Month").Bold();
                        stack.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(h =>
                            {
                                h.Cell().Text("Year").Bold();
                                h.Cell().Text("Month").Bold();
                                h.Cell().Text("Revenue").Bold();
                            });

                            foreach (var item in revenueByMonth)
                            {
                                table.Cell().Text(item.Year.ToString());
                                table.Cell().Text(CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(item.Month));
                                table.Cell().Text(item.Revenue + " USD");
                            }
                        });

                        stack.Item().PaddingVertical(10);
                        stack.Item().Text("Revenue by Room Type").Bold();
                        stack.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(h =>
                            {
                                h.Cell().Text("Room Type").Bold();
                                h.Cell().Text("Revenue").Bold();
                            });

                            foreach (var item in revenueByRoomType)
                            {
                                table.Cell().Text(item.RoomType);
                                table.Cell().Text(item.Revenue + " USD");
                            }
                        });
                    });
                });
            });

            return File(pdf.GeneratePdf(), "application/pdf", "RevenueReport.pdf");
        }

        // ================= HELPERS =================

        private async Task<List<Receipt>> GetFilteredReceipts(int? month, int? year)
        {
            var query = _context.Receipts
                .Include(r => r.Booking)
                    .ThenInclude(b => b.Room)
                .Where(r => r.Booking.BookingStatus != BookingStatus.Cancelled);

            if (month.HasValue)
                query = query.Where(r => r.DateCreated.Month == month.Value);

            if (year.HasValue)
                query = query.Where(r => r.DateCreated.Year == year.Value);

            return await query.ToListAsync();
        }

        private async Task PrepareRevenueData(int? month, int? year)
        {
            var receipts = await GetFilteredReceipts(month, year);
            var bookings = await _context.Bookings.ToListAsync();

            ViewBag.TotalRevenue = receipts.Sum(r => r.TotalAmount);
            ViewBag.TotalBookings = bookings.Count;
            ViewBag.Confirmed = bookings.Count(b => b.BookingStatus == BookingStatus.Confirmed);
            ViewBag.Cancelled = bookings.Count(b => b.BookingStatus == BookingStatus.Cancelled);
            ViewBag.Completed = bookings.Count(b => b.BookingStatus == BookingStatus.Completed);

            ViewBag.RevenueByMonth = receipts
                .GroupBy(r => new { r.DateCreated.Year, r.DateCreated.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Revenue = g.Sum(x => x.TotalAmount)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();

            ViewBag.RevenueByRoomType = receipts
                .GroupBy(r => r.Booking.Room.RoomType)
                .Select(g => new
                {
                    RoomType = g.Key,
                    Revenue = g.Sum(x => x.TotalAmount)
                })
                .ToList();

            ViewBag.Years = receipts
                .Select(r => r.DateCreated.Year)
                .Distinct()
                .OrderBy(y => y)
                .ToList();

            ViewBag.SelectedMonth = month;
            ViewBag.SelectedYear = year;
        }
    }
}
