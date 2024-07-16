using ImageMagick;
using ImagePdf.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ImagePdf.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(ImageUpload model)
        {
            if (model.Image != null && model.Image.Length > 0)
            {
                // Dosyayı bellek içi bir byte dizisine kopyala
                using (var memoryStream = new MemoryStream())
                {
                    await model.Image.CopyToAsync(memoryStream);
                    var imageBytes = memoryStream.ToArray();

                    // Görseli işleme ve PDF oluşturma
                    var pdfBytes = ProcessImageAndCreatePdf(imageBytes, model.Scale);

                    return File(pdfBytes, "application/pdf", "output.pdf");
                }
            }

            return RedirectToAction("Index");
        }

        private byte[] ProcessImageAndCreatePdf(byte[] imageBytes, int scale)
        {
            using (var image = new MagickImage(imageBytes))
            {
                // Görseli büyüt
                image.Resize(new MagickGeometry($"{scale}%"));

                // A4 boyutları (72 DPI)
                const int A4_WIDTH = 595;  // 8.27 inch * 72 DPI
                const int A4_HEIGHT = 842; // 11.69 inch * 72 DPI

                var images = new List<MagickImage>();
                for (int y = 0; y < image.Height; y += A4_HEIGHT)
                {
                    for (int x = 0; x < image.Width; x += A4_WIDTH)
                    {
                        var rectangle = new MagickGeometry(x, y, A4_WIDTH, A4_HEIGHT);
                        var croppedImage = new MagickImage(image);
                        croppedImage.Crop(rectangle);
                        images.Add(croppedImage);
                    }
                }

                using (var ms = new MemoryStream())
                {
                    using (var document = new Document(PageSize.A4))
                    {
                        var writer = PdfWriter.GetInstance(document, ms);
                        document.Open();

                        foreach (var img in images)
                        {
                            using (var imgStream = new MemoryStream())
                            {
                                img.Write(imgStream, MagickFormat.Jpeg);
                                var pdfImage = iTextSharp.text.Image.GetInstance(imgStream.ToArray());
                                pdfImage.ScaleToFit(PageSize.A4.Width, PageSize.A4.Height);
                                document.NewPage();
                                document.Add(pdfImage);
                            }
                        }

                        document.Close();
                        writer.Close();
                    }

                    return ms.ToArray();
                }
            }
        }

    }

}
