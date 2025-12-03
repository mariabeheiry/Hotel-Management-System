using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Hotel_Management_System.Data;
using Hotel_Management_System.Models;

namespace Hotel_Management_System.Controllers
{
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

       
        // GET: Bookings 
        public async Task<IActionResult> Index()
        {
            var bookings = _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.Guest)
                .Include(b => b.Receipt);

            return View(await bookings.ToListAsync());
        }

        
        // GET: Bookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.Guest)
                .Include(b => b.Receipt)
                .FirstOrDefaultAsync(m => m.BookingID == id);

            if (booking == null) return NotFound();

            return View(booking);
        }


        // GET: Bookings/Create
        public IActionResult Create()
        {
            ViewData["RoomID"] = new SelectList(_context.Rooms.Where(r => r.IsAvailable), "RoomID", "RoomNumber");
            ViewData["GuestID"] = new SelectList(_context.Guests, "GuestID", "Name");
            return View();
        }


        // POST: Bookings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BookingID,GuestID,RoomID,CheckInDate,CheckOutDate")] Booking booking)
        {
            booking.BookingStatus = BookingStatus.Confirmed;

            if (booking.CheckOutDate <= booking.CheckInDate)
            {
                ModelState.AddModelError("CheckOutDate", "Check-out must be after check-in");

                ViewData["RoomID"] = new SelectList(_context.Rooms.Where(r => r.IsAvailable), "RoomID", "RoomNumber", booking.RoomID);
                ViewData["GuestID"] = new SelectList(_context.Guests, "GuestID", "Name", booking.GuestID);

                return View(booking);
            }

            if (ModelState.IsValid)
            {
                var room = await _context.Rooms.FindAsync(booking.RoomID);

                int nights = (int)(booking.CheckOutDate - booking.CheckInDate).TotalDays;
                decimal amount = room.Price * nights;

                var receipt = new Receipt
                {
                    Booking = booking,
                    TotalAmount = amount
                };

                booking.Receipt = receipt;
                _context.Add(booking);

                room.IsAvailable = false;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["RoomID"] = new SelectList(_context.Rooms.Where(r => r.IsAvailable), "RoomID", "RoomNumber", booking.RoomID);
            ViewData["GuestID"] = new SelectList(_context.Guests, "GuestID", "Name", booking.GuestID);

            return View(booking);
        }


        // GET: Bookings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            ViewData["RoomID"] = new SelectList(_context.Rooms, "RoomID", "RoomNumber", booking.RoomID);
            ViewData["GuestID"] = new SelectList(_context.Guests, "GuestID", "Name", booking.GuestID);

            return View(booking);
        }


        // POST: Bookings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Booking booking)
        {
            if (id != booking.BookingID) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewData["RoomID"] = new SelectList(_context.Rooms, "RoomID", "RoomNumber", booking.RoomID);
                ViewData["GuestID"] = new SelectList(_context.Guests, "GuestID", "Name", booking.GuestID);
                return View(booking);
            }

            if (DateTime.Now.Date >= booking.CheckOutDate.Date &&
                booking.BookingStatus != BookingStatus.Cancelled)
            {
                booking.BookingStatus = BookingStatus.Completed;
            }

            var oldBooking = await _context.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.BookingID == id);
            if (oldBooking == null) return NotFound();

            if (oldBooking.RoomID != booking.RoomID)
            {
                var oldRoom = await _context.Rooms.FindAsync(oldBooking.RoomID);
                var newRoom = await _context.Rooms.FindAsync(booking.RoomID);

                if (oldRoom != null) oldRoom.IsAvailable = true;
                if (newRoom != null) newRoom.IsAvailable = false;
            }

            _context.Update(booking);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        // GET: Bookings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.Guest)
                .Include(b => b.Receipt)
                .FirstOrDefaultAsync(m => m.BookingID == id);

            if (booking == null) return NotFound();

            return View(booking);
        }


        // POST: Bookings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            var room = await _context.Rooms.FindAsync(booking.RoomID);
            if (room != null) room.IsAvailable = true;

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
