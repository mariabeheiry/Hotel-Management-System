using Hotel_Management_System.Data;
using Hotel_Management_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hotel_Management_System.Controllers
{
    public class ReceiptsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReceiptsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Receipts
        public async Task<IActionResult> Index()
        {
            var receipts = _context.Receipts
                .Include(r => r.Booking)
                .ThenInclude(b => b.Guest)
                .Include(r => r.Booking.Room);

            return View(await receipts.ToListAsync());
        }

        // GET: Receipts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var receipt = await _context.Receipts
                .Include(r => r.Booking)
                    .ThenInclude(b => b.Guest)
                .Include(r => r.Booking.Room)
                .FirstOrDefaultAsync(r => r.ReceiptID == id);

            if (receipt == null) return NotFound();

            return View(receipt);
        }


        // GET: Receipts/Create
     
        public IActionResult Create()
        {
            // Only allow bookings that do NOT have receipts yet
            var availableBookings = _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Room)
                .Where(b => !_context.Receipts.Any(r => r.BookingID == b.BookingID))
                .ToList();

            ViewData["BookingID"] = new SelectList(
                availableBookings.Select(b => new
                {
                    b.BookingID,
                    Display = $"{b.BookingID} - {b.Guest.Name} - Room {b.Room.RoomNumber}"
                }),
                "BookingID", "Display"
            );

            return View();
        }

        // POST: Receipts/Create
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Receipt receipt)
        {
            if (!ModelState.IsValid)
            {
                PopulateBookingsForCreate(receipt.BookingID);
                return View(receipt);
            }

            // Prevent duplicate receipt for same booking
            if (_context.Receipts.Any(r => r.BookingID == receipt.BookingID))
            {
                ModelState.AddModelError("", "A receipt already exists for this booking.");
                PopulateBookingsForCreate(receipt.BookingID);
                return View(receipt);
            }

            // Auto-set issued date
            receipt.DateCreated = DateTime.Now;

            // Auto-calc total amount from booking
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.BookingID == receipt.BookingID);

            if (booking == null) return NotFound();

            int nights = (booking.CheckOutDate - booking.CheckInDate).Days;
            receipt.TotalAmount = booking.Room.Price * nights;

            _context.Add(receipt);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // Helper for reloading booking dropdown
        private void PopulateBookingsForCreate(int? selectedID = null)
        {
            var availableBookings = _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Room)
                .Where(b => !_context.Receipts.Any(r => r.BookingID == b.BookingID))
                .ToList();

            ViewData["BookingID"] = new SelectList(
                availableBookings.Select(b => new
                {
                    b.BookingID,
                    Display = $"{b.BookingID} - {b.Guest.Name} - Room {b.Room.RoomNumber}"
                }),
                "BookingID", "Display", selectedID
            );
        }

        // GET: Bookings/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var receipt = await _context.Receipts.FindAsync(id);
            if (receipt == null) return NotFound();

            ViewData["BookingID"] = new SelectList(_context.Bookings, "BookingID", "BookingID", receipt.BookingID);

            return View(receipt);
        }

        // POST: Bookings/Edit/5
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Receipt receipt)
        {
            if (id != receipt.ReceiptID) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewData["BookingID"] = new SelectList(_context.Bookings, "BookingID", "BookingID", receipt.BookingID);
                return View(receipt);
            }

            var booking = await _context.Bookings
                .Include(b => b.Room)
                .FirstOrDefaultAsync(b => b.BookingID == receipt.BookingID);

            if (booking == null) return NotFound();

            int nights = (booking.CheckOutDate - booking.CheckInDate).Days;
            receipt.TotalAmount = booking.Room.Price * nights;

            try
            {
                _context.Update(receipt);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Receipts.Any(r => r.ReceiptID == id))
                    return NotFound();
                else
                    throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Receipts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var receipt = await _context.Receipts
                .Include(r => r.Booking)
                    .ThenInclude(b => b.Guest)
                .Include(r => r.Booking.Room)
                .FirstOrDefaultAsync(m => m.ReceiptID == id);

            if (receipt == null) return NotFound();

            return View(receipt);
        }

        // POST: Receipts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var receipt = await _context.Receipts.FindAsync(id);
            if (receipt == null) return NotFound();

            _context.Receipts.Remove(receipt);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
