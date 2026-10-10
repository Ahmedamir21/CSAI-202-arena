using System.Data;
using Microsoft.Data.SqlClient;
using SwiftShip.Web.Reporting;

namespace SwiftShip.Web.Data
{
    /// <summary>
    /// Opens connections and runs stored procedures.
    ///
    /// Every query in the project goes through these four helpers, and every value that
    /// comes from the user is added as a parameter, so no SQL text is ever built by
    /// concatenating strings.
    /// </summary>
    public static class Db
    {
        private static string _connectionString;

        public static void Configure(string connectionString)
        {
            _connectionString = connectionString;
        }

        public static SqlConnection Open()
        {
            var con = new SqlConnection(_connectionString);
            con.Open();
            return con;
        }

        private static SqlCommand NewCmd(string proc, SqlConnection con,
                                         Action<SqlParameterCollection> addParams)
        {
            var cmd = new SqlCommand(proc, con);
            cmd.CommandType = CommandType.StoredProcedure;
            addParams?.Invoke(cmd.Parameters);
            return cmd;
        }

        /// <summary>Runs a procedure and maps every row of the result into a list.</summary>
        public static List<T> Query<T>(string proc, Action<SqlParameterCollection> addParams,
                                       Func<SqlDataReader, T> map)
        {
            var result = new List<T>();
            using (var con = Open())
            using (var cmd = NewCmd(proc, con, addParams))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    result.Add(map(reader));
            }
            return result;
        }

        /// <summary>Runs a procedure and maps the first row, or null when there is none.</summary>
        public static T QuerySingle<T>(string proc, Action<SqlParameterCollection> addParams,
                                       Func<SqlDataReader, T> map) where T : class
        {
            using (var con = Open())
            using (var cmd = NewCmd(proc, con, addParams))
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                    return map(reader);
            }
            return null;
        }

        /// <summary>
        /// For the procedures that answer with more than one result set, for example the
        /// tracking page (the shipment, then its timeline, then its payments).
        /// </summary>
        public static void ReadMultiple(string proc, Action<SqlParameterCollection> addParams,
                                        params Action<SqlDataReader>[] readers)
        {
            using (var con = Open())
            using (var cmd = NewCmd(proc, con, addParams))
            using (var reader = cmd.ExecuteReader())
            {
                for (int i = 0; i < readers.Length; i++)
                {
                    if (i > 0 && !reader.NextResult())
                        break;
                    while (reader.Read())
                        readers[i](reader);
                }
            }
        }

        /// <summary>Runs a procedure that changes data and returns the affected rows.</summary>
        public static int Execute(string proc, Action<SqlParameterCollection> addParams)
        {
            using (var con = Open())
            using (var cmd = NewCmd(proc, con, addParams))
            {
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Runs a procedure that returns one value, for example a new id.</summary>
        public static object Scalar(string proc, Action<SqlParameterCollection> addParams)
        {
            using (var con = Open())
            using (var cmd = NewCmd(proc, con, addParams))
            {
                return cmd.ExecuteScalar();
            }
        }

        /// <summary>
        /// Reads every result set of a procedure without knowing its shape. Used by the report
        /// pages and by the CSV export, which render whatever columns came back.
        /// </summary>
        public static List<ReportTable> ReadAll(string proc, Action<SqlParameterCollection> addParams)
        {
            var tables = new List<ReportTable>();
            using (var con = Open())
            using (var cmd = NewCmd(proc, con, addParams))
            using (var reader = cmd.ExecuteReader())
            {
                do
                {
                    var table = new ReportTable();
                    for (int i = 0; i < reader.FieldCount; i++)
                        table.Columns.Add(reader.GetName(i));
                    while (reader.Read())
                    {
                        var row = new List<string>();
                        for (int i = 0; i < reader.FieldCount; i++)
                            row.Add(reader.IsDBNull(i) ? "" : Convert.ToString(reader.GetValue(i)));
                        table.Rows.Add(row);
                    }
                    tables.Add(table);
                }
                while (reader.NextResult());
            }
            return tables;
        }

        // Small helpers so that the callers do not repeat "cmd.Parameters.AddWithValue".
        public static void Add(this SqlParameterCollection p, string name, object value)
        {
            p.AddWithValue(name, value ?? DBNull.Value);
        }

        public static void AddInt(this SqlParameterCollection p, string name, int? value)
        {
            p.AddWithValue(name, value.HasValue ? (object)value.Value : DBNull.Value);
        }

        public static void AddMoney(this SqlParameterCollection p, string name, decimal? value)
        {
            p.AddWithValue(name, value.HasValue ? (object)value.Value : DBNull.Value);
        }
    }

    /// <summary>Reading a column without repeating the DBNull test everywhere.</summary>
    public static class ReaderExtensions
    {
        private static int Index(SqlDataReader r, string name)
        {
            for (int i = 0; i < r.FieldCount; i++)
                if (string.Equals(r.GetName(i), name, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        public static bool IsNull(this SqlDataReader r, string name)
        {
            int i = Index(r, name);
            return i < 0 || r.IsDBNull(i);
        }

        public static int Int(this SqlDataReader r, string name)
        {
            return r.IsNull(name) ? 0 : Convert.ToInt32(r[name]);
        }

        public static int? IntOrNull(this SqlDataReader r, string name)
        {
            return r.IsNull(name) ? (int?)null : Convert.ToInt32(r[name]);
        }

        public static long Long(this SqlDataReader r, string name)
        {
            return r.IsNull(name) ? 0 : Convert.ToInt64(r[name]);
        }

        public static decimal Dec(this SqlDataReader r, string name)
        {
            return r.IsNull(name) ? 0m : Convert.ToDecimal(r[name]);
        }

        public static decimal? DecOrNull(this SqlDataReader r, string name)
        {
            return r.IsNull(name) ? (decimal?)null : Convert.ToDecimal(r[name]);
        }

        public static string Str(this SqlDataReader r, string name)
        {
            return r.IsNull(name) ? null : Convert.ToString(r[name]);
        }

        public static bool Bool(this SqlDataReader r, string name)
        {
            return !r.IsNull(name) && Convert.ToBoolean(r[name]);
        }

        public static DateTime? Date(this SqlDataReader r, string name)
        {
            return r.IsNull(name) ? (DateTime?)null : Convert.ToDateTime(r[name]);
        }

        public static byte[] Bytes(this SqlDataReader r, string name)
        {
            return r.IsNull(name) ? null : (byte[])r[name];
        }
    }
}
