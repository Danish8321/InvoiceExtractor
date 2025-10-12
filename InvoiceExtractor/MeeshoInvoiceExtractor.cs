using System.Text.RegularExpressions;

namespace OrderExtractorSample;

public class MeeshoInvoiceExtractor
{
    // Common color names used in products
    private static readonly HashSet<string> CommonColors = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Basic colors
            "Red", "Blue", "Green", "Yellow", "Orange", "Purple", "Pink", "Brown", "Black", "White",
            "Grey", "Gray", "Beige", "Cream", "Ivory", "Navy", "Maroon", "Teal", "Cyan", "Magenta",
            "Lavender", "Turquoise", "Tan", "Olive", "Peach", "Mint", "Coral", "Salmon", "Gold",
            "Silver", "Bronze", "Copper", "Platinum", "Rose", "Burgundy", "Indigo", "Violet",
            "Mustard", "Khaki", "Rust", "Plum", "Mauve", "Crimson", "Scarlet", "Azure",
            
            // Multi-word colors and variations
            "Light Blue", "Dark Blue", "Sky Blue", "Royal Blue", "Baby Blue", "Powder Blue",
            "Light Green", "Dark Green", "Lime Green", "Mint Green", "Sea Green", "Forest Green",
            "Light Pink", "Hot Pink", "Baby Pink", "Rose Pink", "Dusty Pink",
            "Light Yellow", "Lemon Yellow", "Golden Yellow",
            "Light Grey", "Dark Grey", "Charcoal Grey", "Ash Grey",
            "Off White", "Pure White", "Cream White",
            "Wine Red", "Blood Red", "Cherry Red",
            "Multicolor", "Multi Color", "Multicolour", "Multi Colour",
            "Assorted", "Mixed", "Rainbow", "Combo"
        };

    public static InvoiceData ExtractInvoiceData(string invoiceText)
    {
        var invoiceData = new InvoiceData();

        try
        {
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

            // Extract Invoice Totals
            ExtractInvoiceTotals(invoiceText, invoiceData);
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
        }

        // Extract the section after SKU that contains Size, Qty, Color, Order No
        // Pattern: SKU + Size + Qty(digit) + Color + OrderNo(long number with underscore)
        var detailsPattern = @"Order No\.\w{8}((?:Free Size|One Size|Standard Size|XXL|XL|L|M|S|XS|XXS|\d+\s*(?:cm|CM|inch|Inch)))(\d+)(\w+?)(\d{15,}_\d+)";
        var detailsMatch = Regex.Match(text, detailsPattern, RegexOptions.IgnoreCase);

        if (detailsMatch.Success)
        {
            // Size
            data.Size = detailsMatch.Groups[1].Value.Trim();

            // Qty
            data.Qty = int.Parse(detailsMatch.Groups[2].Value);

            // Color - the word between Qty and Order Number
            string potentialColor = detailsMatch.Groups[3].Value;

            // Validate if it's a known color
            if (CommonColors.Contains(potentialColor))
            {
                data.Color = potentialColor;
            }
            else
            {
                // If not in common colors, still use it (might be a brand-specific color)
                data.Color = potentialColor;
            }

            // Order No
            data.OrderNo = detailsMatch.Groups[4].Value;
        }
        else
        {
            // Fallback: try to extract individually
            var sizeMatch = Regex.Match(text, @"Order No\.\w{8}(Free Size|One Size|XXL|XL|L|M|S)", RegexOptions.IgnoreCase);
            if (sizeMatch.Success)
            {
                data.Size = sizeMatch.Groups[1].Value.Trim();
            }

            // Try to extract order number separately
            var orderMatch = Regex.Match(text, @"Order No\.[\w\s]+?(\d{15,}_\d+)");
            if (orderMatch.Success)
            {
                data.OrderNo = orderMatch.Groups[1].Value;

                var colorPattern = @"(?:Free Size|Size|XL|L|M|S)\d+(\w+?)\d{15,}_";
                var colorMatch = Regex.Match(text, colorPattern, RegexOptions.IgnoreCase);
                if (colorMatch.Success)
                {
                    data.Color = colorMatch.Groups[1].Value;
                }
            }

            // Extract qty
            var qtyPattern = @"(?:Free Size|Size|XL|L|M|S)(\d+)";
            var qtyMatch = Regex.Match(text, qtyPattern, RegexOptions.IgnoreCase);
            if (qtyMatch.Success)
            {
                data.Qty = int.Parse(qtyMatch.Groups[1].Value);
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
        }
    }

    private static void ExtractSellerInfo(string text, InvoiceData data)
    {
        // Seller Name: Sold by : FAIZAN AHMAD (text until next field or address)
        var sellerMatch = Regex.Match(text, @"Sold by\s*:\s*([A-Z\s]+?)(?=\s+[A-Z][a-z]|\s+GSTIN)");
        if (sellerMatch.Success)
        {
            data.SellerName = sellerMatch.Groups[1].Value.Trim();
        }

        // Seller GSTIN: GSTIN - 10DQLPA9951C1Z5
        var gstinMatch = Regex.Match(text, @"GSTIN\s*[:\-–]?\s*([A-Z0-9]{15})", RegexOptions.IgnoreCase);
        if (gstinMatch.Success)
        {
            data.SellerGSTIN = gstinMatch.Groups[1].Value.Trim();
        }
    }

    private static void ExtractOrderInfo(string text, InvoiceData data)
    {
        Console.WriteLine("\n=== ORDER DETAILS ===");

        // Purchase Order No.204827517525195264
        var poMatch = Regex.Match(text, @"Purchase Order No\.(\d+)");
        if (poMatch.Success)
        {
            data.PurchaseOrderNo = poMatch.Groups[1].Value.Trim();
            Console.WriteLine($"Purchase Order No: {data.PurchaseOrderNo}");
        }

        // Invoice No.d2rnq26255 - exactly 10 characters (alphanumeric)
        var invoiceMatch = Regex.Match(text, @"Invoice No\.([a-z0-9]{10})", RegexOptions.IgnoreCase);
        if (invoiceMatch.Success)
        {
            data.InvoiceNo = invoiceMatch.Groups[1].Value.Trim();
            Console.WriteLine($"Invoice No: {data.InvoiceNo}");
        }

        // Order Date01.10.2025
        var orderDateMatch = Regex.Match(text, @"Order Date([\d.]+)");
        if (orderDateMatch.Success)
        {
            data.OrderDate = orderDateMatch.Groups[1].Value.Trim();
            Console.WriteLine($"Order Date: {data.OrderDate}");
        }

        // Invoice Date01.10.2025
        var invoiceDateMatch = Regex.Match(text, @"Invoice Date([\d.]+)");
        if (invoiceDateMatch.Success)
        {
            data.InvoiceDate = invoiceDateMatch.Groups[1].Value.Trim();
            Console.WriteLine($"Invoice Date: {data.InvoiceDate}");
        }
    }

    private static void ExtractProductLineItems(string text, InvoiceData data)
    {
        Console.WriteLine("\n=== PRODUCT ITEMS ===");

        // Pattern: Find section between table header and final Total line
        // Use a more specific pattern to avoid stopping at column header
        var tableSectionMatch = Regex.Match(text, @"DescriptionHSNQtyGross AmountDiscountTaxable ValueTaxesTotal(.+?)(?=TotalRs\.)", RegexOptions.Singleline);

        if (!tableSectionMatch.Success)
        {
            Console.WriteLine("Could not find product table section");
            return;
        }

        string tableContent = tableSectionMatch.Groups[1].Value;

        // Pattern for each product line - handles both IGST and CGST+SGST
        var productPattern = @"(.+?)(?=\d{6})(\d{6})(\d+|NA)Rs\.([\d.]+)Rs\.([\d.]+)Rs\.([\d.]+)((?:(?:IGST|CGST|SGST)\s*@[\d.]+%\s*:?\s*Rs\.[\d.]+\s*)+)Rs\.([\d.]+)";

        var matches = Regex.Matches(tableContent, productPattern);

        Console.WriteLine($"Found {matches.Count} product(s)\n");

        int productNum = 1;
        foreach (Match match in matches)
        {
            if (match.Success)
            {
                string productName = match.Groups[1].Value.Trim();
                string hsn = match.Groups[2].Value;
                string qtyStr = match.Groups[3].Value;
                string gross = match.Groups[4].Value;
                string discount = match.Groups[5].Value;
                string taxable = match.Groups[6].Value;
                string taxSection = match.Groups[7].Value; // Contains all tax info
                string total = match.Groups[8].Value;

                //// Parse tax section to extract tax type and amount
                //string taxType = "";
                //decimal taxAmount = 0;
                //// Extract all tax components
                //var taxMatches = Regex.Matches(taxSection, @"(IGST|CGST|SGST)\s*@([\d.]+)%\s*:?\s*Rs\.([\d.]+)");
                //var taxComponents = new List<string>();
                //foreach (Match taxMatch in taxMatches)
                //{
                //    string taxName = taxMatch.Groups[1].Value;
                //    string taxRate = taxMatch.Groups[2].Value;
                //    decimal taxVal = decimal.Parse(taxMatch.Groups[3].Value);
                //    taxComponents.Add($"{taxName} @{taxRate}%");
                //    taxAmount += taxVal;
                //}
                //taxType = string.Join(" + ", taxComponents);

                var product = new ProductDetail
                {
                    Description = productName,
                    HSN = hsn,
                    Qty = qtyStr,
                    GrossAmount = gross,
                    Discount = discount,
                    TaxableValue = taxable,
                    Taxes = taxSection,
                    Total = total
                };

                data.Products.Add(product);
                Console.WriteLine($"Product {productNum}: {product.Description}");
                Console.WriteLine($"  HSN: {product.HSN} | Qty: {product.Qty} | Total: Rs.{product.Total}");
                productNum++;
            }
        }
    }

    private static void ExtractInvoiceTotals(string text, InvoiceData data)
    {
        // Pattern: TotalRs.17.85Rs.613.00
        // The first Rs. amount is total tax, second is grand total
        var totalMatch = Regex.Match(text, @"TotalRs\.([\d.]+)Rs\.([\d.]+)");

        if (totalMatch.Success)
        {
            data.TotalTax = decimal.Parse(totalMatch.Groups[1].Value);
            data.GrandTotal = decimal.Parse(totalMatch.Groups[2].Value);
        }
    }

    public static void DisplayInvoiceData(InvoiceData data)
    {
        Console.WriteLine("\n╔════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                          INVOICE HEADER                                ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════════════╝");
        Console.WriteLine($"{"SKU:",-25} {data.SKU}");
        Console.WriteLine($"{"Size:",-25} {data.Size}");
        Console.WriteLine($"{"Qty:",-25} {data.Qty}");
        Console.WriteLine($"{"Color:",-25} {data.Color}");
        Console.WriteLine($"{"Order No:",-25} {data.OrderNo}");
        Console.WriteLine($"{"Ship To:",-25} {(data.ShipTo?.Length > 50 ? data.ShipTo.Substring(0, 47) + "..." : data.ShipTo)}");
        Console.WriteLine($"{"Seller Name:",-25} {data.SellerName}");
        Console.WriteLine($"{"Seller GSTIN:",-25} {data.SellerGSTIN}");
        Console.WriteLine($"{"Purchase Order No:",-25} {data.PurchaseOrderNo}");
        Console.WriteLine($"{"Invoice No:",-25} {data.InvoiceNo}");
        Console.WriteLine($"{"Order Date:",-25} {data.OrderDate}");
        Console.WriteLine($"{"Invoice Date:",-25} {data.InvoiceDate}");

        Console.WriteLine("\n╔════════════════════════════════════════════════════════════════════════╗");
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
            Console.WriteLine($"{"Tax:",-25} {product.Taxes}");
            Console.WriteLine($"{"Total:",-25} Rs.{product.Total:N2}");
            Console.WriteLine("─────────────────────────────────────────────────────────────────────────");
            productNum++;
        }

        Console.WriteLine("\n╔════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                         INVOICE TOTALS                                 ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════════════╝");
        Console.WriteLine($"{"Total Tax:",-25} Rs.{data.TotalTax:N2}");
        Console.WriteLine($"{"Grand Total:",-25} Rs.{data.GrandTotal:N2}");
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
