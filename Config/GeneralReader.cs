using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace TrivolloWebApi.Config
{
    public class GeneralReader
    {
        public static int getInt(SqlDataReader reader, int order)
        {
            return Int32.Parse(reader.GetString(order));
        }
    }
}