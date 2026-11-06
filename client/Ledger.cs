using System;
using System.Collections.Generic;

namespace ServiceStars
{
    public sealed class ClientData
    {
        public int Version = 1;
        public string StreamId = Guid.NewGuid().ToString("N");
        public Dictionary<string, long> Counts = new Dictionary<string, long>();
        public Dictionary<string, long> Totals = new Dictionary<string, long>();
        public Dictionary<string, long> Acknowledged = new Dictionary<string, long>();
        public int KillsPerStar = 100;
    }
    public sealed class Reply
    {
        public int Protocol;
        public bool Ok;
        public string Error;
        public int KillsPerStar;
        public Dictionary<string, long> Totals;
        public Dictionary<string, long> Acknowledged;
    }
    public static class Ledger
    {
        public static long Count(Dictionary<string, long> values, string id)
        {
            long value;
            return values.TryGetValue(id, out value) ? value : 0;
        }
        public static long Total(ClientData data, string id)
        {
            return Count(data.Totals, id) + Math.Max(0, Count(data.Counts, id) - Count(data.Acknowledged, id));
        }
    }
}
