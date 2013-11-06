using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;
using System.Collections.Specialized;
using System.Web.UI;
using System.Linq;


namespace System.Web
{
    public enum JSONSerializationMode
    {
        /// <summary>
        /// Slower, but takes advantage of overriden ToJSON methods
        /// </summary>
        UseReflection = 1,
        /// <summary>
        /// Faster, but ignores overriden ToJSON methods
        /// </summary>
        NoReflection = 2,
        /// <summary>
        /// Not strictly valid json, but is a vaild javascript object
        /// </summary>
        JavascriptObject = 4
    };
    /// <summary>
    /// This class encodes and decodes JSON strings.
    /// Spec. details, see http://www.json.org/
    /// </summary>
    public static class JSON
    {
        /// <summary>
        /// Converts the public Properties of any object to a JSON-encoded string. Uses reflection to take advantaget of overridden ToJSON methods
        /// </summary>
        /// <returns>JSON-encoded string</returns>
        public static string ToJSON(this object obj)
        {
            //Check for null value
            if (obj == null)
                return "null";

            //Call ToJSON method
            return obj.ToJSON(JSONSerializationMode.UseReflection);
        }

        public static string ToJSON(this object obj, JSONSerializationMode mode)
        {
            //Check for null value
            if (obj == null)
                return "null";

            //Check if reflection should be used
            if ((mode & JSONSerializationMode.UseReflection) == JSONSerializationMode.UseReflection)
            {
                //Use Reflection to get methode
                Type type = obj.GetType();
                MethodInfo[] methods = type.GetMethods();

                //Check for a ToJSON method
                methods = methods.Where(c => c.Name == "ToJSON" && c.ReturnType == typeof(string)).ToArray();
                MethodInfo method = methods.SingleOrDefault(c => c.DeclaringType == type && c.GetParameters().Length == 0);

                //If there is a ToJSON method, call it
                if (method != null)
                    return (string)method.Invoke(obj, new object[0]);
            }

            //Call Serialize method
            return JSON.Serialize(obj, mode);
        }

        /// <summary>
        /// Escape a string for JSON encoding
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string EscapeString(string obj)
        {
            return obj.Replace("\\", "\\\\").Replace("/", "\\/").Replace("\"", "\\\"")
                .Replace("\b", "\\b").Replace("\f", "\\f").Replace("\n", "\\n")
                .Replace("\r", "\\r").Replace("\t", "\\t");
        }

        /// <summary>
        /// Unescape a JSON-encoded string
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string UnescapeString(string obj)
        {
            //if there aren't any \\, then it doesn't need to be unescaped
            if (!obj.Contains("\\"))
                return obj;

            return obj.Replace("\\t", "\t").Replace("\\r", "\r").Replace("\\n", "\n")
                .Replace("\\f", "\f").Replace("\\b", "\b").Replace("\\\"", "\"")
                .Replace("\\/", "/").Replace("\\\\", "\\");

        }

        /// <summary>
        /// Turn an object into a JSON-Encoded string
        /// </summary>
        /// <param name="obj">The object that needs to be serialized</param>
        /// <returns></returns>
        public static string Serialize(object obj)
        {
            return Serialize(obj, JSONSerializationMode.UseReflection);
        }

        /// <summary>
        /// Turn an object into a JSON-Encoded string, optionally setting parameters
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="mode">JSONSerializationModes; can be |'d together</param>
        /// <returns></returns>
        public static string Serialize(object obj, JSONSerializationMode mode)
        {
            if (obj == null)
                return "null";

            Type type = obj.GetType();

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
                    result.Append(value.ToJSON(mode) + ",");
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
                    if((mode & JSONSerializationMode.JavascriptObject) == JSONSerializationMode.JavascriptObject)
                        result.Append("" + i.Key + ":");
                    else
                        result.Append("\"" + i.Key + "\":");
                    
                    result.Append(i.Value.ToJSON(mode));
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

                   if((mode & JSONSerializationMode.JavascriptObject) == JSONSerializationMode.JavascriptObject)
                        result.Append("" + key + ":");
                    else
                        result.Append("\"" + key + "\":");

                    result.Append(value.ToJSON(mode));
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
                    result.Append(i.Current.ToJSON(mode) + ",");
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

                    if((mode & JSONSerializationMode.JavascriptObject) == JSONSerializationMode.JavascriptObject)
                        result.Append("" + prop.Name + ":");
                    else
                        result.Append("\"" + prop.Name + "\":");

                    result.Append(prop.GetValue(obj, null).ToJSON(mode));
                    result.Append(",");
                }

                string json = result.ToString();

                if (json.EndsWith(","))
                    json = json.Substring(0, json.Length - 1);

                json += "}";

                return json;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prop"></param>
        /// <returns></returns>
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
            //Deserialize the json into a generic object
            object o = Deserialize(json);

            //Clear the Reflection caches
            PropertyCache.Clear();
            ConstructorCache.Clear();

            //Convert generic object to the specified Type
            return Deserialize(o, type);
        }

        /// <summary>
        /// Parses a JSON string into the specified object type
        /// </summary>
        /// <typeparam name="T">The object type you want your json string deserialized into</typeparam>
        /// <param name="json">The JSON-encoded string you want to deserialize</param>
        /// <returns>Object of type T</returns>
        public static T Deserialize<T>(string json)
        {
            return (T)Deserialize(json, typeof(T));
        }

        #region Reflection Cache Variables
        //The following 3 variables are used to mitigate Reflection's performance effects
        private static Hashtable PropertyCache = new Hashtable(); 
        private static Hashtable ConstructorCache = new Hashtable();        
        #endregion

        private static object Deserialize(object obj, Type type)
        {
            if (obj == null)
                return null;

            Type t = obj.GetType();

            if (t == typeof(ArrayList))
            {
                if (type.IsArray)
                    type = type.GetElementType();

                ArrayList elems = new ArrayList();

                foreach (object o in (ArrayList)obj)
                {
                    if (o != null)
                        elems.Add(Deserialize(o, type));
                }

                return elems.ToArray(type);
            }

            if (t == typeof(Hashtable))
            {
                Hashtable hash = (Hashtable)obj;
                PropertyInfo[] props;
                ConstructorInfo construct;

                if (PropertyCache.ContainsKey(type))
                {
                    props = (PropertyInfo[])PropertyCache[type];
                    construct = (ConstructorInfo)ConstructorCache[type];
                }
                else
                {
                    props = type.GetProperties();
                    PropertyCache[type] = props;

                    construct = type.GetConstructor(new Type[] { });
                    ConstructorCache[type] = construct;
                }

                object elem = construct.Invoke(new object[] { });

                foreach (PropertyInfo prop in props)
                {
                    if (!prop.CanWrite)
                        continue;

                    object temp = hash[prop.Name];

                    if (temp == null)
                        temp = hash[prop.Name.ToLower()];

                    if (temp == null)
                        temp = hash[char.ToLower(prop.Name[0]) + prop.Name.Substring(1)];

                    if (temp != null)
                        prop.SetValue(elem, Deserialize(temp, prop.PropertyType), null);
                }

                return elem;
            }

            if (obj is double)
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
                else if (type == typeof(byte))
                    return (byte)val;
                else
                    return val;
            }

            if (type == typeof(bool))
            {
                return (bool)obj;
            }

            if (type == typeof(string))
                return obj.ToString();

            if (type == typeof(DateTime))
            {
                DateTime outdt;

                if (DateTime.TryParse(obj.ToString(), out outdt))
                    return outdt;
                else
                    return new DateTime(1, 1, 1);
            }

            if (type == typeof(object))
                return obj;

            return null;
        }

        private static double outDouble = 0;

        /// <summary>
        /// Converts a JSON-Encoded string into a generic object
        /// </summary>
        /// <param name="json"></param>
        /// <returns>Either a Hashtable (json object) or an ArrayList (json array) in an object variable</returns>
        public static object Deserialize(string json)
        {
            JSONDeserializer jd = new JSONDeserializer(json);
            return jd.Deserialize();
        }

        /// <summary>
        /// 
        /// </summary>
        private class JSONTypeDeserializer
        {
            private string json;
            private Type type;
            private int index;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="json"></param>
            /// <param name="type"></param>
            public JSONTypeDeserializer(string json, Type type)
            {
                this.json = json;
                this.type = type;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public object Deserialize()
            {
                index = 0;

                if (type.IsArray)
                {
                    var t = type.GetElementType();
                    return ProcessArray(t);

                }
                else
                {
                    return ProcessHash(type);
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="type"></param>
            /// <returns></returns>
            private object ProcessValue(Type type)
            {
                SkipWhitespace();

                if (json[index] == '[')
                {
                    if (json == "[]")
                        return new object[0];

                    return ProcessArray(type);
                }

                if (json[index] == '{')
                {
                    if (json == "{}")
                    {
                        ConstructorInfo constructor = type.GetConstructor(new Type[0]);

                        if (constructor != null)
                            return constructor.Invoke(new object[0]);
                        else
                            return null;
                    }

                    return ProcessHash(type);
                }

                if (json[index] == '"')
                {
                    string val = UnescapeString(ProcessString());

                    DateTime outDate;

                    if (DateTime.TryParse(val, out outDate))
                    {
                        return outDate;
                    }

                    return val;
                }

                int startIndex = index;

                while (index < json.Length && json[index] != ',' && json[index] != '}' && json[index] != ']' && !Char.IsWhiteSpace(json[index]))
                    index++;

                string jval = json.Substring(startIndex, index - startIndex).Trim();

                if (jval == "true")
                    return true;

                if (jval == "false")
                    return false;

                if (jval == "null")
                    return null;

                if (type == typeof(int))
                    return int.Parse(jval);
                else if (type == typeof(short))
                    return short.Parse(jval);
                else if (type == typeof(decimal))
                    return decimal.Parse(jval);
                else if (type == typeof(long))
                    return long.Parse(jval);
                else if (type == typeof(double))
                    return double.Parse(jval);
                else if (type == typeof(float))
                    return float.Parse(jval);

                return null;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="type"></param>
            /// <returns></returns>
            private object[] ProcessArray(Type type)
            {
                index++;

                List<object> list = new List<object>();

                while (true)
                {
                    SkipWhitespace();

                    list.Add(ProcessValue(type));

                    if (!MoveNext())
                        break;
                }

                return list.ToArray();
            }

            /// <summary>
            /// Converts a JSON object to an object of type 'type'
            /// </summary>
            /// <param name="type"></param>
            /// <returns></returns>
            private object ProcessHash(Type type)
            {
                ConstructorInfo constructor = type.GetConstructor(new Type[0]);

                object obj;

                if (constructor != null)
                    obj = constructor.Invoke(new object[0]);
                else
                    return null;

                index++;

                PropertyInfo[] props = type.GetProperties();

                Type t;

                while (true)
                {
                    SkipWhitespace();

                    string key = ProcessHashKey().Trim();
                    SkipWhitespace();

                    PropertyInfo prop = props.FirstOrDefault(c => c.Name == key);

                    if (prop == null)
                        prop = props.FirstOrDefault(c => c.Name.ToLower() == key.ToLower());

                    if (prop == null)
                        t = typeof(object);
                    else
                        t = prop.PropertyType;

                    object value = ProcessValue(t);

                    prop.SetValue(obj, value, null);

                    if (!MoveNext())
                        break;
                }

                return obj;
            }

            /// <summary>
            /// Moves to the next token
            /// </summary>
            /// <returns>False if it's the end of the json data</returns>
            private bool MoveNext()
            {
                while (index < json.Length && json[index] != ',' &&
                    json[index] != ']' && json[index] != '}')
                    index++;

                if (index >= json.Length)
                    return false;

                if (json[index] == ']' || json[index] == '}')
                {
                    index++;
                    return false;
                }

                index++;

                SkipWhitespace();

                if (index >= json.Length)
                    return false;

                return true;
            }

            /// <summary>
            /// Skips whitespace
            /// </summary>
            private void SkipWhitespace()
            {
                while (index < json.Length && Char.IsWhiteSpace(json[index]))
                    index++;
            }

            /// <summary>
            /// Gets the key name for a json object
            /// </summary>
            /// <returns></returns>
            private string ProcessHashKey()
            {
                int startIndex = index;
                while (json[index++] != ':')
                    ;
                string result = json.Substring(startIndex, index - startIndex - 1).Trim();
                result = result.ChopStart("\"").ChopEnd("\"");
                return result;
            }

            /// <summary>
            /// Reads the next JSON string
            /// </summary>
            /// <returns></returns>
            private string ProcessString()
            {                
                StringBuilder result = new StringBuilder();

                while (true)
                {
                    index++;

                    while (json[index] != '"')
                    {
                        result.Append(json[index]);
                        index++;
                    }

                    int j = index - 1;
                    int count = 0;

                    while (json[j] == '\\')
                    {
                        j--;
                        count++;
                    }

                    //if there are an even number of backslashes, 
                    //then they are all just backslashes and they aren't escaping the quote
                    //otherwise, the quote is being escaped and we need to keep searching for the close quote
                    if (count % 2 == 0)
                        break;
                    else
                        result.Append(json[index]);
                }

                return result.ToString();
            }


        }

        /// <summary>
        /// 
        /// </summary>
        private class JSONDeserializer
        {
            private string json;
            private int index;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="JSON"></param>
            public JSONDeserializer(string JSON)
            {
                index = 0;
                json = JSON;
            }

            /// <summary>
            /// Starts the Deserialize process
            /// </summary>
            /// <returns></returns>
            public object Deserialize()
            {
                index = 0;
                SkipWhitespace();
                return ProcessValue();
            }

            /// <summary>
            /// Processes the next token
            /// </summary>
            /// <returns></returns>
            private object ProcessValue()
            {
                if (json[index] == '[')
                {
                    return ProcessArray();
                }

                if (json[index] == '{')
                {
                    return ProcessHash();
                }

                if (json[index] == '"')
                {
                    string val = UnescapeString(ProcessString());

                    //DateTime outDate;

                    //if (DateTime.TryParse(val, out outDate))
                    //{
                    //    return outDate;
                    //}

                    return val;
                }

                int startIndex = index;

                while (index < json.Length && json[index] != ',' && json[index] != '}' && json[index] != ']' && !Char.IsWhiteSpace(json[index]))
                    index++;

                string jval = json.Substring(startIndex, index - startIndex);

                if (jval == "true")
                    return true;

                if (jval == "false")
                    return false;

                if (jval == "null")
                    return null;

                if (double.TryParse(jval, out outDouble))
                    return outDouble;

                return null;
            }

            /// <summary>
            /// Processes the next JSON Array 
            /// </summary>
            /// <returns></returns>
            private ArrayList ProcessArray()
            {
                index++;

                ArrayList list = new ArrayList();

                SkipWhitespace();

                if(json[index] == ']')
                {
                    index++;
                    return list;
                }

                while (true)
                {

                    list.Add(ProcessValue());

                    if (!MoveNext())
                        break;
                }

                return list;
            }

            /// <summary>
            /// Processes a JSON object
            /// </summary>
            /// <returns></returns>
            private Hashtable ProcessHash()
            {
                index++;

                Hashtable hash = new Hashtable();

                SkipWhitespace();

                if (json[index] == '}')
                {
                    index++;
                    return hash;
                }

                while (true)
                {
                    string key = ProcessHashKey();

                    SkipWhitespace();

                    object value = ProcessValue();

                    hash.Add(key, value);

                    if (!MoveNext())
                        break;
                }

                return hash;
            }

            /// <summary>
            /// Moves to the next token
            /// </summary>
            /// <returns>false if it's the end of the json string</returns>
            private bool MoveNext()
            {
                while (index < json.Length && json[index] != ',' &&
                    json[index] != ']' && json[index] != '}')
                    index++;

                if (index >= json.Length)
                    return false;

                if (json[index] == ']' || json[index] == '}')
                {
                    index++;
                    return false;
                }

                index++;

                SkipWhitespaceEnd();

                if (index >= json.Length)
                    return false;

                return true;
            }

            /// <summary>
            /// Skips the next whitespace, checking for the end of file
            /// </summary>
            private void SkipWhitespaceEnd()
            {
                while (index < json.Length && Char.IsWhiteSpace(json[index]))
                    index++;
            }

            /// <summary>
            /// Skips the next whitespace, not expecting an end of file
            /// </summary>
            private void SkipWhitespace()
            {
                while (Char.IsWhiteSpace(json[index]))
                    index++;
            }

            /// <summary>
            /// Reads the next object key
            /// </summary>
            /// <returns></returns>
            private string ProcessHashKey()
            {
                int startIndex = index + 1;
                while (json[index++] != ':')
                    ;
                string result = json.Substring(startIndex, index - startIndex - 2).TrimEnd();
                return result;
            }

            /// <summary>
            /// Reads the next string
            /// </summary>
            /// <returns></returns>
            private string ProcessString()
            {
                int startIndex = index + 1;

                while (true)
                {
                    index++;

                    while (json[index] != '"')
                        index++;

                    int j = index - 1;
                    int count = 0;

                    while (json[j] == '\\')
                    {
                        j--;
                        count++;
                    }

                    //if there are an even number of backslashes, 
                    //then they are all just backslashes and they aren't escaping the quote
                    //otherwise, the quote is being escaped and we need to keep searching for the close quote
                    if (count % 2 == 0)
                        break;
                }

                return json.Substring(startIndex, index - startIndex);
            }

        }

        /// <summary>
        /// Helper method that chops the beginning of a string starting with X
        /// </summary>
        /// <param name="str">The string to chop</param>
        /// <param name="x">The value that needs to be chopped</param>
        /// <returns></returns>
        private static string ChopStart(this string str, string x)
        {
            if (str.StartsWith(x))
                return str.Substring(x.Length);

            return str;
        }

        /// <summary>
        /// Helper method that chops the beginning of a string starting with X
        /// </summary>
        /// <param name="str">The string to chop</param>
        /// <param name="x">The value that needs to be chopped</param>
        /// <returns></returns>
        private static string ChopEnd(this string str, string x)
        {
            if (str.EndsWith(x))
                return str.Substring(0, str.Length - x.Length);

            return str;
        }

        /// <summary>
        /// Checks if a type is numeric
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>true if the type is a numeric type</returns>
        private static bool IsNumeric(this Type type)
        {
            Type[] types = new Type[]{
                typeof(int), typeof(short), typeof(long),
                typeof(double), typeof(decimal), typeof(byte)};

            return types.Contains(type);
        }


    }
}
