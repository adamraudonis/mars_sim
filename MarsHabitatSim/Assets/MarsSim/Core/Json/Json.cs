using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MarsSim.Core.Json
{
    /// <summary>
    /// Minimal dependency-free JSON reader/writer so MarsSim.Core stays engine-agnostic
    /// (UnityEngine.JsonUtility is unavailable in a noEngineReferences assembly, and
    /// System.Text.Json is not shipped with Unity's .NET profile).
    /// Supports the full JSON spec minus exotic number formats; preserves object key order.
    /// </summary>
    public sealed class JsonValue : IEnumerable<KeyValuePair<string, JsonValue>>
    {
        public enum Kind { Null, Bool, Number, String, Array, Object }

        public Kind Type { get; private set; }
        private bool _bool;
        private double _number;
        private string _string;
        private List<JsonValue> _array;
        private List<string> _keys;                 // preserves insertion order
        private Dictionary<string, JsonValue> _object;

        public static JsonValue Null() => new JsonValue { Type = Kind.Null };
        public static JsonValue Of(bool b) => new JsonValue { Type = Kind.Bool, _bool = b };
        public static JsonValue Of(double n) => new JsonValue { Type = Kind.Number, _number = n };
        public static JsonValue Of(string s) => s == null ? Null() : new JsonValue { Type = Kind.String, _string = s };
        public static JsonValue NewArray() => new JsonValue { Type = Kind.Array, _array = new List<JsonValue>() };
        public static JsonValue NewObject() => new JsonValue
        {
            Type = Kind.Object,
            _keys = new List<string>(),
            _object = new Dictionary<string, JsonValue>()
        };

        public bool IsNull => Type == Kind.Null;
        public bool AsBool(bool fallback = false) => Type == Kind.Bool ? _bool : fallback;
        public double AsDouble(double fallback = 0) => Type == Kind.Number ? _number : fallback;
        public int AsInt(int fallback = 0) => Type == Kind.Number ? (int)Math.Round(_number) : fallback;
        public string AsString(string fallback = null) => Type == Kind.String ? _string : fallback;

        public int Count => Type == Kind.Array ? _array.Count : Type == Kind.Object ? _keys.Count : 0;
        public IReadOnlyList<JsonValue> Items => _array ?? (IReadOnlyList<JsonValue>)Array.Empty<JsonValue>();
        public IReadOnlyList<string> Keys => _keys ?? (IReadOnlyList<string>)Array.Empty<string>();

        public JsonValue this[int i] => Type == Kind.Array && i >= 0 && i < _array.Count ? _array[i] : Null();

        public JsonValue this[string key]
        {
            get => Type == Kind.Object && _object.TryGetValue(key, out var v) ? v : Null();
            set
            {
                if (Type != Kind.Object) throw new InvalidOperationException("not an object");
                if (!_object.ContainsKey(key)) _keys.Add(key);
                _object[key] = value ?? Null();
            }
        }

        public bool Has(string key) => Type == Kind.Object && _object.ContainsKey(key);

        public JsonValue Add(JsonValue v)
        {
            if (Type != Kind.Array) throw new InvalidOperationException("not an array");
            _array.Add(v ?? Null());
            return this;
        }

        public IEnumerator<KeyValuePair<string, JsonValue>> GetEnumerator()
        {
            if (Type == Kind.Object)
                foreach (var k in _keys) yield return new KeyValuePair<string, JsonValue>(k, _object[k]);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // ---------------- Parsing ----------------

        public static JsonValue Parse(string text)
        {
            int pos = 0;
            var v = ParseValue(text, ref pos);
            SkipWs(text, ref pos);
            if (pos != text.Length) throw new FormatException($"JSON: trailing content at {pos}");
            return v;
        }

        public static bool TryParse(string text, out JsonValue value, out string error)
        {
            try { value = Parse(text); error = null; return true; }
            catch (Exception e) { value = Null(); error = e.Message; return false; }
        }

        private static void SkipWs(string s, ref int p)
        {
            while (p < s.Length && (s[p] == ' ' || s[p] == '\t' || s[p] == '\n' || s[p] == '\r')) p++;
        }

        private static JsonValue ParseValue(string s, ref int p)
        {
            SkipWs(s, ref p);
            if (p >= s.Length) throw new FormatException("JSON: unexpected end");
            char c = s[p];
            switch (c)
            {
                case '{': return ParseObject(s, ref p);
                case '[': return ParseArray(s, ref p);
                case '"': return Of(ParseString(s, ref p));
                case 't': Expect(s, ref p, "true"); return Of(true);
                case 'f': Expect(s, ref p, "false"); return Of(false);
                case 'n': Expect(s, ref p, "null"); return Null();
                default: return Of(ParseNumber(s, ref p));
            }
        }

        private static void Expect(string s, ref int p, string word)
        {
            if (p + word.Length > s.Length || string.CompareOrdinal(s, p, word, 0, word.Length) != 0)
                throw new FormatException($"JSON: expected '{word}' at {p}");
            p += word.Length;
        }

        private static JsonValue ParseObject(string s, ref int p)
        {
            var obj = NewObject();
            p++; // {
            SkipWs(s, ref p);
            if (p < s.Length && s[p] == '}') { p++; return obj; }
            while (true)
            {
                SkipWs(s, ref p);
                string key = ParseString(s, ref p);
                SkipWs(s, ref p);
                if (p >= s.Length || s[p] != ':') throw new FormatException($"JSON: expected ':' at {p}");
                p++;
                obj[key] = ParseValue(s, ref p);
                SkipWs(s, ref p);
                if (p >= s.Length) throw new FormatException("JSON: unterminated object");
                if (s[p] == ',') { p++; continue; }
                if (s[p] == '}') { p++; return obj; }
                throw new FormatException($"JSON: expected ',' or '}}' at {p}");
            }
        }

        private static JsonValue ParseArray(string s, ref int p)
        {
            var arr = NewArray();
            p++; // [
            SkipWs(s, ref p);
            if (p < s.Length && s[p] == ']') { p++; return arr; }
            while (true)
            {
                arr.Add(ParseValue(s, ref p));
                SkipWs(s, ref p);
                if (p >= s.Length) throw new FormatException("JSON: unterminated array");
                if (s[p] == ',') { p++; continue; }
                if (s[p] == ']') { p++; return arr; }
                throw new FormatException($"JSON: expected ',' or ']' at {p}");
            }
        }

        private static string ParseString(string s, ref int p)
        {
            if (s[p] != '"') throw new FormatException($"JSON: expected string at {p}");
            p++;
            var sb = new StringBuilder();
            while (p < s.Length)
            {
                char c = s[p++];
                if (c == '"') return sb.ToString();
                if (c == '\\')
                {
                    if (p >= s.Length) break;
                    char e = s[p++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (p + 4 > s.Length) throw new FormatException("JSON: bad \\u escape");
                            sb.Append((char)Convert.ToInt32(s.Substring(p, 4), 16));
                            p += 4;
                            break;
                        default: throw new FormatException($"JSON: bad escape '\\{e}'");
                    }
                }
                else sb.Append(c);
            }
            throw new FormatException("JSON: unterminated string");
        }

        private static double ParseNumber(string s, ref int p)
        {
            int start = p;
            if (p < s.Length && (s[p] == '-' || s[p] == '+')) p++;
            while (p < s.Length && (char.IsDigit(s[p]) || s[p] == '.' || s[p] == 'e' || s[p] == 'E' || s[p] == '+' || s[p] == '-'))
                p++;
            string tok = s.Substring(start, p - start);
            if (!double.TryParse(tok, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                throw new FormatException($"JSON: bad number '{tok}' at {start}");
            return d;
        }

        // ---------------- Writing ----------------

        public string Serialize(bool pretty = false)
        {
            var sb = new StringBuilder();
            Write(sb, pretty, 0);
            return sb.ToString();
        }

        private void Write(StringBuilder sb, bool pretty, int indent)
        {
            switch (Type)
            {
                case Kind.Null: sb.Append("null"); break;
                case Kind.Bool: sb.Append(_bool ? "true" : "false"); break;
                case Kind.Number:
                    sb.Append(double.IsFinite(_number)
                        ? _number.ToString("R", CultureInfo.InvariantCulture)
                        : "null");
                    break;
                case Kind.String: WriteEscaped(sb, _string); break;
                case Kind.Array:
                    sb.Append('[');
                    for (int i = 0; i < _array.Count; i++)
                    {
                        if (i > 0) sb.Append(',');
                        if (pretty) { sb.Append('\n'); Indent(sb, indent + 1); }
                        _array[i].Write(sb, pretty, indent + 1);
                    }
                    if (pretty && _array.Count > 0) { sb.Append('\n'); Indent(sb, indent); }
                    sb.Append(']');
                    break;
                case Kind.Object:
                    sb.Append('{');
                    for (int i = 0; i < _keys.Count; i++)
                    {
                        if (i > 0) sb.Append(',');
                        if (pretty) { sb.Append('\n'); Indent(sb, indent + 1); }
                        WriteEscaped(sb, _keys[i]);
                        sb.Append(pretty ? ": " : ":");
                        _object[_keys[i]].Write(sb, pretty, indent + 1);
                    }
                    if (pretty && _keys.Count > 0) { sb.Append('\n'); Indent(sb, indent); }
                    sb.Append('}');
                    break;
            }
        }

        private static void Indent(StringBuilder sb, int n) => sb.Append(' ', n * 2);

        private static void WriteEscaped(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
