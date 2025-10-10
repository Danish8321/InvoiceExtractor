using OrderExtractorSample;
using System.Diagnostics;
using System.IO;
using System.Security;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace InvoiceExtractor;

class Program
{
    static void Main(string[] args)
    {
        string pdfPath = "C:\\Users\\MOGAMBO\\Downloads\\Tahaniya_Orders\\Meesho_2.pdf";
        using PdfDocument document = PdfDocument.Open(pdfPath);
        foreach (Page page in document.GetPages())
        {
            var invoiceData = MeeshoInvoiceExtractor.ExtractInvoiceData(page.Text);
            MeeshoInvoiceExtractor.DisplayInvoiceData(invoiceData);
        }

        //Console.WriteLine("\n\n" + new string('=', 65));
        //Console.WriteLine("JSON OUTPUT:");
        //Console.WriteLine(new string('=', 65));
        //Console.WriteLine(MeeshoInvoiceExtractor.ToJson(invoiceData));
    }
}