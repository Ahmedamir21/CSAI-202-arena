using System.Text;
using SwiftShip.Web.Reporting;

namespace SwiftShip.Web.Services
{
    /// <summary>
    /// Turns a report table into a CSV file. Every report page has an "Export CSV" button,
    /// and this is what it runs.
    /// </summary>
    public static class Csv
    {
        public static byte[] ToBytes(ReportTable table)
        {
            var text = new StringBuilder();

            if (table != null)
            {
                text.AppendLine(string.Join(",", table.Columns.Select(Quote)));
                foreach (var row in table.Rows)
                    text.AppendLine(string.Join(",", row.Select(Quote)));
            }

            // the byte order mark makes Excel open the file with the right encoding
            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text.ToString())).ToArray();
        }

        // a value that contains a comma, a quote or a new line has to be wrapped in quotes
        private static string Quote(string value)
        {
            if (value == null)
                return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }
    }
}
