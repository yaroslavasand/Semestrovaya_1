using System.Collections.Concurrent;

namespace Semestrovaya_1.Orm;

// Кэш карт моделей: рефлексия по атрибутам выполняется
// один раз для каждого типа, а не на каждый запрос.
// EntityMap после создания не изменяется,
// поэтому общий экземпляр безопасен при параллельных запросах.
public static class EntityMapCache
{
    private static readonly ConcurrentDictionary<Type, object> Cache =
        new();

    public static EntityMap<T> Get<T>()
        where T : class, new()
    {
        return (EntityMap<T>)Cache.GetOrAdd(
            typeof(T),
            _ => new EntityMap<T>());
    }
}