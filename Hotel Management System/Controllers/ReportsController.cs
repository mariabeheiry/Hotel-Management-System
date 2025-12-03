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

        // GET: Reports/BookingHistory
        public async Task<IActionResult> BookingHistory()
        {
            // Include bookings and guests
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
    }
}
