using System.ComponentModel.DataAnnotations;

namespace InventoryApi.Helpers
{
    public class ProductQueryParameters : IValidatableObject
    {
        [StringLength(100)]
        public string? Search { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        [Range(0, int.MaxValue)]
        public int? MinQuantity { get; set; }
        public ProductSortField SortBy { get; set; } = ProductSortField.Id;
        public SortOrder SortOrder { get; set; } = SortOrder.Asc;
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;
        [Range(1, 100)]
        public int PageSize { get; set; } = 10;

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            var validationList = new List<ValidationResult>();

            if (MinPrice.HasValue &&
                MaxPrice.HasValue &&
                MinPrice > MaxPrice)
            {
                validationList.Add(new ValidationResult(
                    "MinPrice cannot be greater than MaxPrice.",
                    new[] { nameof(MinPrice), nameof(MaxPrice) }
                ));
            }

            return validationList;
        }
    }
}
