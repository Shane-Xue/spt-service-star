using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace ServiceStars
{
    public static class ModLocale
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Strings = Load();
        private static Dictionary<string, Dictionary<string, string>> Load()
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ServiceStars.locales.json"))
            using (var reader = new StreamReader(stream))
                return JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(reader.ReadToEnd());
        }
        public static string Resolve(string culture)
        {
            string language = (culture ?? "").Trim().Replace('_', '-').ToLowerInvariant();
            return language == "ch" || language == "cn" || language == "zh" || language.StartsWith("zh-") ? "ch" : "en";
        }
        public static string Text(string culture, string key, params object[] values)
        {
            string value;
            if (!Strings[Resolve(culture)].TryGetValue(key, out value) && !Strings["en"].TryGetValue(key, out value)) value = key;
            return values.Length == 0 ? value : String.Format(CultureInfo.InvariantCulture, value, values);
        }
    }
}
