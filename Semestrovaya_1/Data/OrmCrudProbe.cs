using System;
using System.Linq;
using System.Threading.Tasks;
using Semestrovaya_1.Models;
using Semestrovaya_1.Orm;

namespace Semestrovaya_1.Data;

public static class OrmCrudProbe
{
    public static async Task RunAsync(Database database)
    {
        await CheckAsync<CardSet>(database);
        await CheckAsync<Domain>(database);
        await CheckAsync<CardType>(database);
        await CheckAsync<Tag>(database);

        Console.WriteLine(
            "ORM: все проверки CRUD успешно завершены.");
    }

    private static async Task CheckAsync<T>(Database database)
        where T : class, new()
    {
        var repository = new OrmRepository<T>(database);
        var map = EntityMapCache.Get<T>();

        var codeColumn = map.Columns.Single(
            column => column.Name == "code");

        var nameColumn = map.Columns.Single(
            column => column.Name == "name");

        var descriptionColumn = map.Columns.Single(
            column => column.Name == "description");

        var entity = new T();

        var code = "orm-" + Guid.NewGuid().ToString("N")[..20];
        const string initialName = "ORM temporary record";
        const string initialDescription = "Created by CRUD check";

        codeColumn.Property.SetValue(entity, code);
        nameColumn.Property.SetValue(entity, initialName);
        descriptionColumn.Property.SetValue(
            entity,
            initialDescription);

        var id = 0;

        try
        {
            id = await repository.InsertAsync(entity);

            Ensure(id > 0, "Insert не вернул корректный Id.");

            Ensure(
                Equals(map.PrimaryKey.Property.GetValue(entity), id),
                "Insert не записал Id в объект.");

            Console.WriteLine($"{typeof(T).Name}: Create — OK");

            var loaded = await repository.GetByIdAsync(id);

            Ensure(loaded is not null, "Созданная запись не найдена.");

            Ensure(
                Equals(codeColumn.Property.GetValue(loaded!), code),
                "Прочитан неправильный код записи.");

            Ensure(
                Equals(
                    nameColumn.Property.GetValue(loaded!),
                    initialName),
                "Прочитано неправильное название.");

            Ensure(
                Equals(
                    descriptionColumn.Property.GetValue(loaded!),
                    initialDescription),
                "Прочитано неправильное описание.");

            var all = await repository.GetAllAsync();

            Ensure(
                all.Any(item =>
                    Equals(
                        map.PrimaryKey.Property.GetValue(item),
                        id)),
                "Созданная запись отсутствует в общем списке.");

            Console.WriteLine($"{typeof(T).Name}: Read — OK");

            const string updatedName = "ORM updated record";
            const string updatedDescription = "Updated by CRUD check";

            nameColumn.Property.SetValue(entity, updatedName);
            descriptionColumn.Property.SetValue(
                entity,
                updatedDescription);

            Ensure(
                await repository.UpdateAsync(entity),
                "Update не изменил запись.");

            var updated = await repository.GetByIdAsync(id);

            Ensure(updated is not null, "Запись пропала после Update.");

            Ensure(
                Equals(
                    nameColumn.Property.GetValue(updated!),
                    updatedName),
                "Новое название не сохранилось.");

            Ensure(
                Equals(
                    descriptionColumn.Property.GetValue(updated!),
                    updatedDescription),
                "Новое описание не сохранилось.");

            Ensure(
                Equals(codeColumn.Property.GetValue(updated!), code),
                "Update неожиданно изменил код.");

            Console.WriteLine($"{typeof(T).Name}: Update — OK");

            Ensure(
                await repository.DeleteAsync(id),
                "Delete не удалил запись.");

            Ensure(
                await repository.GetByIdAsync(id) is null,
                "После Delete запись всё ещё существует.");

            Ensure(
                !await repository.DeleteAsync(id),
                "Повторный Delete должен вернуть false.");

            Ensure(
                !await repository.UpdateAsync(entity),
                "Update удалённой записи должен вернуть false.");

            Console.WriteLine($"{typeof(T).Name}: Delete — OK");
        }
        finally
        {
            if (id > 0)
            {
                await repository.DeleteAsync(id);
            }
        }
    }

    private static void Ensure(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(
                $"Проверка ORM не пройдена: {message}");
        }
    }
}