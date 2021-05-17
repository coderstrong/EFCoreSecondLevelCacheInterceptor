using System;
using System.Data.Common;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace EFCoreSecondLevelCacheInterceptor
{
    /// <summary>
    /// Helps processing SecondLevelCacheInterceptor
    /// </summary>
    public class DbCommandInterceptorProcessor : IDbCommandInterceptorProcessor
    {
        private readonly IEFCacheServiceProvider _cacheService;
        private readonly IEFCacheDependenciesProcessor _cacheDependenciesProcessor;
        private readonly IEFCacheKeyProvider _cacheKeyProvider;
        private readonly IEFCachePolicyParser _cachePolicyParser;
        private readonly IEFDebugLogger _logger;
        private readonly IEFSqlCommandsProcessor _sqlCommandsProcessor;
        private readonly EFCoreSecondLevelCacheSettings _cacheSettings;

        /// <summary>
        /// Helps processing SecondLevelCacheInterceptor
        /// </summary>
        public DbCommandInterceptorProcessor(
            IEFDebugLogger logger,
            IEFCacheServiceProvider cacheService,
            IEFCacheDependenciesProcessor cacheDependenciesProcessor,
            IEFCacheKeyProvider cacheKeyProvider,
            IEFCachePolicyParser cachePolicyParser,
            IEFSqlCommandsProcessor sqlCommandsProcessor,
            IOptions<EFCoreSecondLevelCacheSettings> cacheSettings)
        {
            _cacheService = cacheService;
            _cacheDependenciesProcessor = cacheDependenciesProcessor;
            _cacheKeyProvider = cacheKeyProvider;
            _cachePolicyParser = cachePolicyParser;
            _logger = logger;
            _sqlCommandsProcessor = sqlCommandsProcessor;

            if (cacheSettings == null)
            {
                throw new ArgumentNullException(nameof(cacheSettings));
            }

            _cacheSettings = cacheSettings.Value;
        }

        /// <summary>
        /// Reads data from cache or cache it and then returns the result
        /// </summary>
        public T ProcessExecutedCommands<T>(DbCommand command, DbContext context, T result)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (result is EFTableRowsDataReader rowsReader)
            {
                _logger.LogDebug(CacheableEventId.CacheHit, $"Returning the cached TableRows[{rowsReader.TableName}].");
                return result;
            }

            var commandText = command.CommandText;
            var cachePolicy = getCachePolicy(context, commandText);
            var efCacheKey = _cacheKeyProvider.GetEFCacheKey(command, context, cachePolicy ?? new EFCachePolicy());
            if (_cacheDependenciesProcessor.InvalidateCacheDependencies(commandText, efCacheKey))
            {
                return result;
            }

            if (cachePolicy == null)
            {
                _logger.LogDebug($"Skipping a none-cachable command[{commandText}].");
                return result;
            }

            if (result is int data)
            {
                if (!shouldSkipCachingResults(commandText, data))
                {
                    _cacheService.InsertValue(efCacheKey, new EFCachedData { NonQuery = data }, cachePolicy);
                    _logger.LogDebug(CacheableEventId.QueryResultCached, $"[{data}] added to the cache[{efCacheKey}].");
                }
                return result;
            }

            if (result is DbDataReader dataReader)
            {
                EFTableRows tableRows;
                using (var dbReaderLoader = new EFDataReaderLoader(dataReader))
                {
                    tableRows = dbReaderLoader.LoadAndClose();
                }

                if (!shouldSkipCachingResults(commandText, tableRows))
                {
                    _cacheService.InsertValue(efCacheKey, new EFCachedData { TableRows = tableRows }, cachePolicy);
                    _logger.LogDebug(CacheableEventId.QueryResultCached, $"TableRows[{tableRows.TableName}] added to the cache[{efCacheKey}].");
                }
                return (T)(object)new EFTableRowsDataReader(tableRows);
            }

            if (result is object)
            {
                if (!shouldSkipCachingResults(commandText, result))
                {
                    _cacheService.InsertValue(efCacheKey, new EFCachedData { Scalar = result }, cachePolicy);
                    _logger.LogDebug(CacheableEventId.QueryResultCached, $"[{result}] added to the cache[{efCacheKey}].");
                }
                return result;
            }

            return result;
        }

        private EFCachePolicy? getCachePolicy(DbContext context, string commandText)
        {
            var allEntityTypes = _sqlCommandsProcessor.GetAllTableNames(context);
            return _cachePolicyParser.GetEFCachePolicy(commandText, allEntityTypes);
        }

        private bool shouldSkipCachingResults(string commandText, object value)
        {
            var result = _cacheSettings.SkipCachingResults != null && _cacheSettings.SkipCachingResults((commandText, value));
            if (result)
            {
                _logger.LogDebug("Skipped caching of this result based on the provided predicate.");
            }
            return result;
        }

        /// <summary>
        /// Reads command's data from the cache, if any.
        /// </summary>
        public T ProcessExecutingCommands<T>(DbCommand command, DbContext context, T result)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var commandText = command.CommandText;
            var cachePolicy = getCachePolicy(context, commandText);
            if (cachePolicy == null)
            {
                _logger.LogDebug($"Skipping a none-cachable command[{commandText}].");
                return result;
            }

            var efCacheKey = _cacheKeyProvider.GetEFCacheKey(command, context, cachePolicy);
            if (!(_cacheService.GetValue(efCacheKey, cachePolicy) is EFCachedData cacheResult))
            {
                _logger.LogDebug($"[{efCacheKey}] was not present in the cache.");
                return result;
            }

            if (result is InterceptionResult<DbDataReader>)
            {
                if (cacheResult.IsNull || cacheResult.TableRows == null)
                {
                    _logger.LogDebug("Suppressed the result with an empty TableRows.");
                    using var rows = new EFTableRowsDataReader(new EFTableRows());
                    return (T)Convert.ChangeType(
                            InterceptionResult<DbDataReader>.SuppressWithResult(rows),
                            typeof(T),
                            CultureInfo.InvariantCulture);
                }

                _logger.LogDebug($"Suppressed the result with the TableRows[{cacheResult.TableRows.TableName}] from the cache[{efCacheKey}].");
                using var dataRows = new EFTableRowsDataReader(cacheResult.TableRows);
                return (T)Convert.ChangeType(
                            InterceptionResult<DbDataReader>.SuppressWithResult(dataRows),
                            typeof(T),
                            CultureInfo.InvariantCulture);
            }

            if (result is InterceptionResult<int>)
            {
                int cachedResult = cacheResult.IsNull ? default : cacheResult.NonQuery;
                _logger.LogDebug($"Suppressed the result with {cachedResult} from the cache[{efCacheKey}].");
                return (T)Convert.ChangeType(
                            InterceptionResult<int>.SuppressWithResult(cachedResult),
                            typeof(T),
                            CultureInfo.InvariantCulture);
            }

            if (result is InterceptionResult<object>)
            {
                var cachedResult = cacheResult.IsNull ? default : cacheResult.Scalar;
                _logger.LogDebug($"Suppressed the result with {cachedResult} from the cache[{efCacheKey}].");
                return (T)Convert.ChangeType(
                            InterceptionResult<object>.SuppressWithResult(cachedResult ?? new object()),
                            typeof(T),
                            CultureInfo.InvariantCulture);
            }

            _logger.LogDebug($"Skipped the result with {result?.GetType()} type.");

            return result;
        }
    }
}