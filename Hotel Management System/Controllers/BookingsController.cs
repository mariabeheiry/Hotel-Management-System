using Hotel_Management_System.Data;
using Hotel_Management_System.Helpers;
using Hotel_Management_System.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;

namespace Hotel_Management_System.Controllers
{
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;

        public BookingsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }


        //public BookingsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        //{
        //    _context = context;
        //    _userManager = userManager;
        //}

        // GET: Bookings (list)
        public async Task<IActionResult> Index()
        {
            var bookings = _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.Guest)
                .Include(b => b.Receipt);

            return View(await bookings.ToListAsync());
        }

        // Details
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

        // Single create (fallback)
        public IActionResult Create()
        {
            ViewData["RoomID"] = new SelectList(_context.Rooms.Where(r => r.IsAvailable), "RoomID", "RoomNumber");
            ViewData["GuestID"] = new SelectList(_context.Guests, "GuestID", "Name");
            return View();
        }

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
                if (room == null) ModelState.AddModelError("", "Selected room not found.");

                int nights = (int)(booking.CheckOutDate - booking.CheckInDate).TotalDays;
                decimal amount = (room != null) ? room.Price * nights : 0m;

                var receipt = new Receipt
                {
                    Booking = booking,
                    TotalAmount = amount
                };

                booking.Receipt = receipt;
                _context.Add(booking);

                if (room != null) room.IsAvailable = false;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["RoomID"] = new SelectList(_context.Rooms.Where(r => r.IsAvailable), "RoomID", "RoomNumber", booking.RoomID);
            ViewData["GuestID"] = new SelectList(_context.Guests, "GuestID", "Name", booking.GuestID);

            return View(booking);
        }

        // Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            ViewData["RoomID"] = new SelectList(_context.Rooms, "RoomID", "RoomNumber", booking.RoomID);
            ViewData["GuestID"] = new SelectList(_context.Guests, "GuestID", "Name", booking.GuestID);

            return View(booking);
        }

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

            // Update Completed status if passed
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

        // Delete
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

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Receipt)
                .FirstOrDefaultAsync(b => b.BookingID == id);

            if (booking == null) return NotFound();

            var room = await _context.Rooms.FindAsync(booking.RoomID);
            if (room != null)
            {
                // mark available; Search will recompute as well
                room.IsAvailable = true;
            }

            if (booking.Receipt != null)
            {
                _context.Receipts.Remove(booking.Receipt);
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // SEARCH: recompute availability & show only when dates provided
        public async Task<IActionResult> Search(DateTime? checkIn, DateTime? checkOut)
        {
            var today = DateTime.Now.Date;

            // load rooms + bookings
            var allRooms = await _context.Rooms
                .Include(r => r.Bookings)
                .ToListAsync();

            // update booking statuses (complete outdated bookings) and recompute room availability
            bool changed = false;
            foreach (var room in allRooms)
            {
                // update booking statuses where checkout has passed and not cancelled/completed yet
                foreach (var b in room.Bookings)
                {
                    if (b.BookingStatus == BookingStatus.Confirmed && b.CheckOutDate.Date < today)
                    {
                        b.BookingStatus = BookingStatus.Completed;
                        _context.Bookings.Update(b);
                        changed = true;
                    }
                }

                // A room is available if there is NO confirmed booking whose CheckOutDate >= today
                var hasActiveConfirmed = room.Bookings
                    .Any(b => b.BookingStatus == BookingStatus.Confirmed && b.CheckOutDate.Date >= today);

                var newAvailability = !hasActiveConfirmed;
                if (room.IsAvailable != newAvailability)
                {
                    room.IsAvailable = newAvailability;
                    _context.Rooms.Update(room);
                    changed = true;
                }
            }

            if (changed) await _context.SaveChangesAsync();

            // If dates not supplied => show nothing and prompt user
            if (checkIn == null || checkOut == null)
            {
                ViewBag.CheckIn = null;
                ViewBag.CheckOut = null;
                return View(new List<Room>());
            }

            // validate date ordering
            if (checkOut <= checkIn)
            {
                TempData["SearchError"] = "Check-out date must be after check-in date.";
                ViewBag.CheckIn = checkIn;
                ViewBag.CheckOut = checkOut;
                return View(new List<Room>());
            }

            // Filter: room is available for requested range if it has no confirmed booking overlapping requested window
            var roomsAvailable = allRooms
                .Where(r => !r.Bookings.Any(b =>
                    b.BookingStatus == BookingStatus.Confirmed &&
                    (checkIn < b.CheckOutDate && checkOut > b.CheckInDate)
                ))
                .OrderBy(r => r.RoomNumber)
                .ToList();

            ViewBag.CheckIn = checkIn;
            ViewBag.CheckOut = checkOut;

            return View(roomsAvailable);
        }

        // Add room to session cart (requires dates)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddRoomToBooking(int roomId, DateTime? checkIn, DateTime? checkOut)
        {
            // enforce presence of dates
            if (checkIn == null || checkOut == null)
            {
                TempData["CartMessage"] = "Please select check-in and check-out dates and press Search first.";
                return RedirectToAction("Search");
            }

            if (checkOut <= checkIn)
            {
                TempData["CartMessage"] = "Invalid dates: Check-out must be after check-in.";
                return RedirectToAction("Search", new { checkIn = checkIn?.ToString("yyyy-MM-dd"), checkOut = checkOut?.ToString("yyyy-MM-dd") });
            }

            if (checkIn.Value.Date < DateTime.Today)
            {
                TempData["CartMessage"] = "Check-in date cannot be in the past.";
                return RedirectToAction("Search");
            }

            if (checkOut.Value.Date <= checkIn.Value.Date)
            {
                TempData["CartMessage"] = "Check-out must be after check-in date.";
                return RedirectToAction("Search");
            }


            var cart = HttpContext.Session.GetObject<List<int>>("RoomCart") ?? new List<int>();

            if (!cart.Contains(roomId))
                cart.Add(roomId);

            HttpContext.Session.SetObject("RoomCart", cart);
            HttpContext.Session.SetString("CheckIn", checkIn.Value.ToString("yyyy-MM-dd"));
            HttpContext.Session.SetString("CheckOut", checkOut.Value.ToString("yyyy-MM-dd"));

            TempData["CartMessage"] = "Room added to cart successfully!";

            return RedirectToAction("Search", new
            {
                checkIn = checkIn.Value.ToString("yyyy-MM-dd"),
                checkOut = checkOut.Value.ToString("yyyy-MM-dd")
            });
        }

        // Remove room from cart
        [HttpPost]
        [ValidateAntiForgeryToken]
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

        // Show confirm booking page (POST from Search => uses session)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBookingPage()
        {
            var cart = HttpContext.Session.GetObject<List<int>>("RoomCart") ?? new List<int>();

            var roomsInCart = await _context.Rooms
                .Where(r => cart.Contains(r.RoomID))
                .ToListAsync();

            return View("ConfirmBooking", roomsInCart);
        }

        public async Task<IActionResult> MyBookings()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            // find the guest profile linked to this identity user
            var guest = await _context.Guests
                .FirstOrDefaultAsync(g => g.IdentityUserId == currentUser.Id);

            if (guest == null)
                return View(new List<Booking>()); // no bookings because no guest profile

            // get only this user's bookings
            var myBookings = await _context.Bookings
                .Where(b => b.GuestID == guest.GuestID)
                .Include(b => b.Room)
                .Include(b => b.Receipt) // important
                .ToListAsync();


            return View("MyBookings", myBookings); // show MyBookings view

        }

        // ConfirmBooking: create bookings + receipts and persist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking()
        {
            var cart = HttpContext.Session.GetObject<List<int>>("RoomCart") ?? new List<int>();
            if (!cart.Any()) return RedirectToAction("Search");

            var checkInString = HttpContext.Session.GetString("CheckIn");
            var checkOutString = HttpContext.Session.GetString("CheckOut");

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ",
                    ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));

                TempData["CartMessage"] = "ModelState invalid: " + errors;
                return RedirectToAction("ConfirmBookingPage");
            }

            if (string.IsNullOrEmpty(checkInString) || string.IsNullOrEmpty(checkOutString))
            {
                TempData["CartMessage"] = "Session dates missing. Please search again.";
                return RedirectToAction("Search");
            }


            var checkIn = DateTime.Parse(checkInString);
            var checkOut = DateTime.Parse(checkOutString);

            if (checkOut <= checkIn)
            {
                TempData["CartMessage"] = "Invalid dates stored in session. Please search again.";
                return RedirectToAction("Search");
            }

            if (checkIn.Date < DateTime.Today)
            {
                TempData["CartMessage"] = "Cannot confirm booking: check-in date is in the past.";
                return RedirectToAction("Search");
            }

            if (checkOut <= checkIn)
            {
                TempData["CartMessage"] = "Cannot confirm booking: invalid check-out date.";
                return RedirectToAction("Search");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account");

            var guest = await _context.Guests.FirstOrDefaultAsync(g => g.Email == currentUser.Email);
            if (guest == null) return RedirectToAction("Index", "Home");

            // Prepare email body
            var emailBody = $"Dear {guest.Name},\n\nYour booking has been confirmed for the following rooms:\n\n";

            // For each room in cart -> re-check availability and insert booking + receipt
            foreach (var roomId in cart)
            {
                var room = await _context.Rooms
                    .Include(r => r.Bookings)
                    .FirstOrDefaultAsync(r => r.RoomID == roomId);

                if (room == null) continue;

                // Check for overlapping confirmed bookings
                var hasConflict = room.Bookings.Any(b =>
                    b.BookingStatus == BookingStatus.Confirmed &&
                    (checkIn < b.CheckOutDate && checkOut > b.CheckInDate)
                );
                if (hasConflict) continue; // skip this room — it was taken in meantime

                var booking = new Booking
                {
                    RoomID = roomId,
                    GuestID = guest.GuestID,
                    CheckInDate = checkIn,
                    CheckOutDate = checkOut,
                    BookingStatus = BookingStatus.Confirmed
                };

                int nights = (int)(checkOut - checkIn).TotalDays;
                if (nights < 1) nights = 1;
                decimal amount = room.Price * nights;

                var receipt = new Receipt
                {
                    Booking = booking,
                    TotalAmount = amount
                };

                booking.Receipt = receipt;

                _context.Bookings.Add(booking);

                room.IsAvailable = false;
                _context.Rooms.Update(room);

                // Add room details to email body
                emailBody += $"Room Number: {room.RoomNumber}\n";
                emailBody += $"Room Type: {room.RoomType}\n";
                emailBody += $"Check-In: {booking.CheckInDate:yyyy-MM-dd}\n";
                emailBody += $"Check-Out: {booking.CheckOutDate:yyyy-MM-dd}\n";
                emailBody += $"Total Amount: {booking.Receipt.TotalAmount:C}\n\n";
            }

            await _context.SaveChangesAsync();

            // Send a single email for all rooms
            SendBookingConfirmationEmail(guest.Email, guest.Name, emailBody);

            HttpContext.Session.Remove("RoomCart");
            HttpContext.Session.Remove("CheckIn");
            HttpContext.Session.Remove("CheckOut");

            return RedirectToAction("MyBookings");
        }


        //private readonly IConfiguration _configuration;

        //public BookingsController(IConfiguration configuration)
        //{
        //    _configuration = configuration;
        //}

        private void SendBookingConfirmationEmail(
            string toEmail,
            string guestName,
            string emailBody)
        {
            var fromEmail = _configuration["EmailSettings:FromEmail"];
            var appPassword = _configuration["EmailSettings:AppPassword"];

            using (var client = new SmtpClient("smtp.gmail.com", 587))
            {
                client.Credentials =
                    new NetworkCredential(fromEmail, appPassword);
                client.EnableSsl = true;

                var mailMessage = new MailMessage();
                mailMessage.From =
                    new MailAddress(fromEmail, "Royal Stay Hotel");
                mailMessage.To.Add(toEmail);
                mailMessage.Subject = "Booking Confirmation";
                mailMessage.Body =
                    emailBody + "\nThank you for choosing our hotel!";
                mailMessage.IsBodyHtml = false;

                client.Send(mailMessage);
            }
        }


        public IActionResult SearchBookings(string term, string statusFilter)
        {
            var bookings = _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Room)
                .Include(b => b.Receipt)
                .AsQueryable();

            if (!string.IsNullOrEmpty(term))
            {
                bookings = bookings.Where(b => b.Guest.Name.Contains(term) || b.Room.RoomNumber.Contains(term));
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                // Parse the string to the BookingStatus enum
                if (Enum.TryParse<BookingStatus>(statusFilter, out var statusEnum))
                {
                    bookings = bookings.Where(b => b.BookingStatus == statusEnum);
                }
            }

            return PartialView("_BookingSearchResults", bookings.ToList());
        }

        // GET: Bookings/Cancel/5
        public async Task<IActionResult> Cancel(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.Guest)
                .FirstOrDefaultAsync(b => b.BookingID == id);

            if (booking == null) return NotFound();

            // Optional: make sure guest can only cancel their own booking
            if (User.IsInRole("Guest"))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                var guest = await _context.Guests.FirstOrDefaultAsync(g => g.IdentityUserId == currentUser.Id);
                if (guest == null || booking.GuestID != guest.GuestID)
                {
                    return Forbid();
                }
            }

            return View(booking); // confirm cancellation page
        }

        // POST: Bookings/Cancel/5
        [HttpPost, ActionName("Cancel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.Receipt)
                .FirstOrDefaultAsync(b => b.BookingID == id);

            if (booking == null) return NotFound();

            // 1️⃣ Update booking status
            booking.BookingStatus = BookingStatus.Cancelled;

            // 2️⃣ Make the room available again
            var room = await _context.Rooms.FindAsync(booking.RoomID);
            if (room != null)
            {
                room.IsAvailable = true;
                _context.Rooms.Update(room);
            }

            // 3️⃣ Optionally, remove receipt or mark it ignored for revenue
            if (booking.Receipt != null)
            {
                _context.Receipts.Remove(booking.Receipt);
            }

            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();

            // 4️⃣ Redirect back
            if (User.IsInRole("Admin"))
                return RedirectToAction(nameof(Index));
            else
                return RedirectToAction(nameof(MyBookings));
        }


    }
}
