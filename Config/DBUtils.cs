using System.Data.SqlClient;

namespace TrivolloWebApi.Config
{
    public class DBUtils
    {
        public static SqlConnection getDbConnection()
        {
            SqlConnection dbConnection = ApplicationContext.getConnection();

            if (dbConnection.State.ToString() == "Closed")
            {
                dbConnection.Open();
            }

            return dbConnection;
        }
    }
}