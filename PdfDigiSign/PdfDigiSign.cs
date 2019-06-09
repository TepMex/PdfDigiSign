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

        /// <summary>
        /// Adding named (empty) signature field to PDF document hard way (touch file descriptor, create and close stamper)
        /// </summary>
        /// <param name="fileName">PDF File</param>
        /// <param name="fieldName">Name of field</param>
        /// <param name="x">X coordinate on the page</param>
        /// <param name="y">Y coordinate on the page</param>
        /// <param name="width">Field width</param>
        /// <param name="height">Field height</param>
        /// <param name="page">The page to place field</param>
        /// <param name="flags">PdfAnnotation flags</param>
        /// <returns>Field added successfully or not</returns>
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

        /// <summary>
        /// Adding named (empty) signature field to PDF document soft way (using existing stamper)
        /// </summary>
        /// <param name="stamper">Existing stamper</param>
        /// <param name="fieldName">Name of field</param>
        /// <param name="x">X coordinate on the page</param>
        /// <param name="y">Y coordinate on the page</param>
        /// <param name="width">Field width</param>
        /// <param name="height">Field height</param>
        /// <param name="page">The page to place field</param>
        /// <param name="flags">PdfAnnotation flags</param>
        /// <returns>Field added successfully or not</returns>
        public static bool AddSignatureField(ref PdfStamper stamper, 
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
                
                PdfFormField field = CreateField(stamper.Writer, fieldName, new iTextSharp.text.Rectangle(x, y, width, height), page, flags);
                stamper.AddAnnotation(field, page);

                result = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Sign (fill) named field in the document hard way (touch file descriptor, create and close stamper)
        /// </summary>
        /// <param name="fileName">PDF file</param>
        /// <param name="fieldName">Field to be signed</param>
        /// <param name="reason">Sign reason</param>
        /// <param name="location">Sign location</param>
        /// <param name="graphics">Sign graphic</param>
        /// <param name="certFile">PFX certificate</param>
        /// <param name="certPassword">password of certificate</param>
        /// <param name="renderingMode">SignatureRender renderingMode</param>
        /// <param name="certificationLevel">PdfSignatureAppearance Certification Level</param>
        /// <returns>Successfull or not</returns>
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


        /// <summary>
        /// Sign (fill) named field in the document soft way (using existing stamper)
        /// </summary>
        /// <param name="stamper">PdfStamper</param>
        /// <param name="fieldName">Field to be signed</param>
        /// <param name="reason">Sign reason</param>
        /// <param name="location">Sign location</param>
        /// <param name="graphics">Sign graphic</param>
        /// <param name="certFile">PFX certificate</param>
        /// <param name="certPassword">password of certificate</param>
        /// <param name="renderingMode">SignatureRender renderingMode</param>
        /// <param name="certificationLevel">PdfSignatureAppearance Certification Level</param>
        /// <returns>Successfull or not</returns>
        public static bool SignField(ref PdfStamper stamper,
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
                
                if (stamper.Reader.AcroFields.Fields.ContainsKey(fieldName))
                {
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

                    result = true;

                }
                else
                {
                    result = false;
                }

            }
            catch (Exception e)
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
