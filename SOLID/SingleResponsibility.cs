using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOLID {
    class RecordBook {
        private readonly List<string> Records = [];

        internal void AddRecord(string record) {
            Records.Add(record);
        }

        public override string ToString() {
            return string.Join(Environment.NewLine, Records);
        }
    }

    class RecordBook__Wrong {
        private readonly List<string> Records = [];

        internal void AddRecord(string record) {
            Records.Add(record);
        }

        public override string ToString() {
            return string.Join(Environment.NewLine, Records);
        }

        // This method should be in another class do not hesitate single responsibility prinsiple
        internal void SaveToFile(string filename) {
            File.WriteAllText(filename, ToString());
        }

        internal void LoadFromFile(string filename) { }

        internal void LoadFromUri(Uri uri) { }
    }

    internal class SingleResponsibility {
        internal static void TestRecords() {
            RecordBook rb = new();
            rb.AddRecord("Record 1");
            rb.AddRecord("Record 2");
            Console.WriteLine(rb);
        }
    }
}
