namespace ContactBook.Models.ViewModels
{
    public class EmailCategoryViewModel
    {
        //holds all members of the category
        public List<Contact>? Contacts { get; set; }
        public EmailData? EmailData { get; set; }
    }
}
