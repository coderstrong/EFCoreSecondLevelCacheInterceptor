﻿using System;
using System.Collections.Generic;
using System.Linq;
using EFCoreSecondLevelCacheInterceptor;
using Issue12PostgreSql.DataLayer;
using Issue12PostgreSql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Issue12PostgreSql;

internal class Program
{
    private static void Main(string[] args)
    {
        initDb();

        EFServiceProvider.RunInContext(context =>
                                       {
                                           testLists(context);
                                           testArrays(context);

                                           var people = context.People.Include(x => x.Addresses).Include(x => x.Books)
                                                               .ToList();
                                           foreach (var person in people)
                                           {
                                               Console
                                                   .WriteLine($"{person.Id}, {person.Name}, {person.Addresses.First().Name}");
                                           }

                                           var cachedPeople = context.People.Include(x => x.Addresses)
                                                                     .Include(x => x.Books).Cacheable().ToList();
                                           cachedPeople = context.People.Include(x => x.Addresses).Include(x => x.Books)
                                                                 .Cacheable().ToList();
                                           foreach (var person in cachedPeople)
                                           {
                                               Console.WriteLine($"{person.Id}, {person.Name}");
                                           }

                                           cachedPeople = context.People.Include(x => x.Addresses).Include(x => x.Books)
                                                                 .Cacheable(CacheExpirationMode.Absolute,
                                                                            TimeSpan.FromMinutes(51)).ToList();
                                           cachedPeople = context.People.Include(x => x.Addresses).Include(x => x.Books)
                                                                 .Cacheable(CacheExpirationMode.Absolute,
                                                                            TimeSpan.FromMinutes(51)).ToList();
                                           foreach (var person in cachedPeople)
                                           {
                                               Console
                                                   .WriteLine($"{person.Id}, {person.Name}, {person.Addresses.First().Name}");
                                           }
                                       });
    }

    private static void testArrays(ApplicationDbContext context)
    {
        var firstQueryResult = queryArrays(context, new[] { 1, 3 });
        foreach (var entity in firstQueryResult)
        {
            Console.WriteLine($"firstArrayQueryResult -> Id: {entity.Id}");
        }

        var secondQueryResult = queryArrays(context, new[] { 4, 8, 9 });
        foreach (var entity in secondQueryResult)
        {
            Console.WriteLine($"secondArrayQueryResult -> Id: {entity.Id}");
        }
    }

    private static List<Entity> queryArrays(ApplicationDbContext dbContext, int[] array)
    {
        return dbContext.Entities.AsNoTracking()
                        .Where(entity => entity.Array.Any(x => array.Contains(x))).Cacheable().ToList();
    }

    private static void testLists(ApplicationDbContext context)
    {
        var firstQueryResult = queryLists(context, new List<string> { "1", "2" });
        foreach (var entity in firstQueryResult)
        {
            Console.WriteLine($"firstListQueryResult -> Id: {entity.Id}");
        }

        var secondQueryResult = queryLists(context, new List<string> { "3", "4" });
        foreach (var entity in secondQueryResult)
        {
            Console.WriteLine($"secondListQueryResult -> Id: {entity.Id}");
        }
    }

    private static List<Entity> queryLists(ApplicationDbContext dbContext, List<string> list)
    {
        return dbContext.Entities.AsNoTracking()
                        .Where(entity => entity.List.Any(x => list.Contains(x))).Cacheable().ToList();
    }

    private static void initDb()
    {
        EFServiceProvider.RunInContext(context =>
                                       {
                                           context.Database.Migrate();

                                           if (!context.People.Any())
                                           {
                                               var person1 = context.People.Add(new Person
                                                                                    {
                                                                                        Name = "Bill",
                                                                                        AddDate = DateTime.UtcNow,
                                                                                        UpdateDate = null,
                                                                                        Points = 1000,
                                                                                        IsActive = true,
                                                                                        ByteValue = 1,
                                                                                        CharValue = 'C',
                                                                                        DateTimeOffsetValue =
                                                                                            DateTimeOffset.UtcNow,
                                                                                        DecimalValue = 1.1M,
                                                                                        DoubleValue = 1.3,
                                                                                        FloatValue = 1.2f,
                                                                                        GuidValue = Guid.NewGuid(),
                                                                                        TimeSpanValue =
                                                                                            TimeSpan.FromDays(1),
                                                                                        ShortValue = 2,
                                                                                        ByteArrayValue =
                                                                                            new byte[] { 1, 2 },
                                                                                        UintValue = 1,
                                                                                        UlongValue = 1,
                                                                                        UshortValue = 1,
                                                                                        Date1 = new DateOnly(2021,
                                                                                             09,
                                                                                             23),
                                                                                        Date2 = null,
                                                                                        Time1 = TimeOnly.FromTimeSpan(
                                                                                             new DateTime(2021,
                                                                                                  09,
                                                                                                  23,
                                                                                                  19,
                                                                                                  2,
                                                                                                  0) -
                                                                                             new DateTime(2021,
                                                                                                  09,
                                                                                                  23,
                                                                                                  6,
                                                                                                  54,
                                                                                                  0)),
                                                                                        Time2 = null,
                                                                                    });

                                               context.Addresses.Add(new Address
                                                                     {
                                                                         Name = "Addr 1", Person = person1.Entity,
                                                                     });
                                               context.Books.Add(new Book { Name = "Book 1", Person = person1.Entity });

                                               var person2 = context.People.Add(new Person
                                                                                    {
                                                                                        Name = "Vahid",
                                                                                        AddDate = DateTime.UtcNow,
                                                                                        UpdateDate = null,
                                                                                        Points = 1000,
                                                                                        IsActive = true,
                                                                                        ByteValue = 1,
                                                                                        CharValue = 'C',
                                                                                        DateTimeOffsetValue =
                                                                                            DateTimeOffset.UtcNow,
                                                                                        DecimalValue = 1,
                                                                                        DoubleValue = 2,
                                                                                        FloatValue = 3,
                                                                                        GuidValue = Guid.NewGuid(),
                                                                                        TimeSpanValue =
                                                                                            TimeSpan.FromDays(1),
                                                                                        ShortValue = 2,
                                                                                        ByteArrayValue =
                                                                                            new byte[] { 1, 2 },
                                                                                        UintValue = 1,
                                                                                        UlongValue = 1,
                                                                                        UshortValue = 1,
                                                                                        OptionDefinitions =
                                                                                            new List<BlogOption>
                                                                                            {
                                                                                                new()
                                                                                                {
                                                                                                    IsActive = true,
                                                                                                    Name = "Test",
                                                                                                    NumberOfTimesUsed =
                                                                                                        1,
                                                                                                    SortOrder = 1,
                                                                                                },
                                                                                            },
                                                                                    });

                                               context.Addresses.Add(new Address
                                                                     {
                                                                         Name = "Addr 2", Person = person2.Entity,
                                                                     });
                                               context.Books.Add(new Book { Name = "Book 2", Person = person2.Entity });

                                               context.SaveChanges();
                                           }

                                           if (!context.Entities.Any())
                                           {
                                               var initialData = new[]
                                                                 {
                                                                     new Entity
                                                                     {
                                                                         Array = new[] { 1, 2, 3 },
                                                                         List = new List<string> { "1", "2" },
                                                                     },
                                                                     new Entity
                                                                     {
                                                                         Array = new[] { 4, 5, 6 },
                                                                         List = new List<string> { "3", "4" },
                                                                     },
                                                                     new Entity
                                                                     {
                                                                         Array = null,
                                                                         List = null,
                                                                     },
                                                                 };
                                               context.AddRange(initialData);

                                               context.SaveChanges();
                                           }
                                       });
    }
}