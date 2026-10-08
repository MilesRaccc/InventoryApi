using InventoryApi.Data;
using InventoryApi.Dtos.Product;
using InventoryApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryApi.Controllers
{
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

        #endregion

        #region API Methods

        [HttpGet]
        public async Task<IActionResult> Get(string? search)
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x => EF.Functions.ILike(x.Name, $"%{search}%"));
            }

            var products = await query.ToListAsync();

            return Ok(products.Select(ToDto));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
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
            await _context.SaveChangesAsync();

            var returnDto = ToDto(product);

            return CreatedAtAction(nameof(GetById), new { id = product.Id }, returnDto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, CreateProductDto updateDto)
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
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
