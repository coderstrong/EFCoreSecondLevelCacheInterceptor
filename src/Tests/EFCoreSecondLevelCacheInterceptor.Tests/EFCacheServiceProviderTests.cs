using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EFCoreSecondLevelCacheInterceptor.Tests
{
    [TestClass]
    public class EFCacheServiceProviderTests
    {
        [DataTestMethod]
        [DataRow(TestCacheProvider.BuiltInInMemory)]
        [DataRow(TestCacheProvider.CacheManagerCoreInMemory)]
        [DataRow(TestCacheProvider.CacheManagerCoreRedis)]
        [DataRow(TestCacheProvider.EasyCachingCoreInMemory)]
        [DataRow(TestCacheProvider.EasyCachingCoreRedis)]
        [DataRow(TestCacheProvider.EasyCachingCoreHybrid)]
        public virtual void TestCacheInvalidationWithTwoRoots(TestCacheProvider cacheProvider)
        {
            var cacheService = EFServiceProvider.GetCacheServiceProvider(cacheProvider);
            var efCachePolicy = new EFCachePolicy().Timeout(TimeSpan.FromMinutes(10)).ExpirationMode(CacheExpirationMode.Absolute);
            var key1 = new EFCacheKey(new HashSet<string> { "entity1.model", "entity2.model" })
            {
                KeyHash = "EF_key1"
            };
            cacheService.InsertValue(
                key1,
                new EFCachedData { Scalar = "value1" }, efCachePolicy);

            var key2 = new EFCacheKey(new HashSet<string> { "entity1.model", "entity2.model" })
            {
                KeyHash = "EF_key2"
            };
            cacheService.InsertValue(
                key2,
                new EFCachedData { Scalar = "value2" },
                efCachePolicy);


            var value1 = cacheService.GetValue(key1, efCachePolicy);
            Assert.IsNotNull(value1);

            var value2 = cacheService.GetValue(key2, efCachePolicy);
            Assert.IsNotNull(value2);

            cacheService.InvalidateCacheDependencies(new EFCacheKey(new HashSet<string> { "entity2.model" })
            {
                KeyHash = "EF_key1"
            });

            value1 = cacheService.GetValue(key1, efCachePolicy);
            Assert.IsNull(value1);

            value2 = cacheService.GetValue(key2, efCachePolicy);
            Assert.IsNull(value2);
        }

        [DataTestMethod]
        [DataRow(TestCacheProvider.BuiltInInMemory)]
        [DataRow(TestCacheProvider.CacheManagerCoreInMemory)]
        [DataRow(TestCacheProvider.CacheManagerCoreRedis)]
        [DataRow(TestCacheProvider.EasyCachingCoreInMemory)]
        [DataRow(TestCacheProvider.EasyCachingCoreRedis)]
        [DataRow(TestCacheProvider.EasyCachingCoreHybrid)]
        public virtual void TestCacheInvalidationWithOneRoot(TestCacheProvider cacheProvider)
        {
            var cacheService = EFServiceProvider.GetCacheServiceProvider(cacheProvider);
            var efCachePolicy = new EFCachePolicy().Timeout(TimeSpan.FromMinutes(10)).ExpirationMode(CacheExpirationMode.Absolute);
            var key1 = new EFCacheKey(new HashSet<string> { "entity1" })
            {
                KeyHash = "EF_key1"
            };
            cacheService.InsertValue(
                key1,
                new EFCachedData { Scalar = "value1" }, efCachePolicy);

            var key2 = new EFCacheKey(new HashSet<string> { "entity1" })
            {
                KeyHash = "EF_key2"
            };
            cacheService.InsertValue(
                key2,
                new EFCachedData { Scalar = "value2" },
                efCachePolicy);

            var value1 = cacheService.GetValue(key1, efCachePolicy);
            Assert.IsNotNull(value1);

            var value2 = cacheService.GetValue(key2, efCachePolicy);
            Assert.IsNotNull(value2);

            cacheService.InvalidateCacheDependencies(new EFCacheKey(new HashSet<string> { "entity1" })
            {
                KeyHash = "EF_key2"
            });

            value1 = cacheService.GetValue(key1, efCachePolicy);
            Assert.IsNull(value1);

            value2 = cacheService.GetValue(key2, efCachePolicy);
            Assert.IsNull(value2);
        }

        [DataTestMethod]
        [DataRow(TestCacheProvider.BuiltInInMemory)]
        [DataRow(TestCacheProvider.CacheManagerCoreInMemory)]
        [DataRow(TestCacheProvider.CacheManagerCoreRedis)]
        [DataRow(TestCacheProvider.EasyCachingCoreInMemory)]
        [DataRow(TestCacheProvider.EasyCachingCoreRedis)]
        [DataRow(TestCacheProvider.EasyCachingCoreHybrid)]
        public virtual void TestObjectCacheInvalidationWithOneRoot(TestCacheProvider cacheProvider)
        {
            var cacheService = EFServiceProvider.GetCacheServiceProvider(cacheProvider);
            var efCachePolicy = new EFCachePolicy().Timeout(TimeSpan.FromMinutes(10)).ExpirationMode(CacheExpirationMode.Absolute);
            const string rootCacheKey = "EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Entities.Product";

            cacheService.InvalidateCacheDependencies(new EFCacheKey(new HashSet<string> { rootCacheKey })
            {
                KeyHash = "EF_key1"
            });

            var key11888622 = new EFCacheKey(new HashSet<string> { rootCacheKey })
            {
                KeyHash = "11888622"
            };
            var val11888622 = cacheService.GetValue(key11888622, efCachePolicy);
            Assert.IsNull(val11888622);

            cacheService.InsertValue(
                key11888622,
                new EFCachedData { Scalar = "Test1" }, efCachePolicy);

            var key44513A63 = new EFCacheKey(new HashSet<string> { rootCacheKey })
            {
                KeyHash = "44513A63"
            };
            var val44513A63 = cacheService.GetValue(key44513A63, efCachePolicy);
            Assert.IsNull(val44513A63);

            cacheService.InsertValue(
                key44513A63,
                new EFCachedData { Scalar = "Test1" }, efCachePolicy);

            cacheService.InvalidateCacheDependencies(new EFCacheKey(new HashSet<string> { rootCacheKey })
            {
                KeyHash = "44513A63"
            });

            val11888622 = cacheService.GetValue(key11888622, efCachePolicy);
            Assert.IsNull(val11888622);

            val44513A63 = cacheService.GetValue(key44513A63, efCachePolicy);
            Assert.IsNull(val44513A63);
        }

        [DataTestMethod]
        [DataRow(TestCacheProvider.BuiltInInMemory)]
        [DataRow(TestCacheProvider.CacheManagerCoreInMemory)]
        [DataRow(TestCacheProvider.CacheManagerCoreRedis)]
        [DataRow(TestCacheProvider.EasyCachingCoreInMemory)]
        [DataRow(TestCacheProvider.EasyCachingCoreRedis)]
        [DataRow(TestCacheProvider.EasyCachingCoreHybrid)]
        public virtual void TestCacheInvalidationWithSimilarRoots(TestCacheProvider cacheProvider)
        {
            var cacheService = EFServiceProvider.GetCacheServiceProvider(cacheProvider);
            var efCachePolicy = new EFCachePolicy().Timeout(TimeSpan.FromMinutes(10)).ExpirationMode(CacheExpirationMode.Absolute);
            var key1 = new EFCacheKey(new HashSet<string> { "entity1", "entity2" })
            {
                KeyHash = "EF_key1"
            };
            cacheService.InsertValue(
                key1,
                new EFCachedData { Scalar = "value1" }, efCachePolicy);

            var key2 = new EFCacheKey(new HashSet<string> { "entity2" })
            {
                KeyHash = "EF_key2"
            };
            cacheService.InsertValue(
                key2,
                new EFCachedData { Scalar = "value2" },
                efCachePolicy);


            var value1 = cacheService.GetValue(key1, efCachePolicy);
            Assert.IsNotNull(value1);

            var value2 = cacheService.GetValue(key2, efCachePolicy);
            Assert.IsNotNull(value2);

            cacheService.InvalidateCacheDependencies(new EFCacheKey(new HashSet<string> { "entity2" })
            {
                KeyHash = "EF_key2"
            });

            value1 = cacheService.GetValue(key1, efCachePolicy);
            Assert.IsNull(value1);

            value2 = cacheService.GetValue(key2, efCachePolicy);
            Assert.IsNull(value2);
        }

        [DataTestMethod]
        [DataRow(TestCacheProvider.BuiltInInMemory)]
        [DataRow(TestCacheProvider.CacheManagerCoreInMemory)]
        [DataRow(TestCacheProvider.CacheManagerCoreRedis)]
        [DataRow(TestCacheProvider.EasyCachingCoreInMemory)]
        [DataRow(TestCacheProvider.EasyCachingCoreRedis)]
        [DataRow(TestCacheProvider.EasyCachingCoreHybrid)]
        public virtual void TestInsertingNullValues(TestCacheProvider cacheProvider)
        {
            var cacheService = EFServiceProvider.GetCacheServiceProvider(cacheProvider);
            var efCachePolicy = new EFCachePolicy().Timeout(TimeSpan.FromMinutes(10)).ExpirationMode(CacheExpirationMode.Absolute);
            var key1 = new EFCacheKey(new HashSet<string> { "entity1", "entity2" })
            {
                KeyHash = "EF_key1"
            };
            cacheService.InsertValue(
                key1,
                null, efCachePolicy);

            var value1 = cacheService.GetValue(key1, efCachePolicy);
            Assert.IsTrue(value1.IsNull, $"value1 is `{value1}`");
        }

        [DataTestMethod]
        [DataRow(TestCacheProvider.BuiltInInMemory)]
        [DataRow(TestCacheProvider.CacheManagerCoreInMemory)]
        [DataRow(TestCacheProvider.CacheManagerCoreRedis)]
        [DataRow(TestCacheProvider.EasyCachingCoreInMemory)]
        [DataRow(TestCacheProvider.EasyCachingCoreRedis)]
        [DataRow(TestCacheProvider.EasyCachingCoreHybrid)]
        public virtual void TestParallelInsertsAndRemoves(TestCacheProvider cacheProvider)
        {
            var cacheService = EFServiceProvider.GetCacheServiceProvider(cacheProvider);
            var efCachePolicy = new EFCachePolicy().Timeout(TimeSpan.FromMinutes(10)).ExpirationMode(CacheExpirationMode.Absolute);

            var tests = new List<Action>();

            for (var i = 0; i < 4000; i++)
            {
                var i1 = i;
                tests.Add(() =>
            cacheService.InsertValue(
                new EFCacheKey(new HashSet<string> { "entity1", "entity2" })
                {
                    KeyHash = $"EF_key{i1}"
                }, new EFCachedData { NonQuery = i1 }, efCachePolicy));
            }

            for (var i = 0; i < 400; i++)
            {
                if (i % 2 == 0)
                {
                    tests.Add(() => cacheService.InvalidateCacheDependencies(new EFCacheKey(new HashSet<string> { "entity1" })
                    {
                        KeyHash = $"EF_key{i}"
                    }));
                }
                else
                {
                    tests.Add(() => cacheService.InvalidateCacheDependencies(new EFCacheKey(new HashSet<string> { "entity2" })
                    {
                        KeyHash = $"EF_key{i}"
                    }));
                }
            }

            Parallel.Invoke(tests.OrderBy(a => RandomNumberProvider.Next()).ToArray());

            var value1 = cacheService.GetValue(new EFCacheKey(new HashSet<string> { "entity1", "entity2" })
            {
                KeyHash = "EF_key1"
            }, efCachePolicy);
            Assert.IsNull(value1);
        }
    }
}