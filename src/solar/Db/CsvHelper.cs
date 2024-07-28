﻿using System.Globalization;
using System.Reflection;

namespace Lucky.Home.Db;

/// <summary>
/// Serialize/deserialize from a csv file to C# objects.
/// </summary>
public static class CsvHelper<T> where T : class, new()
{
    private static readonly List<Tuple<PropertyInfo, string>> s_propertiesForWrite;
    private static readonly List<Tuple<PropertyInfo, string>> s_propertiesForParse;
    private static readonly string s_headerForWrite;

    private class TypeComparer : IComparer<Type>
    {
        public int Compare(Type x, Type y)
        {
            if (x == y)
            {
                return 0;
            }
            if (x.IsAssignableFrom(y))
            {
                return -1;
            }
            return 1;
        }
    }

    static CsvHelper()
    {
        var type = typeof(T);
        // List properties
        var propertiesForWrite = type.GetProperties(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance)
            .OrderBy(f => f.DeclaringType, new TypeComparer())
            .Where(f => f.GetCustomAttribute<CsvAttribute>() != null && f.GetCustomAttribute<CsvAttribute>().OnlyForParsing == false)
            .ToArray();
        var propertiesForRead = type.GetProperties(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance)
            .OrderBy(f => f.DeclaringType, new TypeComparer())
            .Where(f => f.GetCustomAttribute<CsvAttribute>() != null)
            .ToArray();
        s_headerForWrite = string.Join(",", propertiesForWrite.Select(pi => GetCsvFieldName(pi)));
        s_propertiesForWrite = propertiesForWrite.Select(fi => Tuple.Create(fi, fi.GetCustomAttribute<CsvAttribute>().Format)).ToList();
        s_propertiesForParse = propertiesForRead.Select(fi => Tuple.Create(fi, fi.GetCustomAttribute<CsvAttribute>().Format)).ToList();
    }

    private static string GetCsvFieldName(PropertyInfo propertyInfo)
    {
        var attr = propertyInfo.GetCustomAttribute<CsvAttribute>();
        return attr.Name ?? propertyInfo.Name;
    }

    private static string ToCsv(T value)
    {
        return string.Join(",", s_propertiesForWrite.Select(fi =>
        {
            var format = fi.Item2 != null ? ("{0:" + fi.Item2 + "}") : "{0}";
            return string.Format(CultureInfo.InvariantCulture, format, fi.Item1.GetValue(value));
        }));
    }

    public static int[] ParseHeader(string line)
    {
        if (!string.IsNullOrEmpty(line))
        {
            string[] parts = line.Split(',');
            return parts.Select(name => s_propertiesForParse.FindIndex(t => GetCsvFieldName(t.Item1) == name)).ToArray();
        }
        else
        {
            return null;
        }
    }

    public static T ParseLine(string line, int[] header)
    {
        if (!string.IsNullOrEmpty(line))
        {
            string[] parts = line.Split(',');
            if (parts.Length != header.Length)
            {
                return null;
            }

            // Special case: duplicate header
            if (parts.Zip(header, (a1, a2) => Tuple.Create(a1, a2)).All(t => t.Item2 < 0 || t.Item1 == GetCsvFieldName(s_propertiesForParse[t.Item2].Item1)))
            {
                return null;
            }

            T ret = new T();
            for (int i = 0; i < parts.Length; i++)
            {
                if (header[i] >= 0)
                {
                    var t = s_propertiesForParse[header[i]];
                    object value = null;
                    switch (Type.GetTypeCode(t.Item1.PropertyType)) 
                    {
                        case TypeCode.UInt16:
                            ushort u;
                            if (ushort.TryParse(parts[i], CultureInfo.InvariantCulture, out u))
                            {
                                value = u;
                            }
                            break;
                        case TypeCode.Int32:
                            int r;
                            if (int.TryParse(parts[i], CultureInfo.InvariantCulture, out r))
                            {
                                value = r;
                            }
                            break;
                        case TypeCode.Double:
                            double d;
                            if (double.TryParse(parts[i], CultureInfo.InvariantCulture, out d))
                            {
                                value = d;
                            }
                            break;
                        case TypeCode.String:
                            value = parts[i];
                            break;
                    }

                    if (t.Item1.PropertyType == typeof(DateTime))
                    {
                        DateTime dt;
                        if (DateTime.TryParseExact(parts[i], t.Item2, null, DateTimeStyles.None, out dt))
                        {
                            value = dt;
                        }
                    }
                    if (t.Item1.PropertyType == typeof(TimeSpan))
                    {
                        TimeSpan ts;
                        if (TimeSpan.TryParseExact(parts[i], t.Item2, null, out ts))
                        {
                            value = ts;
                        }
                    }

                    if (value != null)
                    {
                        t.Item1.SetValue(ret, value);
                    }
                }
            }
            return ret;
        }
        else
        {
            return null;
        }
    }

    private static void WriteCsvLine(FileInfo file, string line)
    {
        using (var stream = file.Open(FileMode.Append, FileAccess.Write, FileShare.Read))
        {
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine(line);
            }
        }
    }

    public static void WriteCsvHeader(FileInfo file)
    {
        WriteCsvLine(file, s_headerForWrite);
    }

    public static void WriteCsvLine(FileInfo file, T data)
    {
        WriteCsvLine(file, ToCsv(data));
    }

    public static IEnumerable<T> ReadCsv(FileInfo file)
    {
        using (var stream = file.OpenRead())
        {
            using (var reader = new StreamReader(stream))
            {
                // Read header
                string headerStr;
                int[] header = null;
                do
                {
                    headerStr = reader.ReadLine();
                    header = ParseHeader(headerStr);
                } while (headerStr != null && header == null);

                List<T> data = new List<T>();
                if (header != null)
                {
                    // Read data
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line == headerStr)
                        {
                            // Duplicated header after a restart, skip
                            continue;
                        }
                        var l = ParseLine(line, header);
                        if (l != null)
                        {
                            data.Add(l);
                        }
                    }
                }
                return data;
            }
        }
    }
}
