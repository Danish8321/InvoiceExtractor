using System.Text.RegularExpressions;

namespace OrderExtractorSample;

public class MeeshoInvoiceExtractor
{
    public static InvoiceData ExtractInvoiceData(string invoiceText)
    {
        var invoiceData = new InvoiceData();

        try
        {
            //Console.WriteLine("Starting extraction...\n");

            // Extract from Product Details table
            ExtractProductDetailsTable(invoiceText, invoiceData);

            // Extract Ship To
            ExtractShipTo(invoiceText, invoiceData);

            // Extract Seller Information
            ExtractSellerInfo(invoiceText, invoiceData);

            // Extract Order Information
            ExtractOrderInfo(invoiceText, invoiceData);

            // Extract Product Line Items from Description table
            ExtractProductLineItems(invoiceText, invoiceData);

            //Console.WriteLine("Extraction completed!\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting invoice data: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        return invoiceData;
    }

    private static void ExtractProductDetailsTable(string text, InvoiceData data)
    {
        // Pattern for: Product DetailsSKUSizeQtyColorOrder No.mfymzabrFree Size1Gold204827517525195264_1
        // SKU is exactly 8 characters

        // SKU - exactly 8 alphanumeric characters after "Order No."
        var skuMatch = Regex.Match(text, @"Product DetailsSKUSizeQtyColorOrder No\.(\w{8})");
        if (skuMatch.Success)
        {
            data.SKU = skuMatch.Groups[1].Value;
            //Console.WriteLine($"✓ SKU: {data.SKU}");
        }

        // Size - pattern like "Free Size" or similar followed by digit
        var sizeMatch = Regex.Match(text, @"Order No\.\w{8}((?:\w+\s)*Size)(\d+)");
        if (sizeMatch.Success)
        {
            data.Size = sizeMatch.Groups[1].Value.Trim();
            data.Qty = int.Parse(sizeMatch.Groups[2].Value);
            //Console.WriteLine($"✓ Size: {data.Size}");
            //Console.WriteLine($"✓ Qty: {data.Qty}");
        }

        // Color - word between Qty and Order number
        var colorMatch = Regex.Match(text, @"(?:Free Size|Size)(\d+)(\w+)(\d{15,}_\d+)");
        if (colorMatch.Success)
        {
            if (!int.TryParse(colorMatch.Groups[2].Value, out _)) // Make sure it's not a number
            {
                data.Color = colorMatch.Groups[2].Value;
                ///Console.WriteLine($"✓ Color: {data.Color}");
            }
            data.OrderNo = colorMatch.Groups[3].Value;
            //Console.WriteLine($"✓ Order No: {data.OrderNo}");
        }

        // Fallback for Order No if not caught above
        if (string.IsNullOrEmpty(data.OrderNo))
        {
            var orderMatch = Regex.Match(text, @"Order No\.[\w\s]+?(\d{15,}_\d+)");
            if (orderMatch.Success)
            {
                data.OrderNo = orderMatch.Groups[1].Value;
                //Console.WriteLine($"✓ Order No: {data.OrderNo}");
            }
        }
    }

    private static void ExtractShipTo(string text, InvoiceData data)
    {
        // Pattern: BILL TO / SHIP TO ... Sold by
        var pattern = @"BILL TO / SHIP TO\s*(.+?)(?=Sold by)";
        var match = Regex.Match(text, pattern, RegexOptions.Singleline);

        if (match.Success)
        {
            data.ShipTo = match.Groups[1].Value.Trim();
            //Console.WriteLine($"✓ Ship To: {data.ShipTo.Substring(0, Math.Min(50, data.ShipTo.Length))}...");
        }
    }

    private static void ExtractSellerInfo(string text, InvoiceData data)
    {
        // Seller Name: Sold by : FAIZAN AHMAD (text until next field or address)
        var sellerMatch = Regex.Match(text, @"Sold by\s*:\s*([A-Z\s]+?)(?=\s+[A-Z][a-z]|\s+GSTIN)");
        if (sellerMatch.Success)
        {
            data.SellerName = sellerMatch.Groups[1].Value.Trim();
            //Console.WriteLine($"✓ Seller Name: {data.SellerName}");
        }

        // Seller GSTIN: GSTIN - 10DQLPA9951C1Z5
        var gstinMatch = Regex.Match(text, @"GSTIN\s*-\s*([A-Z0-9]+)");
        if (gstinMatch.Success)
        {
            data.SellerGSTIN = gstinMatch.Groups[1].Value.Trim();
            //Console.WriteLine($"✓ Seller GSTIN: {data.SellerGSTIN}");
        }
    }

    private static void ExtractOrderInfo(string text, InvoiceData data)
    {
        // Purchase Order No.204827517525195264
        var poMatch = Regex.Match(text, @"Purchase Order No\.(\d+)");
        if (poMatch.Success)
        {
            data.PurchaseOrderNo = poMatch.Groups[1].Value.Trim();
            //Console.WriteLine($"✓ Purchase Order No: {data.PurchaseOrderNo}");
        }

        // Invoice No.d2rnq26255 - exactly 10 characters (alphanumeric)
        var invoiceMatch = Regex.Match(text, @"Invoice No\.([a-z0-9]{10})", RegexOptions.IgnoreCase);
        if (invoiceMatch.Success)
        {
            data.InvoiceNo = invoiceMatch.Groups[1].Value.Trim();
            //Console.WriteLine($"✓ Invoice No: {data.InvoiceNo}");
        }

        // Order Date01.10.2025
        var orderDateMatch = Regex.Match(text, @"Order Date([\d.]+)");
        if (orderDateMatch.Success)
        {
            data.OrderDate = orderDateMatch.Groups[1].Value.Trim();
            //Console.WriteLine($"✓ Order Date: {data.OrderDate}");
        }

        // Invoice Date01.10.2025
        var invoiceDateMatch = Regex.Match(text, @"Invoice Date([\d.]+)");
        if (invoiceDateMatch.Success)
        {
            data.InvoiceDate = invoiceDateMatch.Groups[1].Value.Trim();
            //Console.WriteLine($"✓ Invoice Date: {data.InvoiceDate}");
        }
    }

    private static void ExtractProductLineItems(string text, InvoiceData data)
    {
        //Console.WriteLine("\nExtracting product line items...");

        var tableSectionMatch = Regex.Match(text, @"DescriptionHSNQtyGross AmountDiscountTaxable ValueTaxesTotal(.+?)Total", RegexOptions.Singleline);

        if (!tableSectionMatch.Success)
        {
            Console.WriteLine("Could not find product table section");
            return;
        }

        string tableContent = tableSectionMatch.Groups[1].Value;

        // Pattern for each product line:
        // Product Name HSN Qty Rs.Amount Rs.Amount Rs.Amount TAX @%  Rs.Amount Rs.Amount
        // Example: Kanaka Mayuri Jhumka - Free Size 7117901Rs.570.00Rs.0.00Rs.553.40IGST @3.0% Rs.16.60Rs.570.00

        var productPattern = @"([^0-9]+?)(\d{6})(\d+|NA)Rs\.([\d.]+)Rs\.([\d.]+)Rs\.([\d.]+)((?:(?:IGST|CGST|SGST)\s*@[\d.]+%\s*:?\s*Rs\.[\d.]+\s*)+)Rs\.([\d.]+)";
        var matches = Regex.Matches(tableContent, productPattern);

        Console.WriteLine($"Found {matches.Count} product line items");

        foreach (Match match in matches)
        {
            if (match.Success)
            {
                var product = new ProductDetail
                {
                    Description = match.Groups[1].Value.Trim(),
                    HSN = match.Groups[2].Value,
                    Qty = match.Groups[3].Value,
                    GrossAmount = match.Groups[4].Value,
                    Discount = match.Groups[5].Value,
                    TaxableValue = match.Groups[6].Value,
                    Taxes = match.Groups[7].Value,
                    Total = match.Groups[8].Value
                };

                data.Products.Add(product);
                //Console.WriteLine($"  ✓ Product: {product.Product}");
            }
        }
    }

    public static void DisplayInvoiceData(InvoiceData data)
    {
        Console.WriteLine();
        Console.WriteLine("╔════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                          INVOICE HEADER                                ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════════════╝");
        Console.WriteLine($"{"SKU:",-25} {data.SKU}");
        Console.WriteLine($"{"Size:",-25} {data.Size}");
        Console.WriteLine($"{"Qty:",-25} {data.Qty}");
        Console.WriteLine($"{"Color:",-25} {data.Color}");
        Console.WriteLine($"{"Order No:",-25} {data.OrderNo}");
        Console.WriteLine($"{"Ship To:",-25} {(data.ShipTo?.Length > 50 ? string.Concat(data.ShipTo.AsSpan(0, 47), "...") : data.ShipTo)}");
        Console.WriteLine($"{"Seller Name:",-25} {data.SellerName}");
        Console.WriteLine($"{"Seller GSTIN:",-25} {data.SellerGSTIN}");
        Console.WriteLine($"{"Purchase Order No:",-25} {data.PurchaseOrderNo}");
        Console.WriteLine($"{"Invoice No:",-25} {data.InvoiceNo}");
        Console.WriteLine($"{"Order Date:",-25} {data.OrderDate}");
        Console.WriteLine($"{"Invoice Date:",-25} {data.InvoiceDate}");

        Console.WriteLine();
        Console.WriteLine("╔════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                         PRODUCT DETAILS                                ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════════════╝");

        int productNum = 1;
        foreach (var product in data.Products)
        {
            Console.WriteLine($"\n[Product #{productNum}]");
            Console.WriteLine($"{"Product:",-25} {product.Description}");
            Console.WriteLine($"{"HSN:",-25} {product.HSN}");
            Console.WriteLine($"{"Qty:",-25} {product.Qty}");
            Console.WriteLine($"{"Gross Amount:",-25} Rs.{product.GrossAmount:N2}");
            Console.WriteLine($"{"Discount:",-25} Rs.{product.Discount:N2}");
            Console.WriteLine($"{"Taxable Value:",-25} Rs.{product.TaxableValue:N2}");
            Console.WriteLine($"{"Taxes:",-25} {product.Taxes}");
            Console.WriteLine($"{"Total:",-25} Rs.{product.Total:N2}");
            Console.WriteLine("─────────────────────────────────────────────────────────────────────────");
            productNum++;
        }
    }

    //public static string ToJson(InvoiceData data)
    //{
    //    var json = "{\n";
    //    json += $"  \"SKU\": \"{data.SKU}\",\n";
    //    json += $"  \"Size\": \"{data.Size}\",\n";
    //    json += $"  \"Qty\": {data.Qty},\n";
    //    json += $"  \"Color\": \"{data.Color}\",\n";
    //    json += $"  \"OrderNo\": \"{data.OrderNo}\",\n";
    //    json += $"  \"ShipTo\": \"{EscapeJson(data.ShipTo)}\",\n";
    //    json += $"  \"SellerName\": \"{data.SellerName}\",\n";
    //    json += $"  \"SellerGSTIN\": \"{data.SellerGSTIN}\",\n";
    //    json += $"  \"PurchaseOrderNo\": \"{data.PurchaseOrderNo}\",\n";
    //    json += $"  \"InvoiceNo\": \"{data.InvoiceNo}\",\n";
    //    json += $"  \"OrderDate\": \"{data.OrderDate}\",\n";
    //    json += $"  \"InvoiceDate\": \"{data.InvoiceDate}\",\n";
    //    json += "  \"Products\": [\n";
    //    for (int i = 0; i < data.Products.Count; i++)
    //    {
    //        var p = data.Products[i];
    //        json += "    {\n";
    //        json += $"      \"Product\": \"{EscapeJson(p.Product)}\",\n";
    //        json += $"      \"HSN\": \"{p.HSN}\",\n";
    //        json += $"      \"Qty\": {p.Qty},\n";
    //        json += $"      \"Gross\": {p.Gross},\n";
    //        json += $"      \"Discount\": {p.Discount},\n";
    //        json += $"      \"Taxable\": {p.Taxable},\n";
    //        json += $"      \"TaxType\": \"{p.TaxType}\",\n";
    //        json += $"      \"Tax\": {p.Tax},\n";
    //        json += $"      \"Total\": {p.Total}\n";
    //        json += "    }" + (i < data.Products.Count - 1 ? "," : "") + "\n";
    //    }
    //    json += "  ]\n";
    //    json += "}";
    //    return json;
    //}

    private static string EscapeJson(string str)
    {
        if (string.IsNullOrEmpty(str)) return "";
        return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }
}
