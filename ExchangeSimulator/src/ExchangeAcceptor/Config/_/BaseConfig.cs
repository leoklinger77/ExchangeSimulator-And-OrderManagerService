﻿namespace ExchangeAcceptor.Config {
    using Newtonsoft.Json;

    public class BaseConfig<T> where T : class {
        private static IDictionary<Type, object> _cache = new Dictionary<Type, object>();

        public static T Get(string name = null) {
            var type = typeof(T);
            if (_cache.TryGetValue(type, out var obj)) {
                return obj as T;
            }
            var nameJson = string.IsNullOrEmpty(name) ? type.Name : name;
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Config", $"{nameJson}.json");
            if (!File.Exists(path)) {
                throw new FileNotFoundException(path);
            }
            var file = File.ReadAllText(path);
            var deserialize = JsonConvert.DeserializeObject<T>(file);
            _cache.TryAdd(type, deserialize);
            return deserialize;
        }
    }
}
