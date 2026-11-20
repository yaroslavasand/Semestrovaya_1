using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using NpgsqlTypes;

namespace Semestrovaya_1.Orm;
//EntityMap читает  метки рефлексией и строит «карту»:
//OrmRepository по этой карте сам собирает SQL
public sealed class OrmRepository<T> where T : class, new()
{
    private readonly Database _database;
    private readonly EntityMap<T> _map;
    private readonly string _selectColumns;

    public OrmRepository(Database database)
    {
        _database = database;
        _map = EntityMapCache.Get<T>();

        _selectColumns = string.Join(
            ", ",
            _map.Columns.Select(column => column.SqlName));
    }

    public Task<List<T>> GetAllAsync()
    {
        var sql =
            $"SELECT {_selectColumns} "
            + $"FROM {_map.SqlTableName} "
            + $"ORDER BY {_map.PrimaryKey.SqlName};";

        return _database.QueryAsync<T>(sql, ReadEntity);
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        var sql =
            $"SELECT {_selectColumns} "
            + $"FROM {_map.SqlTableName} "
            + $"WHERE {_map.PrimaryKey.SqlName} = $1;";

        var items = await _database.QueryAsync<T>(
            sql,
            ReadEntity,
            CreateParameter(_map.PrimaryKey, id));

        return items.Count == 0 ? null : items[0];
    }

    public async Task<int> InsertAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var columns = _map.InsertColumns;

        string sql;

        if (columns.Count == 0)
        {
            sql =
                $"INSERT INTO {_map.SqlTableName} "
                + "DEFAULT VALUES "
                + $"RETURNING {_map.PrimaryKey.SqlName};";
        }
        else
        {
            var names = string.Join(
                ", ",
                columns.Select(column => column.SqlName));

            var placeholders = string.Join(
                ", ",
                Enumerable.Range(1, columns.Count)
                    .Select(index => $"${index}"));

            sql =
                $"INSERT INTO {_map.SqlTableName} ({names}) "
                + $"VALUES ({placeholders}) "
                + $"RETURNING {_map.PrimaryKey.SqlName};";
        }

        var parameters = columns
            .Select(column => CreateParameter(
                column,
                column.Property.GetValue(entity)))
            .ToArray();

        var result = await _database.ExecuteScalarAsync(
            sql,
            parameters);

        if (result is not int id)
        {
            throw new InvalidOperationException(
                "База не вернула целочисленный идентификатор записи.");
        }

        _map.PrimaryKey.Property.SetValue(entity, id);

        return id;
    }

    public async Task<bool> UpdateAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var columns = _map.UpdateColumns;

        if (columns.Count == 0)
        {
            throw new InvalidOperationException(
                $"У модели {typeof(T).Name} нет полей для изменения.");
        }

        var assignments = string.Join(
            ", ",
            columns.Select(
                (column, index) =>
                    $"{column.SqlName} = ${index + 1}"));

        var keyPosition = columns.Count + 1;

        var sql =
            $"UPDATE {_map.SqlTableName} "
            + $"SET {assignments} "
            + $"WHERE {_map.PrimaryKey.SqlName} = ${keyPosition};";

        var parameters = columns
            .Select(column => CreateParameter(
                column,
                column.Property.GetValue(entity)))
            .ToList();

        parameters.Add(CreateParameter(
            _map.PrimaryKey,
            _map.PrimaryKey.Property.GetValue(entity)));

        var affectedRows = await _database.ExecuteAsync(
            sql,
            parameters.ToArray());

        return affectedRows == 1;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var sql =
            $"DELETE FROM {_map.SqlTableName} "
            + $"WHERE {_map.PrimaryKey.SqlName} = $1;";

        var affectedRows = await _database.ExecuteAsync(
            sql,
            CreateParameter(_map.PrimaryKey, id));

        return affectedRows == 1;
    }

    private T ReadEntity(NpgsqlDataReader reader)
    {
        var entity = new T();

        for (var index = 0; index < _map.Columns.Count; index++)
        {
            var column = _map.Columns[index];

            if (reader.IsDBNull(index))
            {
                var propertyType = column.Property.PropertyType;

                if (propertyType.IsValueType
                    && Nullable.GetUnderlyingType(propertyType) is null)
                {
                    throw new InvalidOperationException(
                        $"Столбец {column.Name} содержит NULL, "
                        + $"но свойство {column.Property.Name} "
                        + "не допускает пустое значение.");
                }

                column.Property.SetValue(entity, null);
                continue;
            }

            column.Property.SetValue(
                entity,
                reader.GetValue(index));
        }

        return entity;
    }

    private static NpgsqlParameter CreateParameter(
        ColumnMap column,
        object? value)
    {
        var propertyType =
            Nullable.GetUnderlyingType(column.Property.PropertyType)
            ?? column.Property.PropertyType;

        NpgsqlDbType databaseType;

        if (propertyType == typeof(int))
        {
            databaseType = NpgsqlDbType.Integer;
        }
        else if (propertyType == typeof(string))
        {
            databaseType = NpgsqlDbType.Text;
        }
        else if (propertyType == typeof(bool))
        {
            databaseType = NpgsqlDbType.Boolean;
        }
        else if (propertyType == typeof(DateTime))
        {
            databaseType = NpgsqlDbType.TimestampTz;
        }    
        else
        {
            throw new NotSupportedException(
                $"Тип {propertyType.Name} свойства "
                + $"{column.Property.Name} пока не поддерживается ORM.");
        }

        return new NpgsqlParameter
        {
            NpgsqlDbType = databaseType,
            Value = value ?? DBNull.Value
        };
    }
}