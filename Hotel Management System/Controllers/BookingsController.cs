// Inside BookingsController class
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hotel_Management_System.Models;
using Hotel_Management_System.Data;

public class BookingsController : Controller
{
    private readonly ApplicationDbContext _context;
    public BookingsController(ApplicationDbContext context)
    {
        _context = context;
    }
    // AJAX Search Action
    public IActionResult Search(string term, string statusFilter)
    {
        var bookings = _context.Bookings
            .Include(b => b.Guest)
            .Include(b => b.Room)
            .AsQueryable();

        // Filter by search term (Guest name or Room number)
        if (!string.IsNullOrEmpty(term))
        {
            bookings = bookings.Where(b =>
                b.Guest.Name.Contains(term) ||
                b.Room.RoomNumber.ToString().Contains(term));
        }

        // Filter by BookingStatus
        if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "All")
        {
            bookings = bookings.Where(b => b.BookingStatus == statusFilter);
        }

        bookings = bookings.OrderBy(b => b.CheckInDate);

        return PartialView("_BookingSearchResults", bookings.ToList());
    }
}
