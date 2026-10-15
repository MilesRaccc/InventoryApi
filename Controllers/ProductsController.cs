using System.Linq.Expressions;
using InventoryApi.Data;
using InventoryApi.Dtos.Product;
using InventoryApi.DTOs;
using InventoryApi.Helpers;
using InventoryApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace InventoryApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        #region Constants

        private readonly AppDbContext _context;

        #endregion

        #region Constructor

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        #endregion

        #region Private Methods

        private static ProductDto ToDto(Product product)
        {
            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Quantity = product.Quantity,
                CreatedAt = product.CreatedAt
            };
        }

        private void AddChangeHistory(
            Product product,
            ProductChangeAction action,
            object? oldValues,
            object? newValues)
        {
            _context.ProductChangeHistory.Add(new ProductChangeHistory
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Action = action,
                ChangedAt = DateTimeOffset.UtcNow,
                ChangedBy = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub"),
                OldValues = oldValues is null
                    ? null
                    : JsonSerializer.Serialize(oldValues),
                NewValues = newValues is null
                    ? null
                    : JsonSerializer.Serialize(newValues)
            });
        }

        #endregion

        #region API Methods

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] ProductQueryParameters parameters)
        {
            var query = _context.Products.AsQueryable();

            //Filtration
            if (!string.IsNullOrWhiteSpace(parameters.Search))
            {
                query = query.Where(x => EF.Functions.ILike(x.Name, $"%{parameters.Search}%"));
            }

            if(parameters.MinPrice.HasValue)
            {
                query = query.Where(x => x.Price >= parameters.MinPrice);
            }

            if (parameters.MaxPrice.HasValue)
            {
                query = query.Where(x => x.Price <= parameters.MaxPrice);
            }

            if (parameters.MinQuantity.HasValue)
            {
                query = query.Where(x => x.Quantity >= parameters.MinQuantity);
            }

            //Sorting
            bool ascending = parameters.SortOrder == SortOrder.Asc;

            switch (parameters.SortBy)
            {
                case ProductSortField.Id:
                    query = ascending ? query.OrderBy(x => x.Id) : query.OrderByDescending(x => x.Id); 
                    break;
                case ProductSortField.Name:
                    query = ascending ? query.OrderBy(x => x.Name) : query.OrderByDescending(x => x.Name);
                    break;
                case ProductSortField.Price:
                    query = ascending ? query.OrderBy(x => x.Price) : query.OrderByDescending(x => x.Price);
                    break;
                case ProductSortField.Quantity:
                    query = ascending ? query.OrderBy(x => x.Quantity) : query.OrderByDescending(x => x.Quantity);
                    break;
                case ProductSortField.CreatedAt:
                    query = ascending ? query.OrderBy(x => x.CreatedAt) : query.OrderByDescending(x => x.CreatedAt);
                    break;
            }

            //Building of response
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)parameters.PageSize);
            var products = await query.Skip((parameters.Page - 1) * parameters.PageSize).Take(parameters.PageSize).ToListAsync();

            return Ok(new PagedResult<ProductDto>
            {
                Items = products.Select(ToDto).ToList(),
                Page = parameters.Page,
                PageSize = parameters.PageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var product = await _context.Products.FirstOrDefaultAsync(x => x.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return Ok(ToDto(product));
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateProductDto creationDto)
        {
            var product = new Product
            {
                Name = creationDto.Name,
                Description = creationDto.Description,
                Price = creationDto.Price,
                Quantity = creationDto.Quantity,
                CreatedAt = DateTime.UtcNow
            };            

            _context.Products.Add(product);

            AddChangeHistory(
                product,
                ProductChangeAction.Created,
                oldValues: null,
                newValues: ToDto(product));

            await _context.SaveChangesAsync();

            var returnDto = ToDto(product);

            return CreatedAtAction(nameof(GetById), new { id = product.Id }, returnDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdateProductDto updateDto)
        {
            var product = await _context.Products.FirstOrDefaultAsync(x => x.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            product.Name = updateDto.Name;
            product.Description = updateDto.Description;
            product.Price = updateDto.Price;
            product.Quantity = updateDto.Quantity;

            await _context.SaveChangesAsync();

            return Ok(ToDto(product));
        }

        [HttpPost("{id}/stock-adjustments")]
        public async Task<IActionResult> AdjustStock(Guid id, [FromQuery]AdjustStockDto adjustDto)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(x => x.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            var newQuantity = (long)product.Quantity
                + adjustDto.QuantityChange;

            if (newQuantity < 0 || newQuantity > int.MaxValue)
            {
                return BadRequest(
                    "The resulting quantity is outside the allowed range.");
            }

            product.Quantity = (int)newQuantity;

            await _context.SaveChangesAsync();

            return Ok(ToDto(product));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var product = await _context.Products.FirstOrDefaultAsync(x => x.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        #endregion
    }
}
