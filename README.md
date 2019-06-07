# PdfDigiSign
Library for working with pdf digital signatures based on iTextSharp 4

## Use cases:

Open file, add signatures field, close file
`PdfDigiSign.PdfDigiSign.AddSignatureField("pdf.pdf", "myfield", 0.5f, 0.5f, 71.5f, 47.5f);`

Open file, add signatures field to custom page, do something else with file
`...
int page = ...
PdfStamper stamper = ...
PdfDigiSign.PdfDigiSign.AddSignatureField(stamper, "myfield", 0.5f, 0.5f, 71.5f, 47.5f, page);
`