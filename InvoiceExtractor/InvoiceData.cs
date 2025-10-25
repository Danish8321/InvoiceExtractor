using OrderExtractorSample;

namespace InvoiceExtractor;

public class InvoiceData
{
    public string SKU { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public int Qty { get; set; }
    public string Color { get; set; } = string.Empty;
    public string OrderNo { get; set; } = string.Empty;
    public string ShipTo { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
    public string SellerGSTIN { get; set; } = string.Empty;
    public string PurchaseOrderNo { get; set; } = string.Empty;
    public string InvoiceNo { get; set; } = string.Empty;
    public string OrderDate { get; set; } = string.Empty;
    public string InvoiceDate { get; set; } = string.Empty;
    public List<ProductDetail> Products { get; set; } = [];
    public decimal TotalTax { get; set; }
    public decimal GrandTotal { get; set; }
}
