using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITextSharp5DigitalSign
{
    class Program
    {
        static void Main(string[] args)
        {
            PdfDigiSign.PdfDigiSign.AddSignatureField("pdf.pdf", "myfield", 0.5f, 0.5f, 71.5f, 47.5f);
            PdfDigiSign.PdfDigiSign.SignField("pdf.pdf", "myfield", "fsdaf", "RU", Resource1.Image1, "cert.pfx", "password1234");
        }
    }
}
