# PdfDigiSign
Library for working with pdf digital signatures based on iTextSharp 4

## Use cases:

Open file, add signatures field, close file:
```
PdfDigiSign.PdfDigiSign.AddSignatureField("pdf.pdf", "myfield", 0.5f, 0.5f, 71.5f, 47.5f);
```

Open file, add signatures field to custom page, do something else with file:
```
...
int page = ...
PdfStamper stamper = ...
PdfDigiSign.PdfDigiSign.AddSignatureField(stamper, "myfield", 0.5f, 0.5f, 71.5f, 47.5f, page);
```

Open file, sign field 'myfield' with reason 'Reason', location 'RU' and graphics 'Image1'. Use certificate 'cert.pfx' with password 'password1234'. Then close file.
```
PdfDigiSign.PdfDigiSign.SignField("pdf.pdf", "myfield", "Reason", "RU", Resource1.Image1, "cert.pfx", "password1234");
```

Sign field 'myfield' with reason 'Reason', location 'RU' and graphics 'Image1' using exiting stamper. Use certificate 'cert.pfx' with password 'password1234'. Then do something else.
```
PdfStamper stamper = ...
PdfDigiSign.PdfDigiSign.SignField(stamper, "myfield", "Reason", "RU", Resource1.Image1, "cert.pfx", "password1234");
```

Use generateCert.ps1 to generate simple self-signed pfx certificate with password.