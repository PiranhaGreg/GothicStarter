using GothicStarter.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GothicStarter.Data
{
    /// <summary>
    /// Správá INI souboru.
    /// </summary>
    /// <remarks>
    /// Dokumentace formátu: https://en.wikipedia.org/wiki/INI_file
    /// </remarks>
    public class IniFile : IEnumerable<KeyValuePair<string, string>>
    {
        StringComparer StringComparer => StringComparer.CurrentCultureIgnoreCase;

        Dictionary<string, Dictionary<string, string>> Values { get; } = new Dictionary<string, Dictionary<string, string>>(StringComparer.CurrentCultureIgnoreCase);

        /// <summary>
        /// Vytvoří prázdný INI soubor.
        /// </summary>
        public IniFile()
        {
            Values.Add(string.Empty, new Dictionary<string, string>(StringComparer));
        }

        /// <summary>
        /// Načte INI soubor ze zadané cesty.
        /// </summary>
        public IniFile(string filePath, Encoding encoding) : this(File.ReadAllText(filePath, encoding)) { }

        /// <summary>
        /// Rozparsuje INI soubor ze zadaného textu.
        /// </summary>
        public IniFile(string fileContent) : this()
        {
            string currentGroup = string.Empty;

            foreach (string line in fileContent.Split(Environment.NewLine))
            {
                // Prázdný řádek
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string tmp = line.Trim();

                // Komentář
                if (tmp.StartsWith(";"))
                    continue;

                // Skupina
                if (tmp.StartsWith("[") && tmp.EndsWith("]"))
                {
                    currentGroup = tmp.Substring(1, tmp.Length - 2).Trim();
                    Values[currentGroup] = new Dictionary<string, string>(StringComparer);
                    continue;
                }

                // Nový záznam
                string[] splitted = tmp.Split('=');

                if (splitted.Length != 2)
                {
                    Logger.AddWarning($"Nevalidní záznam: {line}");
                    continue;
                }

                Values[currentGroup][splitted[0].Trim()] = splitted[1].Trim();
            }
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => Values.SelectMany(group => group.Value).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Vrátí všechny parametry ze skupiny.
        /// </summary>
        /// <returns>Pokud nebyla skupina nalezena, vrátí prázdný enumerator.</returns>
        public IEnumerator<KeyValuePair<string, string>> GetGroupEnumerator(string group)
        {
            if (!Values.ContainsKey(group))
                return Enumerable.Empty<KeyValuePair<string, string>>().GetEnumerator();

            return Values[group].GetEnumerator();
        }

        /// <summary>
        /// Přístup k hodnotě parametru podle jeho názvu.
        /// Pokud existuje více parametrů se stejným názvem, volí se ten první.
        /// </summary>
        /// <returns>Vrátí <seealso cref="null"/>, pokud nebyla hodnota nalezena.</returns>
        public string this[string key]
        {
            get
            {
                foreach (Dictionary<string, string> group in Values.Values)
                {
                    if (group.ContainsKey(key))
                        return group[key];
                }

                return null;
            }
            set
            {
                foreach (Dictionary<string, string> group in Values.Values)
                {
                    if (!group.ContainsKey(key))
                        continue;

                    group[key] = value;
                    return;
                }

                Values[string.Empty][key] = value;
            }
        }
        
        /// <summary>
        /// Přístup k hodnotě podle názvu skupiny a názvu parametru.
        /// </summary>
        /// <returns>Vrátí <seealso cref="null"/>, pokud nebyla hodnota nalezena.</returns>
        public string this[string group, string key]
        {
            get
            {
                return Values.Get(group)?.Get(key);
            }
            set
            {
                if (!Values.ContainsKey(group))
                    Values[group] = new Dictionary<string, string>(StringComparer);

                Values[group][key] = value;
            }
        }

        /// <summary>
        /// Deserializuje informace uložené v INI formátu.
        /// </summary>
        public static T Deserialize<T>(string fileContent)
        {
            // TODO
            return default(T);
        }

        /// <summary>
        /// Deserializuje informace uložené v INI formátu.
        /// </summary>
        public static T Deserialize<T>(string filePath, Encoding encoding) => Deserialize<T>(File.ReadAllText(filePath, encoding));
    }
}
