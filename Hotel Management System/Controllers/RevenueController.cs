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

        // ================= ACTIONS =================

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
            var dailyRevenue = await GetDailyRevenue(month, year);

            var totalRevenue = dailyRevenue.Sum(x => x.Amount);

            var revenueByMonth = dailyRevenue
                .GroupBy(x => new { x.Date.Year, x.Date.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Revenue = g.Sum(x => x.Amount)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();

            var revenueByRoomType = dailyRevenue
                .GroupBy(x => x.RoomType)
                .Select(g => new
                {
                    RoomType = g.Key,
                    Revenue = g.Sum(x => x.Amount)
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
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
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
                                table.Cell().Text(
                                    CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(item.Month));
                                table.Cell().Text(item.Revenue + " USD");
                            }
                        });

                        stack.Item().PaddingVertical(10);
                        stack.Item().Text("Revenue by Room Type").Bold();
                        stack.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.RelativeColumn();
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

        // ================= CORE LOGIC =================

        private record DailyRevenue(DateTime Date, decimal Amount, string RoomType);

        private async Task<List<DailyRevenue>> GetDailyRevenue(int? month, int? year)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.Receipt)
                .Where(b =>
                    b.BookingStatus != BookingStatus.Cancelled &&
                    b.Receipt != null)
                .ToListAsync();

            var dailyRevenue = new List<DailyRevenue>();

            foreach (var b in bookings)
            {
                var nights = (b.CheckOutDate - b.CheckInDate).Days;
                if (nights <= 0) continue;

                decimal perNight = b.Room.Price;

                for (int i = 0; i < nights; i++)
                {
                    var date = b.CheckInDate.AddDays(i);

                    dailyRevenue.Add(new DailyRevenue(
                        date,
                        perNight,
                        b.Room.RoomType
                    ));
                }
            }

            if (month.HasValue)
                dailyRevenue = dailyRevenue
                    .Where(x => x.Date.Month == month.Value)
                    .ToList();

            if (year.HasValue)
                dailyRevenue = dailyRevenue
                    .Where(x => x.Date.Year == year.Value)
                    .ToList();

            return dailyRevenue;
        }

        private async Task PrepareRevenueData(int? month, int? year)
        {
            var dailyRevenue = await GetDailyRevenue(month, year);

            var bookings = await _context.Bookings.ToListAsync();

            ViewBag.TotalRevenue = dailyRevenue.Sum(x => x.Amount);

            ViewBag.TotalBookings = bookings.Count;
            ViewBag.Confirmed = bookings.Count(b => b.BookingStatus == BookingStatus.Confirmed);
            ViewBag.Cancelled = bookings.Count(b => b.BookingStatus == BookingStatus.Cancelled);
            ViewBag.Completed = bookings.Count(b => b.BookingStatus == BookingStatus.Completed);

            ViewBag.RevenueByMonth = dailyRevenue
                .GroupBy(x => new { x.Date.Year, x.Date.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Revenue = g.Sum(x => x.Amount)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();

            ViewBag.RevenueByRoomType = dailyRevenue
                .GroupBy(x => x.RoomType)
                .Select(g => new
                {
                    RoomType = g.Key,
                    Revenue = g.Sum(x => x.Amount)
                })
                .ToList();

            ViewBag.Years = dailyRevenue
                .Select(x => x.Date.Year)
                .Distinct()
                .OrderBy(y => y)
                .ToList();

            ViewBag.SelectedMonth = month;
            ViewBag.SelectedYear = year;
        }
    }
}
