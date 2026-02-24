using System.Collections.Concurrent;
using System.Linq.Dynamic.Core;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using MovieApi.Api.Models;

namespace Movies.Api.Common;

public static class QueryableExtensions
{
    // Cache allowed properties per type to avoid reflection every time
    private static readonly ConcurrentDictionary<Type, HashSet<string>> _propertyCache = new();

    public static IQueryable<T> ApplyPagination<T>(this IQueryable<T> query, int pageNumber, int pageSize)
    {
        pageNumber = Math.Max(pageNumber, 1);
        pageSize = Math.Max(pageSize, 1);

        return query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);
    }

    public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, string? sortBy) where T : class
    {
        if (string.IsNullOrWhiteSpace(sortBy))
            return query;

        // Get allowed properties from cache
        var allowedProperties = _propertyCache.GetOrAdd(typeof(T), t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
             .Select(p => p.Name)
             .ToHashSet(StringComparer.OrdinalIgnoreCase));

        var sortExpressions = new List<string>();

        foreach (var part in sortBy.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var tokens = part.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0 || !allowedProperties.Contains(tokens[0]))
                continue;

            string propertyName = tokens[0];

            if (!allowedProperties.Contains(propertyName))
                throw new ArgumentException($"Invalid sort property: '{propertyName}' for type {typeof(T).Name}");

            var direction = tokens.Length > 1 && tokens[1].Equals("desc", StringComparison.OrdinalIgnoreCase)
                ? "descending"
                : "ascending";

            sortExpressions.Add($"{tokens[0]} {direction}");
        }

        return sortExpressions.Count > 0
            ? query.OrderBy(string.Join(", ", sortExpressions))
            : query;
    }

    public static IQueryable<Movie> ApplySearch(this IQueryable<Movie> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return query;

        string trimmedSearch = search.Trim();

        // PostgreSQL ILike (case-insensitive)
        if (query.Provider.GetType().Name.Contains("Npgsql"))
        {
            return query.Where(m =>
                EF.Functions.ILike(m.Title, $"%{trimmedSearch}%") ||
                EF.Functions.ILike(m.Genre, $"%{trimmedSearch}%"));
        }
        // MSSQL Like (case-insensitive)
        else if (query.Provider.GetType().Name.Contains("SqlServer"))
        {
            return query.Where(m =>
                EF.Functions.Like(m.Title, $"%{trimmedSearch}%") ||
                EF.Functions.Like(m.Genre, $"%{trimmedSearch}%"));
        }

        // Generic EF fallback (works for SQLite)
        trimmedSearch = trimmedSearch.ToLower();
        return query.Where(m =>
            m.Title.ToLower().Contains(trimmedSearch) ||
            m.Genre.ToLower().Contains(trimmedSearch));
    }
}
