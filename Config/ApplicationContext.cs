using System.Configuration;
using System.Data.SqlClient;

namespace TrivolloWebApi.Config
{
    public class ApplicationContext
    {
        private static SqlConnection Connection;
        private static string ConnectionString_Enc = ConfigurationManager.ConnectionStrings["appContext"].ConnectionString;

        public static SqlConnection getConnection()
        {
            SimpleAES encryptor = new SimpleAES();
            Connection = new SqlConnection(encryptor.DecryptString(ConnectionString_Enc));
            return Connection;
        }
    }
}