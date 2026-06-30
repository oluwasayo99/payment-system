using Demo.Inventory.Service.Dtos;
using Demo.Inventory.Service.Services;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Inventory.Service.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly InventoryService _inventoryService;

    public InventoryController(InventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    /// <summary>
    /// Endpoint 1: Get products with N+1 query problem (slow)
    /// Makes separate queries for categories and reviews for each product
    /// </summary>
    [HttpGet("products/slow")]
    public async Task<IActionResult> GetProductsSlow()
    {
        try
        {
            var (products, queryTimeMs) = await _inventoryService.GetProductsSlowAsync();
            
            return Ok(new
            {
                message = "Products retrieved with N+1 problem (inefficient)",
                queryTimeMs,
                productCount = products.Count,
                products
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Endpoint 2: Get products with optimized JOIN query (fast)
    /// Uses a single query with JOINs to fetch all data
    /// </summary>
    [HttpGet("products/fast")]
    public async Task<IActionResult> GetProductsFast()
    {
        try
        {
            var (products, queryTimeMs) = await _inventoryService.GetProductsFastAsync();
            
            return Ok(new
            {
                message = "Products retrieved with optimized JOIN query",
                queryTimeMs,
                productCount = products.Count,
                products
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Endpoint 3: Get products with in-memory caching (fastest after first call)
    /// Uses IMemoryCache with 1-hour sliding expiration
    /// </summary>
    [HttpGet("products/cached")]
    public async Task<IActionResult> GetProductsCached()
    {
        try
        {
            var (products, queryTimeMs, fromCache) = await _inventoryService.GetProductsCachedAsync();
            
            return Ok(new
            {
                message = fromCache 
                    ? "Products retrieved from in-memory cache" 
                    : "Products retrieved from database and cached",
                queryTimeMs,
                fromCache,
                productCount = products.Count,
                products
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Endpoint 4: Get all users with their orders and products
    /// Returns users with nested orders containing products
    /// </summary>
    [HttpGet("users/orders")]
    public async Task<IActionResult> GetUsersWithOrders()
    {
        try
        {
            var (users, queryTimeMs) = await _inventoryService.GetUsersWithOrdersAsync();
            
            return Ok(new
            {
                message = "Users with orders retrieved successfully",
                queryTimeMs,
                userCount = users.Count,
                data = users
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Endpoint 5: Update a product and sync cache
    /// Updates product in database, invalidates cache, and updates Redis
    /// </summary>
    [HttpPut("products/{id}")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] ProductUpdateRequest request)
    {
        try
        {
            var (product, success, error) = await _inventoryService.UpdateProductAsync(id, request);
            
            if (!success)
            {
                return BadRequest(new { error });
            }
            
            return Ok(new
            {
                message = "Product updated successfully and cache synchronized",
                product
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Endpoint 6: Read a product directly from Redis using the key
    /// </summary>
    [HttpGet("products/redis/{key}")]
    public async Task<IActionResult> GetProductFromRedis(string key)
    {
        try
        {
            var product = await _inventoryService.GetProductFromRedisAsync(key);
            
            if (product == null)
            {
                return NotFound(new { message = "Product not found in Redis", key });
            }
            
            return Ok(new
            {
                message = "Product retrieved from Redis",
                key,
                product
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
