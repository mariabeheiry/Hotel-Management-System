using Hotel_Management_System.Data;
using Hotel_Management_System.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hotel_Management_System.Controllers
{
    public class RoomsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoomsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Rooms
        public async Task<IActionResult> Index(string sortOrder)
        {
            ViewData["PriceSortParm"] = sortOrder == "price_asc" ? "price_desc" : "price_asc";
            ViewData["AvailabilitySortParm"] = sortOrder == "available" ? "available_desc" : "available";

            var rooms = _context.Rooms.AsQueryable();

            switch (sortOrder)
            {
                case "price_asc":
                    rooms = rooms.OrderBy(r => r.Price);
                    break;
                case "price_desc":
                    rooms = rooms.OrderByDescending(r => r.Price);
                    break;
                case "available":
                    rooms = rooms.OrderByDescending(r => r.IsAvailable);
                    break;
                case "available_desc":
                    rooms = rooms.OrderBy(r => r.IsAvailable);
                    break;
            }

            return View(await rooms.ToListAsync());
        }

        // GET: Rooms/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var room = await _context.Rooms.FirstOrDefaultAsync(m => m.RoomID == id);
            if (room == null) return NotFound();

            return View(room);
        }

        // GET: Rooms/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Rooms/Create
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RoomID,RoomNumber,RoomType,Price,IsAvailable")] Room room)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
            }

            if (ModelState.IsValid)
            {
                _context.Rooms.Add(room); //edited
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(room);
        }

        // GET: Rooms/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            return View(room);
        }

        // POST: Rooms/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("RoomID,RoomNumber,RoomType,Price,IsAvailable")] Room room)
        {
            if (id != room.RoomID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(room);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RoomExists(room.RoomID)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(room);
        }

        // GET: Rooms/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var room = await _context.Rooms.FirstOrDefaultAsync(m => m.RoomID == id);
            if (room == null) return NotFound();

            return View(room);
        }

        // POST: Rooms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room != null) _context.Rooms.Remove(room);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RoomExists(int id)
        {
            return _context.Rooms.Any(e => e.RoomID == id);
        }

        // GET: Rooms/Search
        public IActionResult Search(string term, string sortOrder)
        {
            var rooms = _context.Rooms.AsQueryable();

            // Filter by search term
            if (!string.IsNullOrEmpty(term))
            {
                rooms = rooms.Where(r => r.RoomNumber.ToString().Contains(term)
                                      || r.RoomType.Contains(term));
            }

            // Sort dynamically
            switch (sortOrder)
            {
                case "price_asc":
                    rooms = rooms.OrderBy(r => r.Price);
                    break;
                case "price_desc":
                    rooms = rooms.OrderByDescending(r => r.Price);
                    break;
                case "available":
                    rooms = rooms.OrderByDescending(r => r.IsAvailable);
                    break;
                default:
                    rooms = rooms.OrderBy(r => r.RoomNumber);
                    break;
            }

            return PartialView("_RoomSearchResults", rooms.ToList());
        }

        // GET: Rooms/RoomReport/5
        public IActionResult RoomReport(int id)
        {
            // Include bookings and related guests
            var room = _context.Rooms
                .Include(r => r.Bookings)
                .ThenInclude(b => b.Guest)
                .FirstOrDefault(r => r.RoomID == id);

            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }
    }
}
