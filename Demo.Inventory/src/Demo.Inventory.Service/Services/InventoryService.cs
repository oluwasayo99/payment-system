using Dapper;
using Demo.Common.Postgres;
using Demo.Common.Redis;
using Demo.Inventory.Service.Cache;
using Demo.Inventory.Service.Dtos;
using Npgsql;
using System.Diagnostics;
using System.Text.Json;

namespace Demo.Inventory.Service.Services;

public class InventoryService
{
    private readonly DbConnectionFactory _dbConnectionFactory;
    private readonly IInMemoryCacheService _cacheService;
    private readonly IRedisService _redisService;

    public InventoryService(
        DbConnectionFactory dbConnectionFactory,
        IInMemoryCacheService cacheService,
        IRedisService redisService)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _cacheService = cacheService;
        _redisService = redisService;
    }

    #region Endpoint 1: Slow (N+1 Problem)
    
    public async Task<(List<ProductWithDetailsDto> Products, long QueryTimeMs)> GetProductsSlowAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        
        await using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        
        // Get all products (1 query)
        var products = await connection.QueryAsync<ProductWithDetailsDto>(
            "SELECT id, name, description, price FROM products LIMIT 100");
        
        var productList = products.ToList();
        
        // N+1 problem: separate queries for each product's category and reviews
        foreach (var product in productList)
        {
            // Query category for each product (N queries)
            var category = await connection.QueryFirstOrDefaultAsync<CategoryDto>(
                "SELECT id, name, description FROM categories WHERE id = (SELECT category_id FROM products WHERE id = @ProductId)",
                new { ProductId = product.Id });
            product.Category = category;
            
            // Query reviews for each product (N queries)
            var reviews = await connection.QueryAsync<ReviewDto>(
                "SELECT id, rating, comment, created_at FROM reviews WHERE product_id = @ProductId",
                new { ProductId = product.Id });
            product.Reviews = reviews.ToList();
        }
        
        stopwatch.Stop();
        return (productList, stopwatch.ElapsedMilliseconds);
    }
    
    #endregion

    #region Endpoint 2: Fast (Optimized JOIN)
    
    public async Task<(List<ProductWithDetailsDto> Products, long QueryTimeMs)> GetProductsFastAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        
        await using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        
        // Single optimized query with JOINs
        const string sql = @"
            SELECT 
                p.id, p.name, p.description, p.price,
                c.id as CategoryId, c.name as CategoryName, c.description as CategoryDescription,
                r.id as ReviewId, r.rating, r.comment, r.created_at as CreatedAt
            FROM products p
            LEFT JOIN categories c ON p.category_id = c.id
            LEFT JOIN reviews r ON p.id = r.product_id
            LIMIT 100";
        
        var productDictionary = new Dictionary<Guid, ProductWithDetailsDto>();
        
        var results = await connection.QueryAsync<ProductWithDetailsDto, CategoryDto, ReviewDto, ProductWithDetailsDto>(
            sql,
            (product, category, review) =>
            {
                if (!productDictionary.TryGetValue(product.Id, out var productEntry))
                {
                    productEntry = product;
                    productEntry.Category = category;
                    productEntry.Reviews = new List<ReviewDto>();
                    productDictionary.Add(productEntry.Id, productEntry);
                }
                
                if (review?.Id != Guid.Empty && review?.Id != null)
                {
                    productEntry.Reviews.Add(review);
                }
                
                return productEntry;
            },
            splitOn: "CategoryId,ReviewId");
        
        stopwatch.Stop();
        return (productDictionary.Values.ToList(), stopwatch.ElapsedMilliseconds);
    }
    
    #endregion

    #region Endpoint 3: Cached (In-Memory Cache)
    
    public async Task<(List<ProductWithDetailsDto> Products, long QueryTimeMs, bool FromCache)> GetProductsCachedAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Try to get from cache first
        var cachedProducts = await _cacheService.GetProductsAsync();
        if (cachedProducts != null)
        {
            stopwatch.Stop();
            return (cachedProducts, stopwatch.ElapsedMilliseconds, true);
        }
        
        // If not in cache, fetch from database using optimized query
        var (products, _) = await GetProductsFastAsync();
        
        // Store in cache
        await _cacheService.SetProductsAsync(products);
        
        stopwatch.Stop();
        return (products, stopwatch.ElapsedMilliseconds, false);
    }
    
    #endregion

    #region Endpoint 4: Users with Orders and Products
    
    public async Task<(List<UserWithOrdersDto> Users, long QueryTimeMs)> GetUsersWithOrdersAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        
        await using var connection = await _dbConnectionFactory.CreateConnectionAsync();
        
        // Simple query - get all order items with related data
        const string sql = @"
            SELECT 
                u.username,
                o.id as OrderId,
                o.order_status as OrderStatus,
                o.total as Total,
                p.name as ProductName,
                oi.price as Price
            FROM users u
            INNER JOIN orders o ON u.id = o.user_id
            INNER JOIN order_items oi ON o.id = oi.order_id
            INNER JOIN products p ON oi.product_id = p.id
            ORDER BY u.username, o.created_at";
        
        // Simple flat query - Dapper will map columns to properties by name
        var rows = await connection.QueryAsync<UserOrderFlatRow>(sql);
        
        var userDictionary = new Dictionary<string, UserWithOrdersDto>();
        var orderDictionary = new Dictionary<Guid, OrderDto>();
        
        foreach (var row in rows)
        {
            // Get or create user
            if (!userDictionary.TryGetValue(row.Username, out var userEntry))
            {
                userEntry = new UserWithOrdersDto
                {
                    Username = row.Username,
                    Orders = new List<OrderDto>()
                };
                userDictionary.Add(row.Username, userEntry);
            }
            
            // Get or create order for this user
            if (!orderDictionary.TryGetValue(row.OrderId, out var orderEntry))
            {
                orderEntry = new OrderDto
                {
                    OrderId = row.OrderId.ToString()[..8].ToUpper(),
                    OrderStatus = row.OrderStatus,
                    Total = row.Total,
                    Products = new List<ProductInOrderDto>()
                };
                orderDictionary.Add(row.OrderId, orderEntry);
                userEntry.Orders.Add(orderEntry);
            }
            
            // Add product to order
            orderEntry.Products.Add(new ProductInOrderDto
            {
                ProductName = row.ProductName,
                Price = row.Price
            });
        }
        
        stopwatch.Stop();
        return (userDictionary.Values.ToList(), stopwatch.ElapsedMilliseconds);
    }
    
    // Simple flat DTO to match SQL columns
    private class UserOrderFlatRow
    {
        public string Username { get; set; } = string.Empty;
        public Guid OrderId { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
    
    #endregion

    #region Endpoint 5: Update Product
    
    public async Task<(ProductWithDetailsDto? Product, bool Success, string? Error)> UpdateProductAsync(Guid productId, ProductUpdateRequest request)
    {
        try
        {
            await using var connection = await _dbConnectionFactory.CreateConnectionAsync();
            
            // Update product in database
            const string updateSql = @"
                UPDATE products 
                SET name = @Name, description = @Description, price = @Price, category_id = @CategoryId
                WHERE id = @ProductId
                RETURNING id, name, description, price";
            
            var updatedProduct = await connection.QueryFirstOrDefaultAsync<ProductWithDetailsDto>(
                updateSql,
                new
                {
                    ProductId = productId,
                    request.Name,
                    request.Description,
                    request.Price,
                    request.CategoryId
                });
            
            if (updatedProduct == null)
            {
                return (null, false, "Product not found");
            }
            
            // Get category info
            var category = await connection.QueryFirstOrDefaultAsync<CategoryDto>(
                "SELECT id, name, description FROM categories WHERE id = @CategoryId",
                new { request.CategoryId });
            
            updatedProduct.Category = category;
            
            // Update in-memory cache
            await _cacheService.SetProductAsync(productId, updatedProduct);
            _cacheService.RemoveAllProducts(); // Invalidate all products cache
            
            // Update Redis cache
            var redisKey = $"inventory:product:{productId}";
            var productJson = JsonSerializer.Serialize(updatedProduct);
            await _redisService.SetAsync(redisKey, productJson, TimeSpan.FromHours(1));
            
            return (updatedProduct, true, null);
        }
        catch (Exception ex)
        {
            return (null, false, ex.Message);
        }
    }
    
    #endregion

    #region Endpoint 6: Read Product from Redis
    
    public async Task<ProductWithDetailsDto?> GetProductFromRedisAsync(string redisKey)
    {
        var productJson = await _redisService.GetAsync(redisKey);
        
        if (string.IsNullOrEmpty(productJson))
        {
            return null;
        }
        
        try
        {
            var product = JsonSerializer.Deserialize<ProductWithDetailsDto>(productJson);
            return product;
        }
        catch
        {
            return null;
        }
    }
    
    #endregion
}
