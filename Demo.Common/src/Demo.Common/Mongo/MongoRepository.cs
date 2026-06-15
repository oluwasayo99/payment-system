using System.Linq.Expressions;
using MongoDB.Driver;

namespace Demo.Common.Mongo;

public class MongoRepository<T>: IRepository<T> where T: IEntity
{
    private readonly IMongoCollection<T> mongoCollection;
    private readonly FilterDefinitionBuilder<T> filterBuilder = Builders<T>.Filter;

    private const string IdField = "_id";

    public MongoRepository(IMongoDatabase database)
    {
        var collectionName = typeof(T).Name.ToLower();
        this.mongoCollection = database.GetCollection<T>(collectionName);
    }

    public async Task CreateAsync(T entity)
    {
        if(entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        if(entity.Id == Guid.Empty) entity.Id = Guid.NewGuid();
        await mongoCollection.InsertOneAsync(entity);
    }

    public async Task<IReadOnlyCollection<T>> GetAllAsync()
    {
        return await mongoCollection.Find(filterBuilder.Empty).ToListAsync();
    }

    public async Task<IReadOnlyCollection<T>> GetAllAsync(Expression<Func<T, bool>> filter)
    {
        return await mongoCollection.Find(filter).ToListAsync();
    }

    public async Task<T> GetAsync(Guid id)
    {
        FilterDefinition<T> filter = filterBuilder.Eq(entity => entity.Id, id);
        return await mongoCollection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<T> GetAsync(Expression<Func<T, bool>> filter)
    {
        return await mongoCollection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task RemoveAsync(Guid id)
    {
        FilterDefinition<T> filter = filterBuilder.Eq(existing => existing.Id, id);
        await mongoCollection.DeleteOneAsync(filter);
    }

    public async Task UpdateAsync(T entity)
    {
        if(entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        FilterDefinition<T> filter = filterBuilder.Eq(existing => existing.Id, entity.Id);
        await mongoCollection.ReplaceOneAsync(filter, entity);
    }
}