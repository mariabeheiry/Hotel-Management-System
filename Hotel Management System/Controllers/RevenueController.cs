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

        // Main Revenue View
        public async Task<IActionResult> Revenue(int? month, int? year)
        {
            await PrepareRevenueData(month, year);
            return View();
        }

        // Partial view for AJAX filtering
        public async Task<IActionResult> RevenuePartial(int? month, int? year)
        {
            await PrepareRevenueData(month, year);
            return PartialView("RevenuePartial");
        }

        // PDF generation
        public async Task<IActionResult> RevenuePdf(int? month, int? year)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.Receipt)
                .ToListAsync();

            // Build per-day revenue list
            var dailyRevenue = new List<(DateTime Date, decimal Amount, string RoomType)>();
            foreach (var b in bookings)
            {
                var days = (b.CheckOutDate - b.CheckInDate).Days;
                if (days <= 0) continue;
                decimal perDay = b.Room.Price;
                for (int i = 0; i < days; i++)
                {
                    var date = b.CheckInDate.AddDays(i);
                    if (b.Receipt != null)
                        dailyRevenue.Add((date, perDay, b.Room.RoomType));
                }
            }

            // Apply filtering
            if (month.HasValue)
                dailyRevenue = dailyRevenue.Where(d => d.Date.Month == month.Value).ToList();
            if (year.HasValue)
                dailyRevenue = dailyRevenue.Where(d => d.Date.Year == year.Value).ToList();

            var totalRevenue = dailyRevenue.Sum(d => d.Amount);
            var totalBookings = bookings.Count;
            var confirmed = bookings.Count(b => b.BookingStatus == BookingStatus.Confirmed);
            var cancelled = bookings.Count(b => b.BookingStatus == BookingStatus.Cancelled);
            var completed = bookings.Count(b => b.BookingStatus == BookingStatus.Completed);

            // Revenue by Month
            var revenueByMonth = dailyRevenue
                .GroupBy(d => new { d.Date.Year, d.Date.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(x => x.Amount)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();

            // Revenue by Room Type
            var revenueByRoomType = dailyRevenue
                .GroupBy(d => d.RoomType)
                .Select(g => new
                {
                    RoomType = g.Key,
                    Revenue = g.Sum(x => x.Amount)
                })
                .ToList();

            // PDF generation using QuestPDF
            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Header().Text("Revenue Report").FontSize(20).Bold().AlignCenter();

                    page.Content().Stack(stack =>
                    {
                        stack.Item().Text($"Total Revenue: {totalRevenue} USD").Bold();
                        stack.Item().Text($"Total Bookings: {totalBookings}");
                        stack.Item().Text($"Confirmed: {confirmed}");
                        stack.Item().Text($"Cancelled: {cancelled}");
                        stack.Item().Text($"Completed: {completed}");

                        stack.Item().PaddingVertical(10);
                        stack.Item().Text("Revenue by Month").Bold();
                        stack.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Year").Bold();
                                header.Cell().Text("Month").Bold();
                                header.Cell().Text("Revenue").Bold();
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
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(3);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Room Type").Bold();
                                header.Cell().Text("Revenue").Bold();
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

            var pdfBytes = pdf.GeneratePdf();
            return File(pdfBytes, "application/pdf", "RevenueReport.pdf");
        }

        // Helper method to prepare ViewBag data
        private async Task PrepareRevenueData(int? month, int? year)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.Receipt)
                .ToListAsync();

            // Build per-day revenue list
            var dailyRevenue = new List<(DateTime Date, decimal Amount, string RoomType)>();
            foreach (var b in bookings)
            {
                var days = (b.CheckOutDate - b.CheckInDate).Days;
                if (days <= 0) continue;
                decimal perDay = b.Room.Price;
                for (int i = 0; i < days; i++)
                {
                    var date = b.CheckInDate.AddDays(i);
                    if (b.Receipt != null)
                        dailyRevenue.Add((date, perDay, b.Room.RoomType));
                }
            }

            // Apply filtering
            if (month.HasValue)
                dailyRevenue = dailyRevenue.Where(d => d.Date.Month == month.Value).ToList();
            if (year.HasValue)
                dailyRevenue = dailyRevenue.Where(d => d.Date.Year == year.Value).ToList();

            ViewBag.TotalRevenue = dailyRevenue.Sum(d => d.Amount);
            ViewBag.TotalBookings = bookings.Count;
            ViewBag.Confirmed = bookings.Count(b => b.BookingStatus == BookingStatus.Confirmed);
            ViewBag.Cancelled = bookings.Count(b => b.BookingStatus == BookingStatus.Cancelled);
            ViewBag.Completed = bookings.Count(b => b.BookingStatus == BookingStatus.Completed);

            ViewBag.RevenueByMonth = dailyRevenue
                .GroupBy(d => new { d.Date.Year, d.Date.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(x => x.Amount)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();

            ViewBag.RevenueByRoomType = dailyRevenue
                .GroupBy(d => d.RoomType)
                .Select(g => new
                {
                    RoomType = g.Key,
                    Revenue = g.Sum(x => x.Amount)
                })
                .ToList();

            // Fix: include all years from bookings and receipts
            var bookingYears = bookings
                .SelectMany(b => Enumerable.Range(b.CheckInDate.Year, b.CheckOutDate.Year - b.CheckInDate.Year + 1))
                .Distinct();

            var receiptYears = await _context.Receipts
                .Select(r => r.DateCreated.Year)
                .Distinct()
                .ToListAsync();

            ViewBag.Years = bookingYears
                .Union(receiptYears)
                .OrderBy(y => y)
                .ToList();

            ViewBag.SelectedMonth = month;
            ViewBag.SelectedYear = year;
        }

    }
}
