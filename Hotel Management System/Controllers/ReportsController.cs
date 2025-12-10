using Hotel_Management_System.Data;
using Hotel_Management_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Hotel_Management_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // Booking History
        // =========================

        // GET: Reports/BookingHistory
        public async Task<IActionResult> BookingHistory()
        {
            var rooms = await _context.Rooms
                .Include(r => r.Bookings)
                    .ThenInclude(b => b.Guest)
                .OrderBy(r => r.RoomNumber)
                .ToListAsync();

            return View(rooms);
        }

        // GET: Reports/BookingHistoryPdf
        public async Task<IActionResult> BookingHistoryPdf()
        {
            var rooms = await _context.Rooms
                .Include(r => r.Bookings)
                    .ThenInclude(b => b.Guest)
                .OrderBy(r => r.RoomNumber)
                .ToListAsync();

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Header().Text("Room Booking History").FontSize(20).Bold().AlignCenter();
                    page.Content().Stack(stack =>
                    {
                        foreach (var room in rooms)
                        {
                            stack.Item().Text($"Room {room.RoomNumber} ({room.RoomType})").Bold().FontSize(16);

                            if (room.Bookings.Any())
                            {
                                stack.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3); // Guest Name
                                        columns.RelativeColumn(2); // Check-In
                                        columns.RelativeColumn(2); // Check-Out
                                        columns.RelativeColumn(2); // Status
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Text("Guest Name").Bold();
                                        header.Cell().Text("Check-In").Bold();
                                        header.Cell().Text("Check-Out").Bold();
                                        header.Cell().Text("Status").Bold();
                                    });

                                    foreach (var booking in room.Bookings.OrderBy(b => b.CheckInDate))
                                    {
                                        table.Cell().Text(booking.Guest?.Name ?? "N/A");
                                        table.Cell().Text(booking.CheckInDate.ToString("yyyy-MM-dd"));
                                        table.Cell().Text(booking.CheckOutDate.ToString("yyyy-MM-dd"));
                                        table.Cell().Text(booking.BookingStatus.ToString());
                                    }
                                });
                            }
                            else
                            {
                                stack.Item().Text("No bookings").Italic();
                            }

                            stack.Item().PaddingVertical(5).Element(container =>
                            {
                                container.LineHorizontal(1);
                            });
                        }
                    });
                });
            });

            var pdfBytes = pdf.GeneratePdf();
            return File(pdfBytes, "application/pdf", "BookingHistory.pdf");
        }

        // =========================
        // Revenue Report
        // =========================

        // GET: Reports/Revenue
        public async Task<IActionResult> Revenue()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Room)
                .ToListAsync();

            // Overall stats
            ViewBag.TotalRevenue = bookings.Sum(b => b.Room.Price * (b.CheckOutDate - b.CheckInDate).Days);
            ViewBag.TotalBookings = bookings.Count;
            ViewBag.Confirmed = bookings.Count(b => b.BookingStatus == BookingStatus.Confirmed);
            ViewBag.Cancelled = bookings.Count(b => b.BookingStatus == BookingStatus.Cancelled);
            ViewBag.Completed = bookings.Count(b => b.BookingStatus == BookingStatus.Completed);

            // Revenue by Month
            ViewBag.RevenueByMonth = bookings
                .GroupBy(b => new { b.CheckInDate.Year, b.CheckInDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(b => b.Room.Price * (b.CheckOutDate - b.CheckInDate).Days)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();

            // Revenue by Room Type
            ViewBag.RevenueByRoomType = bookings
                .GroupBy(b => b.Room.RoomType)
                .Select(g => new
                {
                    RoomType = g.Key,
                    Revenue = g.Sum(b => b.Room.Price * (b.CheckOutDate - b.CheckInDate).Days)
                })
                .ToList();

            return View(); // renders Revenue Report view
        }

        // Optional: Export Revenue Report as PDF
        public async Task<IActionResult> RevenuePdf()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Room)
                .ToListAsync();

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Header().Text("Revenue Report").FontSize(20).Bold().AlignCenter();

                    page.Content().Stack(stack =>
                    {
                        // Overall totals
                        var totalRevenue = bookings.Sum(b => b.Room.Price * (b.CheckOutDate - b.CheckInDate).Days);
                        var totalBookings = bookings.Count;
                        var confirmed = bookings.Count(b => b.BookingStatus == BookingStatus.Confirmed);
                        var cancelled = bookings.Count(b => b.BookingStatus == BookingStatus.Cancelled);
                        var completed = bookings.Count(b => b.BookingStatus == BookingStatus.Completed);

                        stack.Item().Text($"Total Revenue: {totalRevenue} EGP").FontSize(14).Bold();
                        stack.Item().Text($"Total Bookings: {totalBookings}").FontSize(14);
                        stack.Item().Text($"Confirmed: {confirmed}").FontSize(14);
                        stack.Item().Text($"Cancelled: {cancelled}").FontSize(14);
                        stack.Item().Text($"Completed: {completed}").FontSize(14);

                        stack.Item().PaddingVertical(10);

                        // Revenue by Month
                        stack.Item().Text("Revenue by Month").FontSize(16).Bold();
                        stack.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2); // Year
                                columns.RelativeColumn(2); // Month
                                columns.RelativeColumn(3); // Revenue
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Year").Bold();
                                header.Cell().Text("Month").Bold();
                                header.Cell().Text("Revenue").Bold();
                            });

                            var revenueByMonth = bookings
                                .GroupBy(b => new { b.CheckInDate.Year, b.CheckInDate.Month })
                                .Select(g => new
                                {
                                    Year = g.Key.Year,
                                    Month = g.Key.Month,
                                    Revenue = g.Sum(b => b.Room.Price * (b.CheckOutDate - b.CheckInDate).Days)
                                })
                                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                                .ToList();

                            foreach (var item in revenueByMonth)
                            {
                                table.Cell().Text(item.Year.ToString());
                                table.Cell().Text(item.Month.ToString());
                                table.Cell().Text(item.Revenue.ToString());
                            }
                        });

                        stack.Item().PaddingVertical(10);

                        // Revenue by Room Type
                        stack.Item().Text("Revenue by Room Type").FontSize(16).Bold();
                        stack.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3); // Room Type
                                columns.RelativeColumn(3); // Revenue
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Room Type").Bold();
                                header.Cell().Text("Revenue").Bold();
                            });

                            var revenueByRoomType = bookings
                                .GroupBy(b => b.Room.RoomType)
                                .Select(g => new
                                {
                                    RoomType = g.Key,
                                    Revenue = g.Sum(b => b.Room.Price * (b.CheckOutDate - b.CheckInDate).Days)
                                })
                                .ToList();

                            foreach (var item in revenueByRoomType)
                            {
                                table.Cell().Text(item.RoomType);
                                table.Cell().Text(item.Revenue.ToString());
                            }
                        });

                    });
                });
            });

            var pdfBytes = pdf.GeneratePdf();
            return File(pdfBytes, "application/pdf", "RevenueReport.pdf");
        }
    }
}
