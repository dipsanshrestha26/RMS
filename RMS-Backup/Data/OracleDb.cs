using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace RMS.Data
{
    public class OracleDb
    {
        private readonly IConfiguration _config;

        public OracleDb(IConfiguration config)
        {
            _config = config;
        }

        private OracleConnection GetConnection()
        {
            return new OracleConnection(_config.GetConnectionString("OracleDb"));
        }

        public DataTable Query(string sql, params OracleParameter[] parameters)
        {
            using var con = GetConnection();
            using var cmd = new OracleCommand(sql, con);
            using var da = new OracleDataAdapter(cmd);

            if (parameters != null && parameters.Length > 0)
                cmd.Parameters.AddRange(parameters);

            var dt = new DataTable();
            con.Open();
            da.Fill(dt);
            return dt;
        }

        public int Execute(string sql, params OracleParameter[] parameters)
        {
            using var con = GetConnection();
            using var cmd = new OracleCommand(sql, con);

            if (parameters != null && parameters.Length > 0)
                cmd.Parameters.AddRange(parameters);

            con.Open();
            return cmd.ExecuteNonQuery();
        }

        public object Scalar(string sql, params OracleParameter[] parameters)
        {
            using var con = GetConnection();
            using var cmd = new OracleCommand(sql, con);

            if (parameters != null && parameters.Length > 0)
                cmd.Parameters.AddRange(parameters);

            con.Open();
            return cmd.ExecuteScalar();
        }
    }
}