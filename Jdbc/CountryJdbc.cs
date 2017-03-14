using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using TrivolloWebApi.Config;
using TrivolloWebApi.Models;

namespace TrivolloWebApi.Jdbc
{
    public class CountryJdbc
    {
        public ArrayList GetAllCountries()
        {
            SqlConnection dbConnection = DBUtils.getDbConnection();
            SqlTransaction dbTransaction = dbConnection.BeginTransaction();

            StringBuilder sql = new StringBuilder();
            sql.Append(" SELECT cnt_id, cnt_name, cnt_code ");
            sql.Append(" FROM prmCountry ");
            sql.Append(" ORDER BY cnt_name ");

            SqlCommand dbCommand = new SqlCommand(sql.ToString(), dbConnection);
            dbCommand.Transaction = dbTransaction;

            ArrayList countries = new ArrayList();
            try
            {
                SqlDataReader reader = dbCommand.ExecuteReader();
                while (reader.Read())
                {
                    Country country = new Country();
                    country.Id = (int)reader["cnt_id"];
                    country.Name = (string)reader["cnt_name"];
                    country.Code = (string)reader["cnt_code"];
                    countries.Add(country);
                }
                reader.Close();
            }
            catch (SqlException) { }

            dbConnection.Close();
            return countries;
        }

        
    }
}