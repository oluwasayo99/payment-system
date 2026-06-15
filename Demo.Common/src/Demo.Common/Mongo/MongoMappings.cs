using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace Demo.Common.Mongo;

public static class MongoMappings
{
    private static bool _initialized;
    public static void RegisterMappings()
    {
        if (_initialized) return;

        _initialized = true;


        BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));

        var pack = new ConventionPack{
            new EnumRepresentationConvention(BsonType.String)
        };

        ConventionRegistry.Register(
            "EnumAsString",
            pack,
            _ => true
        );
    }
}