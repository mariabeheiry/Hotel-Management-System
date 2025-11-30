using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hotel_Management_System.Data;
using Hotel_Management_System.Models;

namespace Hotel_Management_System.Controllers
{
    public class GuestsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GuestsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Guests
        public async Task<IActionResult> Index()
        {
            return View(await _context.Guests.OrderBy(g => g.Name).ToListAsync());
        }

        // AJAX Search Action
        public IActionResult Search(string term, string sortOrder)
        {
            var guests = _context.Guests.AsQueryable();

            // Filter by search term
            if (!string.IsNullOrEmpty(term))
            {
                guests = guests.Where(g => g.Name.Contains(term)
                                        || g.Phone.Contains(term)
                                        || g.Email.Contains(term));
            }

            // Apply sorting
            switch (sortOrder)
            {
                case "name_asc":
                    guests = guests.OrderBy(g => g.Name);
                    break;
                case "name_desc":
                    guests = guests.OrderByDescending(g => g.Name);
                    break;
                case "email":
                    guests = guests.OrderBy(g => g.Email);
                    break;
                default:
                    guests = guests.OrderBy(g => g.Name);
                    break;
            }

            return PartialView("_GuestSearchResults", guests.ToList());
        }

        // GET: Guests/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var guest = await _context.Guests.FirstOrDefaultAsync(m => m.GuestID == id);
            if (guest == null) return NotFound();

            return View(guest);
        }

        // GET: Guests/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Guests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GuestID,Name,Phone,Email")] Guest guest)
        {
            if (ModelState.IsValid)
            {
                _context.Add(guest);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(guest);
        }

        // GET: Guests/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var guest = await _context.Guests.FindAsync(id);
            if (guest == null) return NotFound();

            return View(guest);
        }

        // POST: Guests/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("GuestID,Name,Phone,Email")] Guest guest)
        {
            if (id != guest.GuestID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(guest);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GuestExists(guest.GuestID)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(guest);
        }

        // GET: Guests/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var guest = await _context.Guests.FirstOrDefaultAsync(m => m.GuestID == id);
            if (guest == null) return NotFound();

            return View(guest);
        }

        // POST: Guests/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var guest = await _context.Guests.FindAsync(id);
            if (guest != null)
            {
                _context.Guests.Remove(guest);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool GuestExists(int id)
        {
            return _context.Guests.Any(e => e.GuestID == id);
        }
    }
}
