using System.Text.Json;
using System.Text.RegularExpressions;

namespace ServiceStars.Server;

// The client sends monotonic counters from a persistent stream, never deltas.
// Replays, lost responses and out-of-order requests therefore cannot add kills twice.
public class SyncRequest
{
    public int Protocol { get; set; } = 1;
    public string StreamId { get; set; } = "";
    public Dictionary<string, long> Counts { get; set; } = new();
}

public sealed class SyncReply
{
    public int Protocol { get; set; } = 1;
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public int KillsPerStar { get; set; }
    public Dictionary<string, long> Totals { get; set; } = new();
    public Dictionary<string, long> Acknowledged { get; set; } = new();
}

public sealed class StarData
{
    public int Version { get; set; } = 1;
    public Dictionary<string, Dictionary<string, long>> Streams { get; set; } = new();
}

public sealed class StarStore(string root, int killsPerStar, Func<string, bool> isWeapon)
{
    private readonly object gate = new();
    private static readonly Regex ProfileId = new("^[a-f0-9]{24}$", RegexOptions.Compiled);
    private static readonly Regex TemplateId = new("^[a-f0-9]{24}$", RegexOptions.Compiled);
    public SyncReply Sync(string profile, SyncRequest request)
    {
        if (!ProfileId.IsMatch(profile) || request.Protocol != 1 ||
            !Guid.TryParseExact(request.StreamId, "N", out _) || request.Counts == null || request.Counts.Count > 4096)
            throw new ArgumentException("Invalid service stars request.");
        foreach (var pair in request.Counts)
            if (!TemplateId.IsMatch(pair.Key) || pair.Value < 0 || pair.Value > 1_000_000_000L)
                throw new ArgumentException("Invalid weapon counter.");

        lock (gate)
        {
            Directory.CreateDirectory(root);
            string path = Path.Combine(root, profile + ".json");
            // A corrupt file must fail visibly, never silently reset a player's stars.
            var data = File.Exists(path)
                ? JsonSerializer.Deserialize<StarData>(File.ReadAllText(path)) ?? throw new InvalidDataException("Empty star data.")
                : new StarData();
            if (data.Version != 1 || data.Streams == null) throw new InvalidDataException("Unsupported star data.");
            if (!data.Streams.TryGetValue(request.StreamId, out var counters)) counters = new();
            bool changed = false;
            foreach (var pair in request.Counts)
            {
                long old = counters.GetValueOrDefault(pair.Key);
                // A modded weapon may have been removed temporarily. Keep it pending
                // instead of preventing every other weapon from synchronizing.
                if (pair.Value <= old || !isWeapon(pair.Key)) continue;
                counters[pair.Key] = pair.Value;
                changed = true;
            }
            if (changed)
            {
                data.Streams[request.StreamId] = counters;
                AtomicWrite(path, JsonSerializer.Serialize(data));
            }
            var totals = new Dictionary<string, long>();
            foreach (var stream in data.Streams.Values)
                foreach (var pair in stream)
                    totals[pair.Key] = checked(totals.GetValueOrDefault(pair.Key) + pair.Value);
            return new SyncReply { Ok = true, KillsPerStar = killsPerStar,
                Totals = totals, Acknowledged = new(counters) };
        }
    }

    private static void AtomicWrite(string path, string json)
    {
        string temp = path + ".tmp";
        using (var fs = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            using var writer = new StreamWriter(fs, leaveOpen: true);
            writer.Write(json);
            writer.Flush();
            fs.Flush(true);
        }
        if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
        else File.Move(temp, path);
    }
}
