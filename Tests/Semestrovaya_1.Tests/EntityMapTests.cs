using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Semestrovaya_1.Models;
using Semestrovaya_1.Orm;

namespace Semestrovaya_1.Tests;

[TestClass]
public sealed class EntityMapTests
{
    [TestMethod]
    public void CardSet_UsesExpectedTableAndSchema()
    {
        var map = new EntityMap<CardSet>();

        Assert.AreEqual("card_sets", map.TableName);
        Assert.AreEqual("public", map.Schema);
        Assert.AreEqual(
            "\"public\".\"card_sets\"",
            map.SqlTableName);
    }

    [TestMethod]
    public void CardSet_MapsExpectedColumns()
    {
        var map = new EntityMap<CardSet>();

        var actual = map.Columns
            .Select(column => column.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        var expected = new[]
        {
            "code",
            "description",
            "id",
            "name"
        };

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void CardSet_RecognizesGeneratedPrimaryKey()
    {
        var map = new EntityMap<CardSet>();

        Assert.AreEqual("id", map.PrimaryKey.Name);
        Assert.IsTrue(map.PrimaryKey.IsPrimaryKey);
        Assert.IsTrue(map.PrimaryKey.IsGenerated);
    }

    [TestMethod]
    public void InsertColumns_ExcludeGeneratedId()
    {
        var map = new EntityMap<CardSet>();

        Assert.IsFalse(
            map.InsertColumns.Any(column => column.Name == "id"));

        Assert.AreEqual(3, map.InsertColumns.Count);
    }

    [TestMethod]
    public void UpdateColumns_ExcludePrimaryKey()
    {
        var map = new EntityMap<CardSet>();

        Assert.IsFalse(
            map.UpdateColumns.Any(column => column.IsPrimaryKey));

        Assert.AreEqual(3, map.UpdateColumns.Count);
    }

    [TestMethod]
    public void MissingTableAttribute_Throws()
    {
        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            _ = new EntityMap<WithoutTable>();
        });
    }

    [TestMethod]
    public void MissingPrimaryKey_Throws()
    {
        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            _ = new EntityMap<WithoutKey>();
        });
    }

    [TestMethod]
    public void DuplicateColumnNames_Throw()
    {
        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            _ = new EntityMap<DuplicateColumns>();
        });
    }

    [TestMethod]
    public void NonIntegerPrimaryKey_Throws()
    {
        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            _ = new EntityMap<StringKey>();
        });
    }

    public sealed class WithoutTable
    {
        [Column("id", IsPrimaryKey = true)]
        public int Id { get; set; }
    }

    [Table("without_key")]
    public sealed class WithoutKey
    {
        [Column("name")]
        public string Name { get; set; } = "";
    }

    [Table("duplicate_columns")]
    public sealed class DuplicateColumns
    {
        [Column("id", IsPrimaryKey = true)]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; } = "";

        [Column("name")]
        public string AnotherName { get; set; } = "";
    }

    [Table("string_key")]
    public sealed class StringKey
    {
        [Column("id", IsPrimaryKey = true)]
        public string Id { get; set; } = "";
    }
    
    [TestMethod]
    public void Cache_ReturnsSameInstanceForSameType()
    {
        var first = EntityMapCache.Get<CardSet>();
        var second = EntityMapCache.Get<CardSet>();

        Assert.AreSame(first, second);
    }
}