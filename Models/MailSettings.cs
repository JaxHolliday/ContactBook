namespace ContactBook.Models
{
    public class MailSettings
    {
        //mimicing the settings in out "MailSettings" in our user secrets
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? DisplayName { get; set; }
        public string? Host { get; set;}
        public int Port { get; set; }
        
    }
}
