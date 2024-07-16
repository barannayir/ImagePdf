namespace ImagePdf.Models
{
    public class ImageUpload
    {
        public IFormFile Image { get; set; }
        public int Scale { get; set; }
    }
}
