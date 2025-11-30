// GET: Receipts/GuestReceipt/5
using Hotel_Management_System.Models;
using Hotel_Management_System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class ReceiptsController : Controller
{
    private readonly ApplicationDbContext _context;
    public ReceiptsController(ApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<IActionResult> GuestReceipt(int bookingId)
    {
        var booking = await _context.Bookings
            .Include(b => b.Guest)
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.BookingID == bookingId);

        if (booking == null)
        {
            return NotFound();
        }

        // Optional: create receipt object for display
        var receipt = new Receipt
        {
            BookingID = booking.BookingID,
            TotalAmount = (booking.CheckOutDate - booking.CheckInDate).Days * booking.Room.Price,
            DateCreated = DateTime.Now,
            Booking = booking
        };

        return View(receipt);
    }
}
