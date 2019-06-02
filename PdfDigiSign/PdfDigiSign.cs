using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.crypto;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfDigiSign
{
    public class PdfDigiSign
    {

        public static bool AddSignatureField(string fileName, 
            string fieldName, 
            float x, 
            float y, 
            float width, 
            float height, 
            int page = 1, 
            int flags = PdfAnnotation.FLAGS_PRINT)
        {
            bool result = false;
            try
            {
                string tempFileName = Path.GetTempFileName();
                FileStream outFs = new FileStream(tempFileName, FileMode.Create);
                PdfReader reader = new PdfReader(fileName);
                PdfStamper stamper = new PdfStamper(reader, outFs);
                


                PdfFormField field = CreateField(stamper.Writer, fieldName, new iTextSharp.text.Rectangle(x, y, width, height), page, flags);
                stamper.AddAnnotation(field, page);
                stamper.Close();
                outFs.Close();
                reader.Close();

                File.Copy(tempFileName, fileName, true);

                result = true;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                result = false;
            }

            return result;
        }

        public static bool SignField(string fileName, 
            string fieldName, 
            string reason, 
            string location, 
            Bitmap graphics, 
            string certFile, 
            string certPassword, 
            PdfSignatureAppearance.SignatureRender renderingMode = PdfSignatureAppearance.SignatureRender.GraphicAndDescription, 
            int certificationLevel = PdfSignatureAppearance.CERTIFIED_NO_CHANGES_ALLOWED)
        {
            bool result = false;

            try
            {
                PdfReader reader = new PdfReader(fileName);

                if(reader.AcroFields.Fields.ContainsKey(fieldName))
                {
                    string tempFile = Path.GetTempFileName();
                    FileStream fs = new FileStream(tempFile, FileMode.Create);
                    PdfStamper stamper = PdfStamper.CreateSignature(reader, fs, '\0');

                    PdfSignatureAppearance psa = GetPSA(fieldName, stamper, graphics, reason, location, renderingMode, certificationLevel);

                    Pkcs12Store store = new Pkcs12Store(new FileStream(certFile, FileMode.Open), certPassword.ToCharArray());
                    string alias = "";
                    ICollection<X509Certificate> chain = new List<X509Certificate>();

                    foreach (string al in store.Aliases)
                    {
                        if (store.IsKeyEntry(al) && store.GetKey(al).Key.IsPrivate)
                        {
                            alias = al;
                            break;
                        }
                    }
                    AsymmetricKeyEntry ake = store.GetKey(alias);

                    foreach (X509CertificateEntry c in store.GetCertificateChain(alias))
                    {
                        chain.Add(c.Certificate);
                    }

                    RsaPrivateCrtKeyParameters parameters = ake.Key as RsaPrivateCrtKeyParameters;

                    psa.SetCrypto(parameters, chain.ToArray(), null, PdfSignatureAppearance.WINCER_SIGNED);

                    stamper.Close();
                    reader.Close();

                    File.Copy(tempFile, fileName, true);

                    result = true;

                }
                else
                {
                    result = false;
                }

            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                result = false;
            }

            return result;
        }

        private static PdfFormField CreateField(PdfWriter writer, string fieldName, iTextSharp.text.Rectangle placeRect, int page, int flags)
        {
            PdfFormField field = PdfFormField.CreateSignature(writer);
            field.Flags = flags;
            field.SetWidget(placeRect, null);
            field.FieldName = fieldName;
            field.Page = page;
            field.MKBorderColor = iTextSharp.text.Color.BLACK;
            field.MKBackgroundColor = iTextSharp.text.Color.WHITE;

            return field;
        }

        private static PdfSignatureAppearance GetPSA(string fieldName, 
            PdfStamper stamper, 
            Bitmap graphics, 
            string reason, 
            string location, 
            PdfSignatureAppearance.SignatureRender renderingMode, 
            int certLevel)
        {
            PdfSignatureAppearance psa = stamper.SignatureAppearance;
            psa.Acro6Layers = true;
            psa.SignatureGraphic = iTextSharp.text.Image.GetInstance(graphics, ImageFormat.Png);
            psa.Render = renderingMode;
            psa.CertificationLevel = certLevel;
            psa.Reason = reason;
            psa.Location = location;
            psa.SetVisibleSignature(fieldName);

            return psa;
        }


    }
}
