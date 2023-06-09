﻿using ContactBook.Data;
using ContactBook.Models;
using ContactPro.Services.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace ContactPro.Services
{
    public class AddressBookService : IAddressBookService
    {
        private readonly ApplicationDbContext _context;

        public AddressBookService(ApplicationDbContext context)
        {
            _context = context;
        }

        // saves contact and category to db
        public async Task AddContactToCategoryAsync(int categoryId, int contactId)
        {
            try
            {
                // check to see if category is in contact already 
                if (!await IsContactInCategory(categoryId, contactId))
                {
                    Contact? contact = await _context.Contacts.FindAsync(contactId);
                    Category? category = await _context.Categories.FindAsync(categoryId);

                    if (category != null && contact != null)
                    {
                        category.Contacts.Add(contact);
                        await _context.SaveChangesAsync();
                    }

                }

            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<ICollection<Category>> GetContactCategoriesAsync(int contactId)
        {
            try
            {
                //return categories based on the id 
                Contact? contact = await _context.Contacts.Include(c => c.Categories).FirstOrDefaultAsync(c => c.Id == contactId);
                return contact!.Categories;
            }
            catch (Exception)
            {

                throw;
            }

        }

        public async Task<ICollection<int>> GetContactCategoryIdsAsync(int contactId)
        {
            try
            {
                //grabbing contact and include cats that the id is in then filter
                //including contacts for category ==> return first element that satifies [first found match]
                var contact = await _context.Contacts.Include(c => c.Categories)
                                                     .FirstOrDefaultAsync(c => c.Id == contactId);

                //create list of integers then return
                //select statement to just pull back id then turn into list
                List<int> categoryIds = contact!.Categories.Select(c => c.Id).ToList();
                return categoryIds;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<IEnumerable<Category>> GetUserCategoriesAsync(string userId)
        {
            //new blank category list
            List<Category> categories = new List<Category>();

            try
            {
                //do to db => filter with where clause => this user = this id
                categories = await _context.Categories.Where(c => c.AppUserID== userId)
                                                      .OrderBy(c => c.Name)
                                                      .ToListAsync();
            }
            catch (Exception)
            {
                throw;
            }

            return categories;
        }

        public async Task<bool> IsContactInCategory(int categoryId, int contactId)
        {
            //go out and look in db for the contact
            Contact? contact = await _context.Contacts.FindAsync(contactId);

            return await _context.Categories
                                 .Include(c => c.Contacts)
                                 .Where(c => c.Id == categoryId && c.Contacts.Contains(contact))
                                 .AnyAsync();

        }

        public async Task RemoveContactFromCategoryAsync(int categoryId, int contactId)
        {
            //categoryid == cate in contact
            try
            {
                if (await IsContactInCategory(categoryId, contactId))
                {
                    //finding id's for both
                    Contact? contact = await _context.Contacts.FindAsync(contactId);
                    Category? category = await _context.Categories.FindAsync(categoryId);

                    if (category != null && contact != null)
                    {
                        //if both not = to null then remove 
                        category.Contacts.Remove(contact);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public IEnumerable<Contact> SearchForContacts(string searchString, string userId)
        {
            throw new NotImplementedException();
        }
    }
}