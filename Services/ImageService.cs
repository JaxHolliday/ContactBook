using ContactBook.Services.Interfaces;

namespace ContactBook.Services
{
    public class ImageService : IImageService        
    {
        //extnesions list 
        private readonly string[] suffixes = { "Bytes", "KB", "MB", "GB", "TB", "PB" };
        //in case we dont find anything
        private readonly string deafaultImage = "img/DefaultContactImage.png";

        public string ConvertByteArrayToFile(byte[] filedata, string extension)
        {
            throw new NotImplementedException();
        }

        public Task<byte[]> ConvertFileToByteArrayAsync(IFormFile file)
        {
            throw new NotImplementedException();
        }
    }
}
