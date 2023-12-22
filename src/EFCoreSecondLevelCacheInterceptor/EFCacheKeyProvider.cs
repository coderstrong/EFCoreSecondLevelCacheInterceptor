﻿using System;
using System.Collections;
using System.Data.Common;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EFCoreSecondLevelCacheInterceptor;

/// <summary>
///     A custom cache key provider for EF queries.
/// </summary>
public class EFCacheKeyProvider : IEFCacheKeyProvider
{
    private readonly IEFCacheDependenciesProcessor _cacheDependenciesProcessor;
    private readonly IEFCacheKeyPrefixProvider _cacheKeyPrefixProvider;
    private readonly IEFCachePolicyParser _cachePolicyParser;

    private readonly IEFHashProvider _hashProvider;
    private readonly ILogger<EFCacheKeyProvider> _keyProviderLogger;
    private readonly IEFDebugLogger _logger;

    /// <summary>
    ///     A custom cache key provider for EF queries.
    /// </summary>
    public EFCacheKeyProvider(IEFCacheDependenciesProcessor cacheDependenciesProcessor,
                              IEFCachePolicyParser cachePolicyParser,
                              IEFDebugLogger logger,
                              ILogger<EFCacheKeyProvider> keyProviderLogger,
                              IEFHashProvider hashProvider,
                              IEFCacheKeyPrefixProvider cacheKeyPrefixProvider)
    {
        _cacheDependenciesProcessor = cacheDependenciesProcessor;
        _logger = logger;
        _keyProviderLogger = keyProviderLogger;
        _cachePolicyParser = cachePolicyParser;
        _hashProvider = hashProvider ?? throw new ArgumentNullException(nameof(hashProvider));
        _cacheKeyPrefixProvider = cacheKeyPrefixProvider;
    }

    /// <summary>
    ///     Gets an EF query and returns its hashed key to store in the cache.
    /// </summary>
    /// <param name="command">The EF query.</param>
    /// <param name="context">DbContext is a combination of the Unit Of Work and Repository patterns.</param>
    /// <param name="cachePolicy">determines the Expiration time of the cache.</param>
    /// <returns>Information of the computed key of the input LINQ query.</returns>
    public EFCacheKey GetEFCacheKey(DbCommand command, DbContext context, EFCachePolicy cachePolicy)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (command is null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        if (cachePolicy is null)
        {
            throw new ArgumentNullException(nameof(cachePolicy));
        }

        var cacheKey = getCacheKey(command, cachePolicy.CacheSaltKey);
        var cacheKeyPrefix = _cacheKeyPrefixProvider.GetCacheKeyPrefix();
        var cacheKeyHash =
            !string.IsNullOrEmpty(cacheKeyPrefix)
                ? $"{cacheKeyPrefix}{_hashProvider.ComputeHash(cacheKey):X}"
                : $"{_hashProvider.ComputeHash(cacheKey):X}";
        var cacheDbContextType = context.GetType();
        var cacheDependencies = _cacheDependenciesProcessor.GetCacheDependencies(command, context, cachePolicy);

        if (_logger.IsLoggerEnabled)
        {
            _keyProviderLogger
                .LogDebug("KeyHash: {CacheKeyHash}, DbContext: {Name}, CacheDependencies: {Dependencies}.",
                          cacheKeyHash,
                          cacheDbContextType?.Name,
                          string.Join(", ", cacheDependencies));
        }

        return new EFCacheKey(cacheDependencies)
               {
                   KeyHash = cacheKeyHash,
                   DbContext = cacheDbContextType,
               };
    }

    private string getCacheKey(DbCommand command, string saltKey)
    {
        var cacheKey = new StringBuilder();
        cacheKey.AppendLine(_cachePolicyParser.RemoveEFCachePolicyTag(command.CommandText));

        cacheKey.AppendLine("ConnectionString").Append('=').Append(command.Connection?.ConnectionString);

        foreach (DbParameter? parameter in command.Parameters)
        {
            if (parameter == null)
            {
                continue;
            }

            cacheKey.Append(parameter.ParameterName)
                    .Append('=').Append(getParameterValue(parameter)).Append(',')
                    .Append("Size").Append('=').Append(parameter.Size).Append(',')
                    .Append("Precision").Append('=').Append(parameter.Precision).Append(',')
                    .Append("Scale").Append('=').Append(parameter.Scale).Append(',')
                    .Append("Direction").Append('=').Append(parameter.Direction).Append(',');
        }

        cacheKey.AppendLine("SaltKey").Append('=').Append(saltKey);
        return cacheKey.ToString().Trim();
    }

    private static string? getParameterValue(DbParameter parameter)
    {
        return parameter.Value switch
               {
                   DBNull => "null",
                   null => "null",
                   byte[] buffer => bytesToHex(buffer),
                   Array array => enumerableToString(array),
                   IEnumerable enumerable => enumerableToString(enumerable),
                   _ => Convert.ToString(parameter.Value, CultureInfo.InvariantCulture),
               };
    }

    private static string enumerableToString(IEnumerable array)
    {
        var sb = new StringBuilder();
        foreach (var item in array)
        {
            sb.Append(item).Append(' ');
        }

        return sb.ToString();
    }

    private static string bytesToHex(byte[] buffer)
    {
        var sb = new StringBuilder(buffer.Length * 2);
        foreach (var @byte in buffer)
        {
            sb.Append(@byte.ToString("X2", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }
}