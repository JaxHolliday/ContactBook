using Microsoft.Build.Framework;
using System.ComponentModel.DataAnnotations;
using RequiredAttribute = System.ComponentModel.DataAnnotations.RequiredAttribute;

namespace ContactBook.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required] 
        public string? AppUserID { get; set; }

        [Required]
        [Display(Name = "Category Name")]
        public string? Name { get; set;}

        //Virtuals
        //creates a forign key in the app user model 
        //Used to link 2 different tables 
        public virtual AppUser? AppUser { get; set; }
        //if refered in another must have one here 
        //icollection join tables
        public virtual ICollection<Contact> Contacts { get; set; } = new HashSet<Contact>();
    }
}
