﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LaunchDarkly.Client;
using Newtonsoft.Json;

namespace LaunchDarkly.Common
{
    internal class LdValueSerializer : JsonConverter
    {
        internal static readonly LdValueSerializer Instance = new LdValueSerializer();
        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }
            if (!(value is LdValue jv))
            {
                throw new ArgumentException();
            }
            switch (jv.Type)
            {
                case LdValueType.Null:
                    writer.WriteNull();
                    break;
                case LdValueType.Bool:
                    writer.WriteValue(jv.AsBool);
                    break;
                case LdValueType.Number:
                    if (jv.IsInt)
                    {
                        writer.WriteValue(jv.AsInt);
                    }
                    else
                    {
                        writer.WriteValue(jv.AsFloat);
                    }
                    break;
                case LdValueType.String:
                    writer.WriteValue(jv.AsString);
                    break;
                case LdValueType.Array:
                    writer.WriteStartArray();
                    foreach (var v in jv.List)
                    {
                        WriteJson(writer, v, serializer);
                    }
                    writer.WriteEndArray();
                    break;
                case LdValueType.Object:
                    writer.WriteStartObject();
                    foreach (var kv in jv.Dictionary)
                    {
                        writer.WritePropertyName(kv.Key);
                        WriteJson(writer, kv.Value, serializer);
                    }
                    writer.WriteEndObject();
                    break;
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return ReadValue(reader, reader.TokenType);
        }

        private LdValue ReadValue(JsonReader reader, JsonToken token)
        {
            switch (token)
            {
                case JsonToken.None:
                    throw new JsonSerializationException();
                
                case JsonToken.Null:
                case JsonToken.Undefined:
                    return LdValue.Null;

                case JsonToken.Boolean:
                    return LdValue.Of((bool)reader.Value);

                case JsonToken.Float:
                case JsonToken.Integer:
                    switch (reader.Value)
                    {
                        case int n:
                            return LdValue.Of(n);
                        case long n:
                            return LdValue.Of(n);
                        case float n:
                            return LdValue.Of(n);
                        case double n:
                            return LdValue.Of(n);
                        default:
                            throw new JsonSerializationException();
                    }

                case JsonToken.String:
                    return LdValue.Of((string)reader.Value);

                case JsonToken.StartArray:
                    var ab = LdValue.BuildArray();
                    while (true)
                    {
                        reader.Read();
                        var nextToken = reader.TokenType;
                        if (nextToken == JsonToken.EndArray)
                        {
                            return ab.Build();
                        }
                        ab.Add(ReadValue(reader, nextToken));
                    }

                case JsonToken.StartObject:
                    var ob = LdValue.BuildObject();
                    while (true)
                    {
                        reader.Read();
                        var nextToken = reader.TokenType;
                        if (nextToken == JsonToken.EndObject)
                        {
                            return ob.Build();
                        }
                        if (nextToken != JsonToken.PropertyName)
                        {
                            throw new JsonSerializationException();
                        }
                        var key = reader.Value.ToString();
                        reader.Read();
                        ob.Add(key, ReadValue(reader, reader.TokenType));
                    }

                default:
                    // all other token types are supposed to be used only for generating output, not returned
                    // by the parser
                    throw new JsonSerializationException();
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(LdValue);
        }
    }

    // This struct simply represents a list of T as a list of U, without doing any
    // copying, using a conversion function.
    internal struct LdValueListConverter<T, U> : IReadOnlyList<U>
    {
        private readonly IList<T> _source;
        private readonly Func<T, U> _converter;

        internal LdValueListConverter(IList<T> source, Func<T, U> converter)
        {
            _source = source;
            _converter = converter;
        }

        public U this[int index]
        {
            get
            {
                if (_source is null || index < 0 || index >= _source.Count)
                {
                    throw new IndexOutOfRangeException();
                }
                return _converter(_source[index]);
            }
        }

        public int Count => _source is null ? 0 : _source.Count;

        public IEnumerator<U> GetEnumerator() =>
            _source is null ? Enumerable.Empty<U>().GetEnumerator() :
                _source.Select<T, U>(_converter).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            return "[" + string.Join(",", this) + "]";
        }
    }

    // This struct simply represents a dictionary of <string, T> as a dictionary of
    // <string, U>, without doing any copying, using a conversion function.
    internal struct LdValueDictionaryConverter<T, U> : IReadOnlyDictionary<string, U>
    {
        private readonly IDictionary<string, T> _source;
        private readonly Func<T, U> _converter;

        internal LdValueDictionaryConverter(IDictionary<string, T> source, Func<T, U> converter)
        {
            _source = source;
            _converter = converter;
        }

        public U this[string key]
        {
            get
            {
                // Note that JObject[key] does *not* throw a KeyNotFoundException, but we should
                if (_source is null || !_source.TryGetValue(key, out var v))
                {
                    throw new KeyNotFoundException();
                }
                return _converter(v);
            }
        }

        public IEnumerable<string> Keys =>
            _source is null ? Enumerable.Empty<string>() : _source.Keys;

        public IEnumerable<U> Values =>
            _source is null ? Enumerable.Empty<U>() :
                _source.Values.Select(_converter);

        public int Count => _source is null ? 0 : _source.Count;

        public bool ContainsKey(string key) =>
            !(_source is null) && _source.TryGetValue(key, out var ignore);

        public IEnumerator<KeyValuePair<string, U>> GetEnumerator()
        {
            if (_source is null)
            {
                return Enumerable.Empty<KeyValuePair<string, U>>().GetEnumerator();
            }
            var conv = _converter; // lambda can't use instance field
            return _source.Select<KeyValuePair<string, T>, KeyValuePair<string, U>>(
                p => new KeyValuePair<string, U>(p.Key, conv(p.Value))
                ).GetEnumerator();
        }

        public bool TryGetValue(string key, out U value)
        {
            if (!(_source is null) && _source.TryGetValue(key, out var v))
            {
                value = _converter(v);
                return true;
            }
            value = default(U);
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            return "{" +
                string.Join(",", this.Select(kv => "\"" + kv.Key + "\":" + kv.Value)) +
                "}";
        }
    }
}
