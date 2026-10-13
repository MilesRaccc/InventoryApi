using System.ComponentModel.DataAnnotations;

namespace InventoryApi.DTOs;

public class AdjustStockDto
{
    [Range(-1000000, 1000000)]
    public int QuantityChange { get; set; }
}