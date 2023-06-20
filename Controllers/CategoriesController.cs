using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ContactBook.Data;
using ContactBook.Models;
using ContactBook.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace ContactBook.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailSender _emailService;


        public CategoriesController(ApplicationDbContext context, UserManager<AppUser> userManager, IEmailSender emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        // GET: Categories
        [Authorize]
        public async Task<IActionResult> Index(string? swalMessage = null)
        {
            ViewData["SwalMessage"] = swalMessage;
            string appUserId = _userManager.GetUserId(User);

            //made name more meaningful than just appdbcontext
            //ensuring our appuser is filtered out 
            var categories = _context.Categories.Where(c => c.AppUserID == appUserId)
                                     .Include(c => c.AppUser)
                                     .ToList();
            return View(categories);
        }

        [Authorize]
        public async Task<IActionResult> EmailCategory(int id)
        {
            string appUserId = _userManager.GetUserId(User);

            //return category where looking for and no email bombing of wrong categories 
            Category? category = await _context.Categories
                                              .Include(c => c.Contacts)
                                              .FirstOrDefaultAsync(c => c.Id == id && c.AppUserID == appUserId);
            //only contacts in specific category 
            List<string> emails = category.Contacts.Select(c => c.Email).ToList();

            EmailData emailData = new EmailData()
            {
                GroupName = category.Name,
                //converts to 1 string from this array
                EmailAddress = string.Join(",", emails),
                //{} allow for the C# dot notation
                Subject = $"Group Message: {category.Name}"
            };

            EmailCategoryViewModel model = new EmailCategoryViewModel()
            {
                Contacts = category.Contacts.ToList(),
                EmailData = emailData
            };

            //return view w/ data collected from the model
            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EmailCategory(EmailCategoryViewModel ecvm)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    //gather our info
                    await _emailService.SendEmailAsync(ecvm.EmailData.EmailAddress, ecvm.EmailData.Subject, ecvm.EmailData.Body);
                    //View ==> Controller
                    return RedirectToAction("Index", "Categories", new { swalMessage = "Success: Email Sent" });
                }
                catch (Exception)
                {
                    return RedirectToAction("Index", "Categories", new { swalMessage = "Error: Email failed to send" });

                    throw;
                }
            }

            return View(ecvm);
        }


        // GET: Categories/Create
        [Authorize]
        public IActionResult Create()
        {

            return View();
        }

        // POST: Categories/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,AppUserID,Name")] Category category)
        {
            //remove from model then add back in b4 we save it 
            ModelState.Remove("AppUserId");

            if (ModelState.IsValid)
            {
                string appUserId = _userManager.GetUserId(User);
                category.AppUserID = appUserId;
                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        // GET: Categories/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            string appUserId = _userManager.GetUserId(User);

            //check id to see if it corresponds to the app user id in use
            var category = await _context.Categories.Where(c => c.Id == id && c.AppUserID == appUserId)
                                                    .FirstOrDefaultAsync();

            if (category == null)
            {
                return NotFound();
            }
            ViewData["AppUserID"] = new SelectList(_context.Users, "Id", "Id", category.AppUserID);
            return View(category);
        }

        // POST: Categories/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AppUserID,Name")] Category category)
        {
            if (id != category.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    //making use the id belongs to the current user
                    string appUserId = _userManager.GetUserId(User);
                    category.AppUserID = appUserId;
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            return View(category);
        }

        // GET: Categories/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Categories == null)
            {
                return NotFound();
            }

            string appUserId = _userManager.GetUserId(User);

            var category = await _context.Categories
                                         .FirstOrDefaultAsync(c => c.Id == id && c.AppUserID == appUserId);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            string appUserId = _userManager.GetUserId(User);

            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.AppUserID == appUserId);

            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }


            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return (_context.Categories?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
