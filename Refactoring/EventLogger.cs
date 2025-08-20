using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Refactoring {
    public class EventLogger {
        public string FormatEvent(string category, string message, DateTime timestamp) {
            return $"{category}: {timestamp:yyyy-MM-dd HH:mm:ss} - {message}";
            //return category + " : " + $"{timestamp:yyyy-MM-dd HH:mm:ss}" + " - " + message;
        }

        public void LogEvents(string[] categories, string[] messages) {
            string log = "";
            for (int i = 0; i < categories.Length; i++) {
                log += FormatEvent(categories[i], messages[i], DateTime.Now) + Environment.NewLine;
            }
            File.AppendAllText("events.log", log);
        }
    }

    public class ApplicationLogger {
        private Mutex mutex = new Mutex(false, "Global\\ApplicationLog");
        private const string fileName = "ApplicationLog.txt";

        public void WriteStringBuilderToFile(StringBuilder sb) {
            bool acquired = mutex.WaitOne();
            try {
                using var stream = new FileStream(fileName, FileMode.Append);
                using var streamWriter = new StreamWriter(stream, Encoding.UTF8);
                streamWriter.Write(sb);
            } finally {
                if (acquired) mutex.ReleaseMutex();
            }
        }
    }

    public class EventLoggerOptimizedMutex {
        private readonly ApplicationLogger _applicationLogger;
        public EventLoggerOptimizedMutex(ApplicationLogger applicationLogger) {
            _applicationLogger = applicationLogger;
        }

        public void FormatEvent(string category, string message, DateTime timestamp, StringBuilder sb) {
            sb.AppendFormat("{0}: {1:yyyy-MM-dd HH:mm:ss} - {2}", category, timestamp, message);
        }

        private int CalculateResultStringLength(string[] categories, string[] messages) {
            int result = 0;
            for (int i = 0; i < categories.Length; ++i) {
                result += 26 + categories[i].Length + messages[i].Length;
            }
            return result;
        }

        private StringBuilder AggregateString(string[] categories, string[] messages) {
            if (categories.Length != messages.Length) {
                throw new ArgumentException($"{nameof(categories)} Length should be equal {nameof(messages)} Length");
            }
            StringBuilder sb = new(CalculateResultStringLength(categories, messages));
            for (int i = 0; i < categories.Length; i++) {
                FormatEvent(categories[i], messages[i], DateTime.Now, sb);
                sb.Append(Environment.NewLine);
            }
            return sb;
        }

        public void LogEvents(string[] categories, string[] messages) {
            StringBuilder sb = AggregateString(categories, messages);
            _applicationLogger.WriteStringBuilderToFile(sb);
        }
    }
}
