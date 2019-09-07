﻿using Newtonsoft.Json;

namespace BuildNotifications.Core.Utilities
{
    internal class Serializer : ISerializer
    {
        public Serializer()
        {
            _settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
        }

        public string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, Formatting.Indented, _settings);
        }

        public T Deserialize<T>(string serialized)
        {
            return JsonConvert.DeserializeObject<T>(serialized, _settings);
        }

        private readonly JsonSerializerSettings _settings;
    }
}