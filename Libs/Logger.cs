using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GothicStarter
{
    /// <summary>
    /// Úroveň zalogované hlášky.
    /// </summary>
    public enum Level
    {
        /// <summary>
        /// Zpráva informativního charakteru.
        /// </summary>
        Info,
        /// <summary>
        /// Varování. Nemělo by mít zásadní dopad na běh programu.
        /// </summary>
        Warning,
        /// <summary>
        /// Chyba v aplikaci. Je potřeba řešit.
        /// </summary>
        Error,
        /// <summary>
        /// Kritická chyba, se kterou nemůže aplikace dále fungovat.
        /// </summary>
        Fatal
    }

    /// <summary>
    /// Jeden konkrétní log.
    /// </summary>
    [DebuggerDisplay("[{Level}] {Message}")]
    public class LogEntry
    {
        /// <summary>
        /// Časová značka, kdy log vzniknul.
        /// </summary>
        public DateTime TimeStamp { get; protected set; }

        /// <summary>
        /// Úroveň logu.
        /// </summary>
        public Level Level { get; protected set; }

        /// <summary>
        /// Zpráva logu.
        /// </summary>
        public string Message { get; protected set; }

        /// <summary>
        /// V jaké třídě se log vytvořil.
        /// </summary>
        public string ClassName { get; protected set; }

        /// <summary>
        /// V jaké metodě se log vytvořil.
        /// </summary>
        public string MethodName { get; protected set; }

        /// <summary>
        /// Vytvoří nový log.
        /// </summary>
        /// <param name="level">Úroveň logu.</param>
        /// <param name="message">Zpráva logu.</param>
        public LogEntry(Level level, string message)
        {
            TimeStamp = DateTime.Now;
            ClassName = string.Empty;
            MethodName = string.Empty;

            Level = level;
            Message = message;
        }

        /// <summary>
        /// Vytvoří nový log.
        /// </summary>
        /// <param name="level">Úroveň logu.</param>
        /// <param name="message">Zpráva logu.</param>
        /// <param name="className">V jaké třídě se log vytvořil.</param>
        /// <param name="methodName">V jaké metodě se log vytvořil.</param>
        public LogEntry(Level level, string message, string className, string methodName)
        {
            TimeStamp = DateTime.Now;
            ClassName = className;
            MethodName = methodName;

            Level = level;
            Message = message;
        }
    }

    /// <summary>
    /// Statická třída uchovávající veškeré logy aplikace.
    /// </summary>
    public static class Logger
    {
        #region Getters/Setters
        /// <summary>
        /// Vyvolá se při přidání nového logu.
        /// </summary>
        public static event EventHandler<LogEntry> OnNewLog;

        /// <summary>
        /// Obsahuje všechny logy.
        /// </summary>
        public static ObservableCollection<LogEntry> Logs { get; } = new ObservableCollection<LogEntry>();

        /// <summary>
        /// Všechny záznamy s informativním charakterem.
        /// </summary>
        public static IEnumerable<LogEntry> Infos => Logs.Where(log => log.Level == Level.Info);

        /// <summary>
        /// Všechny záznamy s varováním.
        /// </summary>
        public static IEnumerable<LogEntry> Warnings => Logs.Where(log => log.Level == Level.Warning);

        /// <summary>
        /// Všechny záznamy o chybách.
        /// </summary>
        public static IEnumerable<LogEntry> Errors => Logs.Where(log => log.Level == Level.Error);

        /// <summary>
        /// Všechny záznamy o kritických chybách.
        /// </summary>
        public static IEnumerable<LogEntry> Fatals => Logs.Where(log => log.Level == Level.Fatal);
        #endregion

        #region Public Methods
        /// <summary>
        /// Přidá do loggeru nový záznam s informativním charakterem.
        /// </summary>
        public static void AddInfo(string message, params object[] args) => AddNewLog(Level.Info, message, args);

        /// <summary>
        /// Přidá do loggeru nový záznam o varování.
        /// </summary>
        public static void AddWarning(string message, params object[] args) => AddNewLog(Level.Warning, message, args);

        /// <summary>
        /// Přidá do loggeru nový záznam o chybě.
        /// </summary>
        public static void AddError(string message, params object[] args) => AddNewLog(Level.Error, message, args);

        /// <summary>
        /// Přidá do loggeru nový záznam o kritické chybě.
        /// </summary>
        public static void AddFatal(string message, params object[] args) => AddNewLog(Level.Fatal, message, args);

        /// <summary>
        /// Uloží do textového souboru kompletní obsah loggeru. Text je kódovaný v UNICODE.
        /// </summary>
        /// <param name="filepath">Cesta k textovému souboru.</param>
        public static void SaveLogToTextFile(string filepath)
        {
            using (StreamWriter sw = new StreamWriter(filepath))
            {
                WriteLogs(sw);
            }
        }

        /// <summary>
        /// Uloží do CSV souboru kompletní obsah loggeru.
        /// </summary>
        /// <param name="filepath">Cesta k CSV souboru.</param>
        public static void SaveLogToCsvFile(string filepath)
        {
            using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.GetEncoding(1250)))
            {
                sw.WriteLine("\"Level\";\"Date\";\"Class\";\"Method\";\"Message\"");

                foreach (LogEntry log in Logs)
                {
                    switch (log.Level)
                    {
                        case Level.Info:    sw.WriteLine($"\"INFO\";\"{log.TimeStamp:yyyy-MM-dd hh:mm:ss}\";\"{log.ClassName}\";\"{log.MethodName}\";\"{log.Message}\""); break;
                        case Level.Warning: sw.WriteLine($"\"WARNING\";\"{log.TimeStamp:yyyy-MM-dd hh:mm:ss}\";\"{log.ClassName}\";\"{log.MethodName}\";\"{log.Message}\""); break;
                        case Level.Error:   sw.WriteLine($"\"ERROR\";\"{log.TimeStamp:yyyy-MM-dd hh:mm:ss}\";\"{log.ClassName}\";\"{log.MethodName}\";\"{log.Message}\""); break;
                        case Level.Fatal:   sw.WriteLine($"\"FATAL\";\"{log.TimeStamp:yyyy-MM-dd hh:mm:ss}\";\"{log.ClassName}\";\"{log.MethodName}\";\"{log.Message}\""); break;
                    }
                }
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Pomocná metoda, která zapíše do TextWriteru celý log.
        /// </summary>
        static void WriteLogs(TextWriter tw)
        {
            foreach (LogEntry log in Logs)
            {
                switch (log.Level)
                {
                    case Level.Info:    tw.WriteLine($"[ INFO    ] {log.TimeStamp:yyyy-MM-dd hh:mm:ss} {log.Message}"); break;
                    case Level.Warning: tw.WriteLine($"[ WARNING ] {log.TimeStamp:yyyy-MM-dd hh:mm:ss} {log.Message}"); break;
                    case Level.Error:   tw.WriteLine($"[ ERROR   ] {log.TimeStamp:yyyy-MM-dd hh:mm:ss} {log.Message}"); break;
                    case Level.Fatal:   tw.WriteLine($"[ FATAL   ] {log.TimeStamp:yyyy-MM-dd hh:mm:ss} {log.Message}"); break;
                }
            }
        }

        /// <summary>
        /// Přidání nového logu do kolekce.
        /// </summary>
        /// <param name="level">Úroveň logu. Viz <see cref="Level"/>.</param>
        /// <param name="message">Zpráva v logu.</param>
        /// <param name="args">Zpráva může obsahovat '{x}', za které se pak dosadí tyto argumenty. Podobně jako u <see cref="Console.WriteLine"/>.</param>
        static void AddNewLog(Level level, string message, params object[] args)
        {
            MethodBase method = new StackFrame(2).GetMethod();

            string methodName = method.Name;
            LogEntry newLog = new LogEntry(level, string.Format(message, args), method.DeclaringType.Name, method.Name);

            Logs.Add(newLog);

            OnNewLog?.Invoke(null, newLog);
        }
        #endregion
    }
}
