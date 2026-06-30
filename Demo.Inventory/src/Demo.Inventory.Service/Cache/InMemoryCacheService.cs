using Demo.Inventory.Service.Dtos;
using Microsoft.Extensions.Caching.Memory;

namespace Demo.Inventory.Service.Cache;

public interface IInMemoryCacheService
{
    Task<List<ProductWithDetailsDto>?> GetProductsAsync();
    Task SetProductsAsync(List<ProductWithDetailsDto> products);
    Task<ProductWithDetailsDto?> GetProductAsync(Guid productId);
    Task SetProductAsync(Guid productId, ProductWithDetailsDto product);
    void RemoveProduct(Guid productId);
    void RemoveAllProducts();
}

public class InMemoryCacheService : IInMemoryCacheService
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public InMemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<List<ProductWithDetailsDto>?> GetProductsAsync()
    {
        var products = _cache.Get<List<ProductWithDetailsDto>>(CacheKeys.ProductsAll);
        return Task.FromResult(products);
    }

    public Task SetProductsAsync(List<ProductWithDetailsDto> products)
    {
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(CacheDuration)
            .SetAbsoluteExpiration(TimeSpan.FromHours(2));
        
        _cache.Set(CacheKeys.ProductsAll, products, cacheOptions);
        return Task.CompletedTask;
    }

    public Task<ProductWithDetailsDto?> GetProductAsync(Guid productId)
    {
        var key = string.Format(CacheKeys.ProductById, productId);
        var product = _cache.Get<ProductWithDetailsDto>(key);
        return Task.FromResult(product);
    }

    public Task SetProductAsync(Guid productId, ProductWithDetailsDto product)
    {
        var key = string.Format(CacheKeys.ProductById, productId);
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(CacheDuration)
            .SetAbsoluteExpiration(TimeSpan.FromHours(2));
        
        _cache.Set(key, product, cacheOptions);
        return Task.CompletedTask;
    }

    public void RemoveProduct(Guid productId)
    {
        var key = string.Format(CacheKeys.ProductById, productId);
        _cache.Remove(key);
    }

    public void RemoveAllProducts()
    {
        _cache.Remove(CacheKeys.ProductsAll);
    }
}
