using Hotel_Management_System.Data;
using Hotel_Management_System.Helpers;
using Hotel_Management_System.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;


namespace Hotel_Management_System.Controllers
{
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public BookingsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
            var booking = await _context.Bookings
                .Include(b => b.Receipt)
                .FirstOrDefaultAsync(b => b.BookingID == id);

            if (booking == null) return NotFound();

            // Make room available again
            var room = await _context.Rooms.FindAsync(booking.RoomID);
            if (room != null)
            {
                room.IsAvailable = true;
            }

            // Delete receipt if exists
            if (booking.Receipt != null)
            {
                _context.Receipts.Remove(booking.Receipt);
            }

            // Delete booking
            _context.Bookings.Remove(booking);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> Search(DateTime? checkIn, DateTime? checkOut)
        {
            var rooms = await _context.Rooms
                .Include(r => r.Bookings)
                .ToListAsync();

            // Filtering by date window
            if (checkIn != null && checkOut != null)
            {
                rooms = rooms
                    .Where(r => !r.Bookings.Any(b =>
                        (checkIn < b.CheckOutDate && checkOut > b.CheckInDate) &&
                        b.BookingStatus == BookingStatus.Confirmed))
                    .ToList();
            }

            ViewBag.CheckIn = checkIn;
            ViewBag.CheckOut = checkOut;

            return View(rooms);
        }

        [HttpPost]
        public async Task<IActionResult> AddRoomToBooking(int roomId, DateTime checkIn, DateTime checkOut)
        {
            var cart = HttpContext.Session.GetObject<List<int>>("RoomCart") ?? new List<int>();

            if (!cart.Contains(roomId))
                cart.Add(roomId);

            HttpContext.Session.SetObject("RoomCart", cart);
            HttpContext.Session.SetString("CheckIn", checkIn.ToString());
            HttpContext.Session.SetString("CheckOut", checkOut.ToString());

            TempData["CartMessage"] = "Room added to cart successfully!";

            return RedirectToAction("Search", new
            {
                checkIn = checkIn.ToString("yyyy-MM-dd"),
                checkOut = checkOut.ToString("yyyy-MM-dd")
            });
        }


        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int roomId)
        {
            var cart = HttpContext.Session.GetObject<List<int>>("RoomCart") ?? new List<int>();
            cart.Remove(roomId);
            HttpContext.Session.SetObject("RoomCart", cart);

            var roomsInCart = await _context.Rooms
                .Where(r => cart.Contains(r.RoomID))
                .ToListAsync();

            return View("ConfirmBooking", roomsInCart);
        }
        [HttpPost]
        public async Task<IActionResult> ConfirmBooking()
        {
            var cart = HttpContext.Session.GetObject<List<int>>("RoomCart") ?? new List<int>();
            if (!cart.Any()) return RedirectToAction("Search");

            var checkIn = DateTime.Parse(HttpContext.Session.GetString("CheckIn") ?? "");
            var checkOut = DateTime.Parse(HttpContext.Session.GetString("CheckOut") ?? "");

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var guest = await _context.Guests.FirstOrDefaultAsync(g => g.Email == currentUser.Email);
            if (guest == null) return RedirectToAction("Index", "Home");

            foreach (var roomId in cart)
            {
                var room = await _context.Rooms.FindAsync(roomId);
                if (room == null || !room.IsAvailable) continue; // skip unavailable rooms

                var booking = new Booking
                {
                    RoomID = roomId,
                    GuestID = guest.GuestID,
                    CheckInDate = checkIn,
                    CheckOutDate = checkOut,
                    BookingStatus = BookingStatus.Confirmed
                };
                _context.Bookings.Add(booking);

                // Mark room as unavailable
                room.IsAvailable = false;
            }

            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("RoomCart");
            HttpContext.Session.Remove("CheckIn");
            HttpContext.Session.Remove("CheckOut");

            return RedirectToAction("Index"); // Redirect to user's bookings page
        }

        // GET: ConfirmBooking page
        public async Task<IActionResult> ConfirmBookingPage()
        {
            var cart = HttpContext.Session.GetObject<List<int>>("RoomCart") ?? new List<int>();

            var roomsInCart = await _context.Rooms
                .Where(r => cart.Contains(r.RoomID))
                .ToListAsync();

            return View("ConfirmBooking", roomsInCart);
        }


    }
}
