// GPL-2.0 License
// https://github.com/lusoris/jellyfin-plugin-lastfm

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Lastfm.Scrobbler.Core.Models.Requests
{
    public abstract class BaseRequest
    {
        [JsonIgnore]
        public abstract string Method { get; }
        public abstract Dictionary<string, string> ToDictionary();

        protected Dictionary<string, string> ToDictionary(object obj)
        {
            var dictionary = obj.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(prop => prop.Name != "Method" && prop.GetValue(obj, null) != null)
                .ToDictionary(prop => prop.Name.ToLower(), prop => prop.GetValue(obj, null)?.ToString() ?? string.Empty);

            dictionary.Add("method", Method);
            return dictionary;
        }
    }
}
