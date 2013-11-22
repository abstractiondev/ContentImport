using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using java.io;
using org.apache.pdfbox.pdmodel;
using org.apache.pdfbox.util;

namespace TheBall.Import
{
    public class PDFImport
    {
        public static string GetPDFAsText(byte[] pdfData)
        {
            InputStream inputStream = new ByteArrayInputStream(pdfData);
            PDDocument doc = PDDocument.load(inputStream);
            PDFTextStripper stripper = new PDFTextStripper();
            return stripper.getText(doc);
        }
    }
}
