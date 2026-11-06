using System.Text.Json;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;

namespace ServiceStars.Server;

public sealed record Metadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.shane-xue.service-stars";
    public string Name { get; init; } = "Service Stars";
    public string Author { get; init; } = "Local";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("1.2.1");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.6");
    public bool HasPrepatcher { get; init; }
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; }
    public string License { get; init; } = "MIT";
}

public sealed class StarRequest : SyncRequest, IRequestData { }

public sealed class StarConfig
{
    public int KillsPerStar { get; set; } = 100;
}

[Injectable(InjectionType.Singleton)]
public sealed class StarService
{
    private readonly SaveServer saves;
    private readonly StarStore store;
    public StarService(SaveServer saves, ItemHelper items)
    {
        this.saves = saves;
        string configPath = Path.Combine(Path.GetDirectoryName(typeof(StarService).Assembly.Location)!, "config.json");
        var config = File.Exists(configPath)
            ? JsonSerializer.Deserialize<StarConfig>(File.ReadAllText(configPath)) ?? throw new InvalidDataException("Invalid ServiceStars config.")
            : new StarConfig();
        if (config.KillsPerStar < 1 || config.KillsPerStar > 100000)
            throw new InvalidDataException("ServiceStars KillsPerStar must be 1..100000.");
        store = new StarStore(Path.Combine(AppContext.BaseDirectory, "user", "profileData", "ServiceStars"),
            config.KillsPerStar, id => WeaponCategories.Accepts(id, items.IsOfBaseclass));
    }
    public ValueTask<string> Sync(string url, StarRequest request, MongoId session, string? output, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // Identity comes exclusively from SPT's session header; no client profile selector.
        if (saves.GetProfile(session) == null) return ValueTask.FromResult(JsonSerializer.Serialize(
            new SyncReply { Error = "Unknown SPT profile." }));
        try { return ValueTask.FromResult(JsonSerializer.Serialize(store.Sync(session.ToString(), request))); }
        catch (Exception ex) when (ex is ArgumentException || ex is IOException || ex is JsonException || ex is InvalidDataException)
        {
            Console.Error.WriteLine("[ServiceStars] Synchronization failed: " + ex.Message);
            return ValueTask.FromResult(JsonSerializer.Serialize(new SyncReply { Error = "Service stars could not be saved. See server log." }));
        }
    }
}

[Injectable(InjectionType.Singleton)]
public sealed class StarRouter(JsonUtil json, StarService stars) : StaticRouter(json,
    [new RouteAction<StarRequest>("/service-stars/sync", stars.Sync)]);
