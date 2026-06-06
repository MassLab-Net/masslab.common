using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MassLab.Common.Caching.Memory;
using MassLab.Common.Caching.Memory.Configuration;

namespace MassLab.Common.Caching.Tests;

public class MemoryCachePropertyTests
{
    [Property(MaxTest = 100)]
    public bool Memory_cache_round_trips_string_values(FsCheck.NonEmptyString key, string value)
    {
        // Feature: common-caching-system, Property: cache set/get round-trips values.
        var cache = new MemoryCacheService(
            new MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()),
            Options.Create(new MassLab.Common.Caching.Memory.Configuration.MassLabMemoryCacheOptions()));
        var safeKey = $"property:{key.Get.GetHashCode():x}";

        cache.SetAsync(safeKey, value).GetAwaiter().GetResult();
        var actual = cache.GetAsync<string>(safeKey).GetAwaiter().GetResult();

        return actual == value;
    }
}
