namespace OrderExtractorSample;

public class InvoiceData
{
    // Product Details Section (Top)
    public string SKU { get; set; }
    public string Size { get; set; }
    public int Qty { get; set; }
    public string Color { get; set; }
    public string OrderNo { get; set; }

    // Ship To Information
    public string ShipTo { get; set; }

    // Seller Information
    public string SellerName { get; set; }
    public string SellerGSTIN { get; set; }

    // Order Information
    public string PurchaseOrderNo { get; set; }
    public string InvoiceNo { get; set; }
    public string OrderDate { get; set; }
    public string InvoiceDate { get; set; }

    // Product Line Items
    public List<ProductDetail> Products { get; set; } = new List<ProductDetail>();

    public decimal TotalTax { get; set; }
    public decimal GrandTotal { get; set; }
}
