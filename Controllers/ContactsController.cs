﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using ContactBook.Data;
using ContactBook.Models;
using ContactBook.Enums;
using ContactBook.Services.Interfaces;
using ContactPro.Services.Interfaces;
using ContactBook.Models.ViewModels;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace ContactBook.Controllers
{
    public class ContactsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IImageService _imageService;
        private readonly IAddressBookService _addressBookService;
        private readonly IEmailSender _emailService;

        //injection => injecting objects so that we can benefit from them (methods and etc)
        //injector ==> constructor 
        public ContactsController(ApplicationDbContext context, UserManager<AppUser> userManager, IImageService imageService, IAddressBookService addressBookService, IEmailSender emailService)
        {
            _context = context;
            _userManager = userManager;
            _imageService = imageService;
            _addressBookService = addressBookService;
            _emailService = emailService;
        }

        // GET: Contacts
        [Authorize]
        public async Task<IActionResult> Index(int categoryId, string? swalMessage = null)
        {
            ViewData["SwalMessage"] = swalMessage;

            var contacts = new List<Contact>();
            string appUserId = _userManager.GetUserId(User);

            //return the UserId and its associated contacts and categories
            AppUser? appUser = _context.Users
                                     .Include(c => c.Contacts)
                                     .ThenInclude(c => c.Categories)
                                     .FirstOrDefault(u => u.Id == appUserId);

            var categories = appUser!.Categories;

            //
            if (categoryId == 0)
            {
                contacts = appUser.Contacts.OrderBy(c => c.LastName)
                                           .ThenBy(c => c.FirstName)
                                           .ToList();
            }else
            {
                //return contacts in this filter only
                contacts = appUser.Categories.FirstOrDefault(c => c.Id == categoryId)
                                   .Contacts.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToList();
            }

            ViewData["CategoryId"] = new SelectList(categories, "Id", "Name");

            return View(contacts);
        }

        [Authorize]
        public IActionResult SearchContacts(string searchString)
        {
            //list of contacts based on userid then by search
            string appUserId = _userManager.GetUserId(User);
            var contacts = new List<Contact>();

            AppUser appUser = _context.Users
                                      .Include(c => c.Contacts)
                                      .ThenInclude(c => c.Categories)
                                      .FirstOrDefault(u => u.Id == appUserId);

            //test if they have put in searchstring or not 
            if (String.IsNullOrEmpty(searchString))
            {
                contacts = appUser.Contacts
                                  .OrderBy(c => c.LastName)
                                  .ThenBy(c => c.FirstName)
                                  .ToList();
            }
            else
            {
                //filter by fullname
                contacts = appUser.Contacts.Where(c => c.FullName!.ToLower().Contains(searchString.ToLower()))
                                  .OrderBy(c => c.LastName)
                                  .ThenBy(c => c.FirstName)
                                  .ToList();
            }

            ViewData["CategoryId"] = new SelectList(appUser.Categories, "Id", "Name", 0);
            //returns to the index page w/ contacts model
            return View(nameof(Index), contacts);

        }

        [Authorize]
        public async Task<IActionResult> EmailContact(int Id)
        {

            string appUserId = _userManager.GetUserId(User);
            Contact? contact = await _context.Contacts.Where(c => c.Id == Id && c.AppUserID == appUserId)
                                                     .FirstOrDefaultAsync();
            if (contact == null)
            {
                return NotFound();
            }

            //creates new obj for us
            EmailData emailData = new EmailData()
            {
                EmailAddress = contact.Email,
                FirstName = contact.FirstName,
                LastName = contact.LastName
            };

            //viewmodels come from multiple data sets
            EmailContactViewModel model = new EmailContactViewModel()
            {
                Contact = contact,
                EmailData = emailData
            };

            return View(model);
        }
        
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> EmailContact(EmailContactViewModel ecvm)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _emailService.SendEmailAsync(ecvm.EmailData.EmailAddress, ecvm.EmailData.Subject, ecvm.EmailData.Body);
                    //return to index view of contacts ==> 3rd is swal alert msg
                    return RedirectToAction("Index", "Contacts", new { swalMessage = "Success: Email Sent"});
                }
                catch (Exception)
                {
                    //if error has been throw then this can be thrown 
                    return RedirectToAction("Index", "Contacts", new { swalMessage = "Error: Email Failed To Send" });

                    throw;
                }

            }

            return View(ecvm);
        }

        // GET: Contacts/Details/5
        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .Include(c => c.AppUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // GET: Contacts/Create
        [Authorize]
        public async Task<IActionResult> Create()
        {
            string appUserId = _userManager.GetUserId(User);

            //Viewdata = unstructed data that can be accessed in the view later 
            //Converts comma'd list into regualr list
            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>().ToList());
            ViewData["CategoryList"] = new MultiSelectList(await _addressBookService.GetUserCategoriesAsync(appUserId), "Id", "Name");

            return View();
        }

        // POST: Contacts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FirstName,LastName,BirthDate,Address,Address2,City,State,ZipCode,Email,PhoneNumber,ImageFile")] Contact contact, List<int> CategoryList)
        {
            //This is to make sure it isnt validated
            ModelState.Remove("AppUserId");

            if (ModelState.IsValid)
            {
                //getting current user
                contact.AppUserID = _userManager.GetUserId(User);
                contact.Created = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);

                if (contact.Birthdate != null)
                {
                    contact.Birthdate = DateTime.SpecifyKind(contact.Birthdate.Value, DateTimeKind.Utc);
                }

                //IMAGES
                //File being sent to us [ImageFile]
                if (contact.ImageFile != null)
                {
                    //Register => 
                    contact.ImageData = await _imageService.ConvertFileToByteArrayAsync(contact.ImageFile);
                    contact.ImageType = contact.ImageFile.ContentType;
                }

                _context.Add(contact);
                await _context.SaveChangesAsync();

                //loop over all selected categories
                foreach (int categoryId in CategoryList)
                {
                    await _addressBookService.AddContactToCategoryAsync(categoryId, contact.Id);
                }
                //save each category to the contact categories table.


                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Index));
        }


        // GET: Contacts/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }

            string appUserId = _userManager.GetUserId(User);

            //var contact = await _context.Contacts.FindAsync(id);
            //look up contact
            var contact = await _context.Contacts.Where(c => c.Id == id && c.AppUserID == appUserId)
                                                 .FirstOrDefaultAsync();

            if (contact == null)
            {
                return NotFound();
            }

            //generates select lists for states and categories in DB
            ViewData["StatesList"] = new SelectList(Enum.GetValues(typeof(States)).Cast<States>().ToList());
            ViewData["CategoryList"] = new MultiSelectList(await _addressBookService.GetUserCategoriesAsync(appUserId), "Id", "Name", await _addressBookService.GetContactCategoryIdsAsync(contact.Id));


            return View(contact);
        }

        // POST: Contacts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AppUserID,FirstName,LastName,Birthdate,Address1,Address2,City,State,ZipCode,Email,PhoneNumber,Created,ImageFile,ImageData,ImageType")] Contact contact, List<int> CategoryList)
        {
            if (id != contact.Id)
            {
                return NotFound();
            }

            //if all model info is correct ==> save it 
            if (ModelState.IsValid)
            {
                try
                {
                    //manually putting created via system them converting to right format
                    contact.Created = DateTime.SpecifyKind(contact.Created, DateTimeKind.Utc);
                    
                    if (contact.Birthdate != null)
                    {
                        contact.Birthdate = DateTime.SpecifyKind(contact.Birthdate.Value, DateTimeKind.Utc);
                    }
                    if (contact.ImageFile != null)
                    {
                        contact.ImageData = await _imageService.ConvertFileToByteArrayAsync(contact.ImageFile);
                        contact.ImageType = contact.ImageFile.ContentType;
                    }

                    _context.Update(contact);
                    await _context.SaveChangesAsync();

                    //save categories
                    //remove cureent categories 
                    List<Category> oldCategories = (await _addressBookService.GetContactCategoriesAsync(contact.Id)).ToList();
                    
                    foreach (var category in oldCategories)
                    {
                        await _addressBookService.RemoveContactFromCategoryAsync(category.Id, contact.Id);
                    }

                    //add selected categories 
                    foreach (int categoryId in CategoryList)
                    {
                        await _addressBookService.AddContactToCategoryAsync(categoryId, contact.Id);
                    }



                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContactExists(contact.Id))
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
            ViewData["AppUserID"] = new SelectList(_context.Users, "Id", "Name", contact.AppUserID);
            return View(contact);
        }

        // GET: Contacts/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Contacts == null)
            {
                return NotFound();
            }
            string appUserId = _userManager.GetUserId(User);

            var contact = await _context.Contacts
                                        .FirstOrDefaultAsync(c => c.Id == id && c.AppUserID == appUserId);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // POST: Contacts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
           
            //making sure that we get the right user
           string appUserId = _userManager.GetUserId(User);

            var contact = await _context.Contacts.FirstOrDefaultAsync(c => c.Id == id && c.AppUserID == appUserId);
            if (contact != null)
            {
                _context.Contacts.Remove(contact);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool ContactExists(int id)
        {
            return (_context.Contacts?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
