using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;
using System.Collections.Specialized;
using System.Web.UI;


namespace System.Web
{
    /// <summary>
    /// This class encodes and decodes JSON strings.
    /// Spec. details, see http://www.json.org/
    /// </summary>
    public static class JSON
    {
        /// <summary>
        /// Converts the public Properties of any object to a JSON-encoded string.
        /// </summary>
        /// <returns>JSON-encoded string</returns>
        public static string ToJSON(this object obj)
        {
            return JSON.Serialize(obj);
        }

        public static string EscapeString(string obj)
        {
            return obj.Replace("\\", "\\\\").Replace("/", "\\/").Replace("\"", "\\\"")
                .Replace("\b", "\\b").Replace("\f", "\\f").Replace("\n", "\\n")
                .Replace("\r", "\\r").Replace("\t", "\\t");
        }

        public static string Serialize(object obj)
        {
            if (obj == null)
                return "null";

            Type type = obj.GetType();

            //Console.Write(type);
            Debugger.Log(0, "SerializeType", type.ToString());


            if (type.IsNumeric())
            {
                return obj.ToString();
            }
            else if (type == typeof(string) || type == typeof(char))
            {
                return "\"" + EscapeString("" + obj) + "\"";
            }
            else if (type == typeof(bool))
            {
                bool x = (bool)obj;

                return x ? "true" : "false";
            }
            else if (type == typeof(DateTime))
            {
                DateTime dt = (DateTime)obj;

                return "\"" + dt.ToString("MM/dd/yyyy hh:mm:ss tt") + "\"";
            }

            StringBuilder result = new StringBuilder();


            if (obj.GetType().IsArray)
            {
                result.Append("[");

                Array array = (Array)obj;

                foreach (object value in array)
                {
                    result.Append(value.ToJSON() + ",");
                }

                string json = result.ToString();

                if (json.EndsWith(","))
                    json = json.Substring(0, json.Length - 1);

                json += "]";

                return json;
            }
            else if (obj is IDictionary)
            {
                IDictionary d = (IDictionary)obj;
                IDictionaryEnumerator i = d.GetEnumerator();

                result.Append("{");

                while (i.MoveNext())
                {
                    result.Append("\"" + i.Key + "\":");
                    result.Append(i.Value.ToJSON());
                    result.Append(",");
                }

                string json = result.ToString();

                if (json.EndsWith(","))
                    json = json.Substring(0, json.Length - 1);

                json += "}";

                return json;
            }
            else if (obj is NameValueCollection)
            {
                NameValueCollection col = (NameValueCollection)obj;

                result.Append("{");

                for (int i = 0; i < col.Count; i++)
                {
                    string key = col.Keys[i];
                    string value = col[key];

                    result.Append("\"" + key + "\":");
                    result.Append(value.ToJSON());
                    result.Append(",");
                }

                string json = result.ToString();

                if (json.EndsWith(","))
                    json = json.Substring(0, json.Length - 1);

                json += "}";

                return json;
            }
            else if (obj is IEnumerable)
            {
                IEnumerable item = (IEnumerable)obj;
                IEnumerator i = item.GetEnumerator();

                result.Append("[");

                while (i.MoveNext())
                {
                    result.Append(i.Current.ToJSON() + ",");
                }

                string json = result.ToString();

                if (json.EndsWith(","))
                    json = json.Substring(0, json.Length - 1);

                json += "]";

                return json;

            }
            else
            {
                result.Append("{");

                PropertyInfo[] props = obj.GetType().GetProperties();


                foreach (PropertyInfo prop in props)
                {
                    if (IgnoreProperty(prop))
                        continue;

                    object value = prop.GetValue(obj, null);

                    result.Append("\"" + prop.Name + "\":");
                    result.Append(prop.GetValue(obj, null).ToJSON());
                    result.Append(",");
                }

                string json = result.ToString();

                if (json.EndsWith(","))
                    json = json.Substring(0, json.Length - 1);

                json += "}";

                return json;
            }
        }

        private static bool IgnoreProperty(PropertyInfo prop)
        {
            if (prop.GetIndexParameters().Length > 0)
                return true;

            string Namespace = prop.PropertyType.Namespace;

            if (Namespace != null && (Namespace.StartsWith("System.Reflection") || Namespace.StartsWith("System.Security")))
                return true;

            Type[] ignoredTypes = new Type[] { typeof(Type), typeof(HtmlTextWriter), typeof(TextWriter), typeof(Stream) };

            foreach (Type type in ignoredTypes)
            {
                if (prop.PropertyType == type || prop.PropertyType.IsSubclassOf(type))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Parses a JSON string into the specified object type
        /// </summary>
        /// <param name="json">The JSON-encoded string you want to deserialize</param>
        /// <param name="type">The object type you want your json string deserialized into</param>
        /// <returns>Object of type Type</returns>
        public static object Deserialize(string json, Type type)
        {
            object o = Deserialize(json);

            return Deserialize(o, type);
        }

        private static object Deserialize(object obj, Type type)
        {
            Type t = obj.GetType();

            if (t == typeof(ArrayList))
            {
                if (type.IsArray)
                    type = type.GetElementType();

                ArrayList elems = new ArrayList();

                foreach (object o in (ArrayList)obj)
                {
                    elems.Add(Deserialize(o, type));
                }

                return elems.ToArray(type);
            }

            if (type.IsNumeric())
            {
                double val = (double)obj;

                if (type == typeof(int))
                    return (int)val;
                else if (type == typeof(short))
                    return (short)val;
                else if (type == typeof(decimal))
                    return (decimal)val;
                else if (type == typeof(long))
                    return (long)val;
                else if (type == typeof(double))
                    return (double)val;
                else if (type == typeof(float))
                    return (float)val;

                return val;
            }

            if (type == typeof(bool))
            {
                return (bool)obj;
            }

            if (type == typeof(string))
                return (string)obj;

            if (type == typeof(object))
                return obj;

            if (t == typeof(Hashtable))
            {
                Hashtable hash = (Hashtable)obj;
                PropertyInfo[] props = type.GetProperties();

                object elem = type.GetConstructor(new Type[] { }).Invoke(new object[] { });

                foreach (PropertyInfo prop in props)
                {
                    object temp = hash[prop.Name];

                    if (temp == null)
                        temp = hash[prop.Name.ToLower()];

                    if (temp == null)
                        temp = hash[char.ToLower(prop.Name[0]) + prop.Name.Substring(1)];

                    if (temp != null && prop.CanWrite)
                        prop.SetValue(elem, Deserialize(temp, prop.PropertyType), null);
                }

                return elem;
            }

            return null;
        }

        /// <summary>
        /// Deserialize a JSON string into a generic collection
        /// </summary>
        /// <param name="json"></param>
        /// <returns>Hashtable, Arraylist, string, double, or bool</returns>
        public static object Deserialize(string json)
        {
            json = json.Trim();

            if (json == "true")
                return true;

            if (json == "false")
                return false;

            if (json == "null")
                return null;

            string numRegex = @"-?\d+(\.\d+)?((e|E)(\+|-)?\d+)?";
            string strRegex = @"""((\\"")|[^""])*""";

            Regex reg_string = new Regex(@"^" + strRegex + "$", RegexOptions.Singleline);

            if (reg_string.IsMatch(json))
                return json.ChopStart("\"").ChopEnd("\"");

            Regex reg_numeric = new Regex("^" + numRegex + "$");

            if (reg_numeric.IsMatch(json))
                return double.Parse(json);

            Regex reg_keyval = new Regex(@"^(" + strRegex + @")\s*:\s*(.*)$", RegexOptions.Singleline);

            if (reg_keyval.IsMatch(json))
            {
                Match m = reg_keyval.Match(json);

                return new DictionaryEntry(m.Groups[1].Value.ChopStart("\"").ChopEnd("\""),
                    Deserialize(m.Groups[4].Value));
            }

            Regex reg_date = new Regex(@"^new Date\((.*?)\)$");

            if (reg_date.IsMatch(json))
            {
                Match m = reg_date.Match(json);

                return DateTime.Parse(m.Groups[1].Value.ChopStart("\"").ChopEnd("\""));
            }

            if (json.StartsWith("["))
            {
                return ProcessArray(GetMatchingTokenSubstring('[', ']', json));
            }

            if (json.StartsWith("{"))
            {
                Hashtable hash = new Hashtable();
                ArrayList items = ProcessArray(GetMatchingTokenSubstring('{', '}', json));

                foreach (object o in items)
                {
                    DictionaryEntry entry = (DictionaryEntry)o;
                    hash.Add(entry.Key, entry.Value);
                }

                return hash;
            }

            return null;
        }

        private static ArrayList ProcessArray(string json)
        {
            ArrayList list = new ArrayList();
            json = json.Trim();

            if (GetMatchingTokenSubstring('[', ']', json).Length == json.Length - 2)
                json = json.ChopStart("[").ChopEnd("]");

            int countA = 0; //[]
            int countB = 0; //{}
            bool inString = false;

            for (int i = 0; i < json.Length; i++)
            {
                if (json[i] == ',' && countA == 0 && countB == 0 && !inString)
                {
                    string item = json.Substring(0, i);
                    list.Add(Deserialize(item));
                    json = json.ChopStart(item).ChopStart(",");
                    i = -1;
                    continue;
                }

                if (!inString)
                {
                    switch (json[i])
                    {
                        case '[': countA++; break;
                        case ']': countA--; break;
                        case '{': countB++; break;
                        case '}': countB--; break;
                    }
                }

                if (json[i] == '"' && (i == 0 || json[i - 1] != '\\'))
                    inString = !inString;
            }

            list.Add(Deserialize(json));

            return list;
        }

        private static string GetMatchingTokenSubstring(char startToken, char endToken, string json)
        {
            if (!json.StartsWith("" + startToken))
                return json;

            bool inString = false;

            int found = 0;
            int i = 0;
            for (i = 0; i < json.Length; i++)
            {
                if (!inString)
                {
                    if (json[i] == startToken)
                        found++;
                    else if (json[i] == endToken)
                        found--;
                }

                if (json[i] == '"' && (i == 0 || json[i - 1] != '\\'))
                    inString = !inString;

                if (found == 0)
                    break;
            }

            if (found == 0)
                return json.Substring(1, i - 1);

            return "";

        }

    }
}
