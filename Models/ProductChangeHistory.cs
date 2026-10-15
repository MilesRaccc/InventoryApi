namespace InventoryApi.Models;

public class ProductChangeHistory
{
    public Guid Id { get; set; }

    // Product ID at the time of the change.
    // Intentionally has no foreign key to Product.
    public Guid ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public ProductChangeAction Action { get; set; }

    public DateTimeOffset ChangedAt { get; set; }

    // Identity user ID; no foreign key to ApplicationUser.
    public string? ChangedBy { get; set; }

    // JSON snapshots of the product before and after the change.
    public string? OldValues { get; set; }

    public string? NewValues { get; set; }
}

public enum ProductChangeAction
{
    Created = 1,
    Updated = 2,
    Deleted = 3
}