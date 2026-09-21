using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Vagalume.Infra.Data.Configurations;

internal static class JsonList
{
    public static ValueConverter<List<T>, string> Converter<T>() => new(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
        v => JsonSerializer.Deserialize<List<T>>(v, (JsonSerializerOptions?)null) ?? new List<T>());

    public static ValueComparer<List<T>> Comparer<T>() => new(
        (a, b) => (a ?? new List<T>()).SequenceEqual(b ?? new List<T>()),
        v => v == null ? 0 : v.Aggregate(0, (acc, x) => HashCode.Combine(acc, x!.GetHashCode())),
        v => v == null ? new List<T>() : v.ToList());
}
