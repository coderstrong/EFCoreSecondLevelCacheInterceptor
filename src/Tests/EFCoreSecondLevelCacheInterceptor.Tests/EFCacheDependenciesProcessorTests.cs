using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EFCoreSecondLevelCacheInterceptor.Tests
{
    [TestClass]
    public class EFCacheDependenciesProcessorTests
    {
        private const string CacheKeyPrefix = "EF_";

        [TestMethod]
        public void TestGetCacheDependenciesWorksWithNormalEFQueries()
        {
            const string commandText = @"-- EFCachePolicy[TestCachingByteArrays(line 391)] --> Absolute|00:45:00|||False

      SELECT TOP(1) [u].[Id], [u].[AddDate], [u].[ByteArrayValue], [u].[ByteValue], [u].[CharValue], [u].[DateTimeOffsetValue], [u].[DecimalValue], [u].[DoubleValue], [u].[FloatValue], [u].[GuidValue], [u].[ImageData], [u].[IsActive], [u].[Name], [u].[Points], [u].[ShortValue], [u].[TimeSpanValue], [u].[UintValue], [u].[UlongValue], [u].[UpdateDate], [u].[UserStatus], [u].[UshortValue]
      FROM [Users] AS [u]
      WHERE [u].[Id] = @__user1_Id_0]";
            var cacheDependenciesProcessor = EFServiceProvider.GetRequiredService<IEFCacheDependenciesProcessor>();
            var cacheDependencies = cacheDependenciesProcessor.GetCacheDependencies(new EFCachePolicy(), new SortedSet<string> { "Posts", "Users", "Products" }, commandText);

            var inUseTableNames = new SortedSet<string> { CacheKeyPrefix + "Users" };
            CollectionAssert.AreEqual(expected: inUseTableNames, actual: cacheDependencies);
        }

        [TestMethod]
        public void TestGetCacheDependenciesWorks()
        {
            const string commandText = @"-- EFCachePolicy[Index(27)] --> Absolute|00:45:00

      SELECT TOP(1) [p].[Id], [p].[Title], [p].[UserId], [p].[post_type], [u].[Id], [u].[Name], [u].[UserStatus]
      FROM [Posts] AS [p]
      INNER JOIN [Users] AS [u] ON [p].[UserId] = [u].[Id]
      WHERE [p].[post_type] IN (N'post_base', N'post_page') AND ([p].[Id] > @__param1_0)
      ORDER BY [p].[Id]";
            var cacheDependenciesProcessor = EFServiceProvider.GetRequiredService<IEFCacheDependenciesProcessor>();
            var cacheDependencies = cacheDependenciesProcessor.GetCacheDependencies(new EFCachePolicy(), new SortedSet<string> { "Posts", "Users", "Products" }, commandText);

            var inUseTableNames = new SortedSet<string> { CacheKeyPrefix + "Posts", CacheKeyPrefix + "Users" };
            CollectionAssert.AreEqual(expected: inUseTableNames, actual: cacheDependencies);
        }

        [TestMethod]
        public void TestGetCacheDependenciesWorksWithSchemas()
        {
            const string commandText = @"-- EFCachePolicy[Index(27)] --> Absolute|00:45:00

      SELECT TOP(1) [p].[Id], [p].[Title], [p].[UserId], [p].[post_type], [u].[Id], [u].[Name], [u].[UserStatus]
      FROM dbo.[Posts] AS [p]
      INNER JOIN [dbo].[Users] AS [u] ON [p].[UserId] = [u].[Id]
      WHERE [p].[post_type] IN (N'post_base', N'post_page') AND ([p].[Id] > @__param1_0)
      ORDER BY [p].[Id]";
            var cacheDependenciesProcessor = EFServiceProvider.GetRequiredService<IEFCacheDependenciesProcessor>();
            var cacheDependencies = cacheDependenciesProcessor.GetCacheDependencies(new EFCachePolicy(), new SortedSet<string> { "Posts", "Users", "Products" }, commandText);

            var inUseTableNames = new SortedSet<string> { CacheKeyPrefix + "Posts", CacheKeyPrefix + "Users" };
            CollectionAssert.AreEqual(expected: inUseTableNames, actual: cacheDependencies);
        }


        [TestMethod]
        public void TestGetCacheDependenciesWorksWithASquareBracketInsideAStringValue()
        {
            const string commandText = @"-- EFCachePolicy[Index(27)] --> Absolute|00:45:00

      SELECT TOP(1) [p].[Id], [p].[Title], [p].[UserId], [p].[post_type], [u].[Id], [u].[Name], [u].[UserStatus]
      FROM [Posts] AS [p]
      INNER JOIN [Users] AS [u] ON [p].[UserId] = [u].[Id]
      WHERE [p].[post_type] IN (N'post_base', N'post_page') AND ([p].[Id] > @__param1_0) and [u].[Name]=' [Products] '
      ORDER BY [p].[Id]";
            var cacheDependenciesProcessor = EFServiceProvider.GetRequiredService<IEFCacheDependenciesProcessor>();
            var cacheDependencies = cacheDependenciesProcessor.GetCacheDependencies(new EFCachePolicy(), new SortedSet<string> { "Posts", "Users", "Products" }, commandText);

            var inUseTableNames = new SortedSet<string> { CacheKeyPrefix + "Posts", CacheKeyPrefix + "Users" };
            CollectionAssert.AreEqual(expected: inUseTableNames, actual: cacheDependencies);
        }

        [TestMethod]
        public void TestGetCacheDependenciesWorksWithAQuoteInsideAStringValue()
        {
            const string commandText = @"-- EFCachePolicy[Index(27)] --> Absolute|00:45:00

      SELECT TOP(1) [p].[Id], [p].[Title], [p].[UserId], [p].[post_type], [u].[Id], [u].[Name], [u].[UserStatus]
      FROM [Posts] AS [p]
      INNER JOIN [Users] AS [u] ON [p].[UserId] = [u].[Id]
      WHERE [p].[post_type] IN (N'post_base', N'post_page') AND ([p].[Id] > @__param1_0) and [u].[Name]=' ""Products"" '
      ORDER BY [p].[Id]";
            var cacheDependenciesProcessor = EFServiceProvider.GetRequiredService<IEFCacheDependenciesProcessor>();
            var cacheDependencies = cacheDependenciesProcessor.GetCacheDependencies(new EFCachePolicy(), new SortedSet<string> { "Posts", "Users", "Products" }, commandText);

            var inUseTableNames = new SortedSet<string> { CacheKeyPrefix + "Posts", CacheKeyPrefix + "Users" };
            CollectionAssert.AreEqual(expected: inUseTableNames, actual: cacheDependencies);
        }

        [TestMethod]
        public void TestGetCacheDependenciesWorksForInserts()
        {
            const string commandText = @"SET NOCOUNT ON;
        INSERT INTO [Products] ([IsActive], [Notes], [ProductName], [ProductNumber], [UserId])
        VALUES (@p0, @p1, @p2, @p3, @p4);
        SELECT [ProductId]
        FROM [Products]
        WHERE @@ROWCOUNT = 1 AND [ProductId] = scope_identity();";
            var cacheDependenciesProcessor = EFServiceProvider.GetRequiredService<IEFCacheDependenciesProcessor>();
            var cacheDependencies = cacheDependenciesProcessor.GetCacheDependencies(new EFCachePolicy(), new SortedSet<string> { "Posts", "Users", "Products" }, commandText);

            var inUseTableNames = new SortedSet<string> { CacheKeyPrefix + "Products" };
            CollectionAssert.AreEqual(expected: inUseTableNames, actual: cacheDependencies);
        }

        [TestMethod]
        public void TestGetCacheDependenciesWorksForInsertsWithBacktick()
        {
            const string commandText = @"SET NOCOUNT ON;
        INSERT INTO `Products` (`IsActive`, `Notes`, `ProductName`, `ProductNumber`, `UserId`)
        VALUES (@p0, @p1, @p2, @p3, @p4);
        SELECT `ProductId`
        FROM `Products`
        WHERE @@ROWCOUNT = 1 AND `ProductId` = scope_identity();";
            var cacheDependenciesProcessor = EFServiceProvider.GetRequiredService<IEFCacheDependenciesProcessor>();
            var cacheDependencies = cacheDependenciesProcessor.GetCacheDependencies(new EFCachePolicy(), new SortedSet<string> { "Posts", "Users", "Products" }, commandText);

            var inUseTableNames = new SortedSet<string> { CacheKeyPrefix + "Products" };
            CollectionAssert.AreEqual(expected: inUseTableNames, actual: cacheDependencies);
        }

        [TestMethod]
        public void TestGetCacheDependenciesWorksForDeletes()
        {
            const string commandText = @"SET NOCOUNT ON;
        DELETE FROM [Products]
        WHERE [ProductId] = @p0;
        SELECT @@ROWCOUNT;";
            var cacheDependenciesProcessor = EFServiceProvider.GetRequiredService<IEFCacheDependenciesProcessor>();
            var cacheDependencies = cacheDependenciesProcessor.GetCacheDependencies(new EFCachePolicy(), new SortedSet<string> { "Posts", "Users", "Products" }, commandText);

            var inUseTableNames = new SortedSet<string> { CacheKeyPrefix + "Products" };
            CollectionAssert.AreEqual(expected: inUseTableNames, actual: cacheDependencies);
        }

        [TestMethod]
        public void TestGetCacheDependenciesWorksForUpdates()
        {
            const string commandText = @"SET NOCOUNT ON;
      UPDATE [Users] SET [UserStatus] = @p0
      WHERE [Id] = @p1;
      SELECT @@ROWCOUNT;";
            var cacheDependenciesProcessor = EFServiceProvider.GetRequiredService<IEFCacheDependenciesProcessor>();
            var cacheDependencies = cacheDependenciesProcessor.GetCacheDependencies(new EFCachePolicy(), new SortedSet<string> { "Posts", "Users", "Products" }, commandText);

            var inUseTableNames = new SortedSet<string> { CacheKeyPrefix + "Users" };
            CollectionAssert.AreEqual(expected: inUseTableNames, actual: cacheDependencies);
        }
    }
}