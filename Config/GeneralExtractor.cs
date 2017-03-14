using System;
using System.Data.SqlClient;

namespace TrivolloWebApi.Config
{
    public class GeneralExtractor
    {
        public static int ExtractInt(SqlDataReader reader, string columnName, int defaultValue)
        {
            int result = defaultValue;
            if (reader.Read())
            {
                result = (int)reader[columnName];
            }

            reader.Close();
            return result;
        }

        public static int ExtractInt(SqlDataReader reader, int columnIdx, int defaultValue)
        {
            int result = defaultValue;
            if (reader.Read())
            {
                result = reader.GetInt32(columnIdx);
            }

            reader.Close();
            return result;
        }

        public static string ExtractString(SqlDataReader reader, string columnName, string defaultValue)
        {
            string result = defaultValue;
            if (reader.Read())
            {
                result = (string)reader[columnName];
            }

            reader.Close();
            return result;
        }

        public static string ExtractString(SqlDataReader reader, int columnIdx, string defaultValue)
        {
            string result = defaultValue;
            if (reader.Read())
            {
                result = reader.GetString(columnIdx);
            }

            reader.Close();
            return result;
        }
    }
}