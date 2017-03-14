using System;
using System.Collections;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Text;
using TrivolloWebApi.Config;
using TrivolloWebApi.Models;

namespace TrivolloWebApi.Jdbc
{
    public class UserJdbc
    {
        public bool ActivateEmail(string email)
        {
            SqlConnection dbConnection = DBUtils.getDbConnection();
            SqlTransaction dbTransaction = dbConnection.BeginTransaction();

            StringBuilder sql = new StringBuilder();
            sql.Append(" IF EXISTS (SELECT usem_us_id FROM tblUserEmails WHERE usem_email = @email AND usem_verified = 0) ");
            sql.Append(" BEGIN ");
            sql.Append("    UPDATE tblUserEmails SET usem_verified = 1, usem_verified_date = GETDATE() WHERE usem_email = @email; ");
            sql.Append(" END ");

            SqlCommand dbCommand = new SqlCommand(sql.ToString(), dbConnection);
            dbCommand.Transaction = dbTransaction;
            dbCommand.Parameters.Add("email", SqlDbType.NVarChar).Value = email;

            int result = 0;
            try
            {
                result = dbCommand.ExecuteNonQuery();
                dbTransaction.Commit();
            }
            catch (SqlException)
            {
                dbTransaction.Rollback();
            }

            dbConnection.Close();
            return result == 1;
        }

        /// <summary>Creates user via the usual Email registration process. Username, email and password are required.</summary>
        /// <param name="User"> User object to store on the database</param>
        public int CreateUserFromEmail(User user)
        {
            SqlConnection dbConnection = DBUtils.getDbConnection();
            SqlTransaction dbTransaction = dbConnection.BeginTransaction();

            StringBuilder sql = new StringBuilder();
            sql.Append(" IF NOT EXISTS (SELECT usem_us_id FROM tblUserEmails WHERE usem_email = @email) ");
            sql.Append(" BEGIN ");
            sql.Append("    DECLARE @userId INT; ");
            sql.Append("    INSERT INTO tblUsers (us_name, us_password, us_registration_date, us_profile_picture) VALUES (@userName, @password, GETDATE(), @defaultProfilePic); ");
            sql.Append("    SET @userId = (SELECT CAST(SCOPE_IDENTITY() AS INT)); ");

            sql.Append("    INSERT INTO tblUserEmails (usem_email, usem_us_id, usem_verified) VALUES (@email, @userId, 0); ");

            sql.Append("    SELECT @userId; ");
            sql.Append(" END ");
            sql.Append(" ELSE ");
            sql.Append(" BEGIN ");
            sql.Append("    SELECT -1; ");
            sql.Append(" END ");

            SqlCommand dbCommand = new SqlCommand(sql.ToString(), dbConnection);
            dbCommand.Transaction = dbTransaction;
            dbCommand.Parameters.Add("userName", SqlDbType.NVarChar).Value = user.Username;
            dbCommand.Parameters.Add("email", SqlDbType.NVarChar).Value = user.Emails[0];
            dbCommand.Parameters.Add("password", SqlDbType.NVarChar).Value = user.Password;
            dbCommand.Parameters.Add("defaultProfilePic", SqlDbType.NVarChar).Value = Utilities.WS_API_URL + "images/profile_pics/profile_default.jpg";

            int result = -1;
            try
            {
                result = GeneralExtractor.ExtractInt(dbCommand.ExecuteReader(), 0, -1);
                dbTransaction.Commit();
            }
            catch (SqlException)
            {
                dbTransaction.Rollback();
            }

            dbConnection.Close();
            return result;
        }

        /// <summary>
        /// Creates user via social network. If user already exists, update existing one with social network details
        /// </summary>
        /// <param name="user">User object</param>
        /// <param name="socialNetworkName">Social network name. I.e. Google</param>
        /// <returns>User id</returns>
        public int CreateUserViaSocialNetwork(User user, string socialNetworkName)
        {
            SqlConnection dbConnection = DBUtils.getDbConnection();
            SqlTransaction dbTransaction = dbConnection.BeginTransaction();

            StringBuilder sql = new StringBuilder();
            sql.Append(" DECLARE @userId INT; ");

            sql.Append(" IF NOT EXISTS (SELECT usem_us_id FROM tblUserEmails WHERE usem_email = @email) ");
            sql.Append(" BEGIN ");
            //              User does not exist - create a new one
            sql.Append("    INSERT INTO tblUsers (us_name, us_password, us_registration_date, us_profile_picture) VALUES (@userName, @password, GETDATE(), @profilePic); ");
            sql.Append("    SET @userId = (SELECT CAST(SCOPE_IDENTITY() AS INT)); ");

            sql.Append("    INSERT INTO tblUserEmails (usem_email, usem_us_id, usem_verified, usem_verified_date) VALUES (@email, @userId, 0, GETDATE()); ");

            sql.Append("    INSERT INTO tblUserSocialMedia (ussm_socntw_id, ussm_socntw_user_id, ussm_usem_email, ussm_us_id) ");
            sql.Append("    SELECT socntw_id, @socialNetworkUserId, @email, @userId ");
            sql.Append("    FROM prmSocialNetwork ");
            sql.Append("    WHERE socntw_name = @socialNetworkName; ");

            sql.Append("    SELECT @userId; ");
            sql.Append(" END ");
            sql.Append(" ELSE ");
            sql.Append(" BEGIN ");
            sql.Append("    IF NOT EXISTS (SELECT ussm_usem_email FROM tblUserSocialMedia INNER JOIN prmSocialNetwork ON socntw_id = ussm_socntw_id WHERE socntw_name = @socialNetworkName AND ussm_usem_email = @email) ");
            sql.Append("        AND (SELECT TOP 1 usem_verified FROM tblUserEmails WHERE usem_email = @email) = 1 ");
            sql.Append("    BEGIN");
            //                  User exists - update it with social network details
            sql.Append("        SET @userId = (SELECT usem_us_id FROM tblUserEmails WHERE usem_email = @email); ");

            sql.Append("        UPDATE tblUsers ");
            sql.Append("        SET us_profile_picture = CASE us_profile_picture WHEN @defaultProfilePic THEN @profilePic ELSE @defaultProfilePic END ");
            sql.Append("        WHERE us_id = @userId; ");

            sql.Append("        INSERT INTO tblUserSocialMedia (ussm_socntw_id, ussm_socntw_user_id, ussm_usem_email, ussm_us_id) ");
            sql.Append("        SELECT socntw_id, @socialNetworkUserId, @email, @userId ");
            sql.Append("        FROM prmSocialNetwork ");
            sql.Append("        WHERE socntw_name = @socialNetworkName; ");

            sql.Append("        SELECT @userId; ");

            sql.Append("    END ");
            sql.Append("    ELSE ");
            sql.Append("    BEGIN");
            //                  Such email already exists with this social network or email exists but user has not been activated - should never happen
            sql.Append("        SELECT -1; ");
            sql.Append("    END");
            sql.Append(" END ");

            SqlCommand dbCommand = new SqlCommand(sql.ToString(), dbConnection);
            dbCommand.Transaction = dbTransaction;
            dbCommand.Parameters.Add("Username", SqlDbType.NVarChar).Value = user.Username;
            dbCommand.Parameters.Add("email", SqlDbType.NVarChar).Value = user.Emails[0];
            dbCommand.Parameters.Add("password", SqlDbType.NVarChar).Value = user.Password;
            dbCommand.Parameters.Add("socialNetworkUserId", SqlDbType.NVarChar).Value = user.SocialNetworkUserId;
            dbCommand.Parameters.Add("socialNetworkName", SqlDbType.NVarChar).Value = socialNetworkName;
            dbCommand.Parameters.Add("profilePic", SqlDbType.NVarChar).Value = user.ProfilePicURL;
            dbCommand.Parameters.Add("defaultProfilePic", SqlDbType.NVarChar).Value = Utilities.WS_API_URL + "images/profile_pics/profile_default.jpg";

            int result = -1;
            try
            {
                result = GeneralExtractor.ExtractInt(dbCommand.ExecuteReader(), 0, -1);
                dbTransaction.Commit();
            }
            catch (SqlException)
            {
                dbTransaction.Rollback();
            }

            dbConnection.Close();
            return result;
        }

        public string GetEmailBySocialNetworkUserId(string socialNetworkName, string socialNetworkUserId)
        {
            SqlConnection dbConnection = DBUtils.getDbConnection();
            SqlTransaction dbTransaction = dbConnection.BeginTransaction();

            StringBuilder sql = new StringBuilder();
            sql.Append(" SELECT ussm_usem_email ");
            sql.Append(" FROM tblUserSocialMedia ");
            sql.Append(" INNER JOIN prmSocialNetwork ON socntw_id = ussm_socntw_id ");
            sql.Append(" WHERE socntw_name = @socialNetworkName AND ussm_socntw_user_id = @socialNetworkUserId ");

            SqlCommand dbCommand = new SqlCommand(sql.ToString(), dbConnection);
            dbCommand.Transaction = dbTransaction;
            dbCommand.Parameters.Add("socialNetworkName", SqlDbType.NVarChar).Value = socialNetworkName;
            dbCommand.Parameters.Add("socialNetworkUserId", SqlDbType.NVarChar).Value = socialNetworkUserId;

            string result = null;
            try
            {
                result = GeneralExtractor.ExtractString(dbCommand.ExecuteReader(), 0, null);
                dbTransaction.Commit();
            }
            catch (SqlException)
            {
                dbTransaction.Rollback();
            }

            dbConnection.Close();
            return result;
        }

        public string GenerateOrReturnPasswordResetCode(string email)
        {
            SqlConnection dbConnection = DBUtils.getDbConnection();
            SqlTransaction dbTransaction = dbConnection.BeginTransaction();

            StringBuilder sql = new StringBuilder();
            sql.Append(" IF EXISTS ");
            sql.Append("    ( ");
            sql.Append("        SELECT pwr_code ");
            sql.Append("        FROM tblPasswordReset ");
            sql.Append("        WHERE pwr_email = @email AND pwr_attempts < 3 AND pwr_request_date > DATEADD(MINUTE, -15, GETDATE()) ");
            sql.Append("    ) ");
            sql.Append(" BEGIN ");
            sql.Append("    SELECT pwr_code FROM tblPasswordReset WHERE pwr_email = @email; ");
            sql.Append(" END ");
            sql.Append(" ELSE ");
            sql.Append(" BEGIN ");
            sql.Append("    DELETE tblPasswordReset WHERE pwr_email = @email; ");
            sql.Append("    DECLARE @codeVar VARCHAR(6) = '" + Utilities.generatePasswordResetCode() + "'; ");
            sql.Append("    INSERT INTO tblPasswordReset (pwr_email, pwr_code, pwr_request_date) VALUES (@email, @codeVar, GETDATE()); ");
            sql.Append("    SELECT @codeVar; ");
            sql.Append(" END ");

            SqlCommand dbCommand = new SqlCommand(sql.ToString(), dbConnection);
            dbCommand.Transaction = dbTransaction;
            dbCommand.Parameters.Add("email", SqlDbType.NVarChar).Value = email;

            string result = null;
            try
            {
                result = GeneralExtractor.ExtractString(dbCommand.ExecuteReader(), 0, null);
                dbTransaction.Commit();
            }
            catch (SqlException)
            {
                dbTransaction.Rollback();
            }

            dbConnection.Close();
            return result;
        }

        public User GetUserByEmail(string email)
        {
            SqlConnection dbConnection = DBUtils.getDbConnection();
            SqlTransaction dbTransaction = dbConnection.BeginTransaction();

            StringBuilder sql = new StringBuilder();
            sql.Append(" SELECT us_id, us_name, us_profile_picture, usem_email, usem_verified, cnt_code ");
            sql.Append(" FROM tblUsers ");
            sql.Append(" INNER JOIN tblUserEmails ON usem_us_id = us_id ");
            sql.Append(" LEFT OUTER JOIN prmCountry ON cnt_id = us_cnt_id ");
            sql.Append(" WHERE usem_email = @email ");

            SqlCommand dbCommand = new SqlCommand(sql.ToString(), dbConnection);
            dbCommand.Transaction = dbTransaction;
            dbCommand.Parameters.Add("email", SqlDbType.NVarChar).Value = email;

            User user = null;
            try
            {
                SqlDataReader reader = dbCommand.ExecuteReader();
                if (reader.Read())
                {
                    user = new User();
                    user.Id = (int)reader["us_id"];
                    user.Username = (string)reader["us_name"];
                    user.ProfilePicURL = (string)reader["us_profile_picture"];
                    ArrayList emails = new ArrayList();
                    emails.Add((string)reader["usem_email"]);
                    user.Emails = emails;
                    user.EmailVerified = (bool)reader["usem_verified"];
                    user.CountryCode = reader["cnt_code"]==DBNull.Value ? null : Convert.ToString(reader["cnt_code"]);
                }
                reader.Close();
            }
            catch (SqlException) {}

            dbConnection.Close();
            return user;
        }

        public int GetUserIdByEmail(string email)
        {
            SqlConnection dbConnection = DBUtils.getDbConnection();
            SqlTransaction dbTransaction = dbConnection.BeginTransaction();

            StringBuilder sql = new StringBuilder();
            sql.Append(" SELECT usem_us_id ");
            sql.Append(" FROM tblUserEmails ");
            sql.Append(" WHERE usem_email = @email ");

            SqlCommand dbCommand = new SqlCommand(sql.ToString(), dbConnection);
            dbCommand.Transaction = dbTransaction;
            dbCommand.Parameters.Add("email", SqlDbType.NVarChar).Value = email;

            int result = -1;
            try
            {
                result = GeneralExtractor.ExtractInt(dbCommand.ExecuteReader(), "usem_us_id", -1);
            }
            catch (SqlException) { }

            dbConnection.Close();
            return result;
        }

        public string GetUserPasswordByEmail(string email)
        {
            SqlConnection dbConnection = DBUtils.getDbConnection();
            SqlTransaction dbTransaction = dbConnection.BeginTransaction();

            StringBuilder sql = new StringBuilder();
            sql.Append(" SELECT us_password FROM tblUsers ");
            sql.Append(" INNER JOIN tblUserEmails ON usem_us_id = us_id ");
            sql.Append(" WHERE usem_email = @email; ");

            SqlCommand dbCommand = new SqlCommand(sql.ToString(), dbConnection);
            dbCommand.Transaction = dbTransaction;
            dbCommand.Parameters.Add("email", SqlDbType.NVarChar).Value = email;

            string result = null;
            try
            {
                result = GeneralExtractor.ExtractString(dbCommand.ExecuteReader(), "us_password", null);
            }
            catch (SqlException) { }

            dbConnection.Close();
            return result;
        }

        public void RemoveUserIfActivationExpired(string email)
        {
            SqlConnection dbConnection = DBUtils.getDbConnection();
            SqlTransaction dbTransaction = dbConnection.BeginTransaction();

            StringBuilder sql = new StringBuilder();
            sql.Append(" IF EXISTS ");
            sql.Append("    ( ");
            sql.Append("        SELECT usem_us_id ");
            sql.Append("        FROM tblUserEmails ");
            sql.Append("        INNER JOIN tblUsers ON us_id = usem_us_id ");
            sql.Append("        WHERE usem_email = @email AND usem_verified = 0 AND DATEADD(HOUR, 48, us_registration_date) < GETDATE() ");
            sql.Append("    ) ");
            sql.Append(" BEGIN ");
            sql.Append("    DECLARE @userId INT = (SELECT usem_us_id FROM tblUserEmails WHERE usem_email = @email); ");
            sql.Append("    EXEC sp_RemoveUser @userId; ");
            sql.Append(" END ");

            SqlCommand dbCommand = new SqlCommand(sql.ToString(), dbConnection);
            dbCommand.Transaction = dbTransaction;
            dbCommand.Parameters.Add("email", SqlDbType.NVarChar).Value = email;

            try
            {
                dbCommand.ExecuteNonQuery();
                dbTransaction.Commit();
            }
            catch (SqlException)
            {
                dbTransaction.Rollback();
            }

            dbConnection.Close();
        }

        public bool ResetUserPassword(string email, string code, string newPassword)
        {
            SqlConnection dbConnection = DBUtils.getDbConnection();
            SqlTransaction dbTransaction = dbConnection.BeginTransaction();

            StringBuilder sql = new StringBuilder();
            sql.Append(" IF EXISTS ");
            sql.Append("    ( ");
            sql.Append("        SELECT pwr_code ");
            sql.Append("        FROM tblPasswordReset ");
            sql.Append("        WHERE pwr_email = @email AND pwr_attempts < 3 AND pwr_code = @code AND pwr_request_date > DATEADD(MINUTE, -20, GETDATE()) ");
            sql.Append("    ) ");
            sql.Append(" BEGIN ");
            //Update password and remove code entry
            sql.Append("    UPDATE tblUsers SET us_password = @password ");
            sql.Append("    FROM tblUsers ");
            sql.Append("    INNER JOIN tblUserEmails ON usem_us_id = us_id ");
            sql.Append("    WHERE usem_email = @email; ");

            sql.Append("    DELETE tblPasswordReset WHERE pwr_email = @email; ");

            sql.Append("    SELECT 1; ");
            sql.Append(" END ");
            sql.Append(" ELSE ");
            sql.Append(" BEGIN ");
            sql.Append("    IF EXISTS ");
            sql.Append("        ( ");
            sql.Append("            SELECT pwr_code ");
            sql.Append("            FROM tblPasswordReset ");
            sql.Append("            WHERE pwr_email = @email AND pwr_attempts < 3 AND pwr_request_date > DATEADD(MINUTE, -20, GETDATE()) ");
            sql.Append("        ) ");
            sql.Append("    BEGIN ");
            //Incorrect code - increase number of attempts
            sql.Append("        UPDATE tblPasswordReset SET pwr_attempts = pwr_attempts + 1 FROM tblPasswordReset WHERE pwr_email = @email; ");
            sql.Append("    END ");
            sql.Append("    ELSE ");
            sql.Append("    BEGIN ");
            //3 attempts have been reached or code has expired or email does not exist in the request table - remove code entry for email
            sql.Append("        DELETE tblPasswordReset WHERE pwr_email = @email; ");
            sql.Append("    END ");
            sql.Append("    SELECT 0; ");
            sql.Append(" END ");

            SqlCommand dbCommand = new SqlCommand(sql.ToString(), dbConnection);
            dbCommand.Transaction = dbTransaction;
            dbCommand.Parameters.Add("email", SqlDbType.NVarChar).Value = email;
            dbCommand.Parameters.Add("code", SqlDbType.NVarChar).Value = code;
            dbCommand.Parameters.Add("password", SqlDbType.NVarChar).Value = newPassword;

            bool result = false;
            try
            {
                result = GeneralExtractor.ExtractInt(dbCommand.ExecuteReader(), 0, 0) == 1;
                dbTransaction.Commit();
            }
            catch (SqlException)
            {
                dbTransaction.Rollback();
            }

            dbConnection.Close();
            return result;
        }

        /// <summary>Update Social network's email address for a user. This method should be used to keep user
        /// details up to date when user updates their email address in social network.
        /// 
        /// If a new email does not exist in the system, a new email will be added into the tblUserEmails table and updated in the tblUserSocialMedia table.
        /// If a new email exists in the system and belongs to another user, users will be merged and email will be updated in tblUserSocialMedia table.
        /// If a new email exists in the system and belongs to the same user, email will be updated in the tblUserSocialMedia table.</summary>
        /// <param name="socialNetworkName"> Name of a Social Network</param>
        /// <param name="socialNetworkUserId"> User's user ID in the Social Network</param>
        /// <param name="newEmail"> New email address</param>
        public bool UpdateSocialNetworkEmail(string socialNetworkName, string socialNetworkUserId, string newEmail)
        {
            SqlConnection dbConnection = DBUtils.getDbConnection();
            SqlTransaction dbTransaction = dbConnection.BeginTransaction();

            StringBuilder sql = new StringBuilder();
            sql.Append(" DECLARE @userId INT = (SELECT ussm_us_id FROM tblUserSocialMedia INNER JOIN prmSocialNetwork ON socntw_id = ussm_socntw_id ");
            sql.Append("                                    WHERE socntw_name = @socialNetworkName AND ussm_socntw_user_id = @socialNetworkUserId); ");

            sql.Append(" IF NOT EXISTS (SELECT usem_us_id FROM tblUserEmails WHERE usem_email = @newEmail) ");
            sql.Append(" BEGIN ");
            //              Email does not exist - add new email in tblUserEmails table and update email in tblUserSocialMedia table
            sql.Append("    INSERT INTO tblUserEmails (usem_email, usem_us_id, usem_verified, usem_verified_date) VALUES (@newEmail, @userId, 1, GETDATE()); ");
            sql.Append("    UPDATE tblUserSocialMedia SET ussm_usem_email = @newEmail FROM tblUserSocialMedia INNER JOIN prmSocialNetwork ON socntw_id = ussm_socntw_id WHERE socntw_name = @socialNetworkName AND ussm_socntw_user_id = @socialNetworkUserId; ");
            sql.Append(" END ");
            sql.Append(" ELSE ");
            sql.Append(" BEGIN ");
            //              Email exists
            sql.Append("    IF EXISTS (SELECT usem_us_id FROM tblUserEmails WHERE usem_email = @newEmail AND usem_us_id <> @userId) ");
            sql.Append("    BEGIN ");
            //                  but belongs to another user - merge users
            sql.Append("        DECLARE @userIdOld INT = (SELECT usem_us_id FROM tblUserEmails WHERE usem_email = @newEmail); ");
            sql.Append("        EXEC sp_MergeUsers @userId, @userIdOld; ");
            sql.Append("    END ");
            //              update email in tblUserSocialMedia table
            sql.Append("    UPDATE tblUserSocialMedia SET ussm_usem_email = @newEmail FROM tblUserSocialMedia INNER JOIN prmSocialNetwork ON socntw_id = ussm_socntw_id WHERE socntw_name = @socialNetworkName AND ussm_socntw_user_id = @socialNetworkUserId; ");
            sql.Append(" END ");

            SqlCommand dbCommand = new SqlCommand(sql.ToString(), dbConnection);
            dbCommand.Transaction = dbTransaction;
            dbCommand.Parameters.Add("socialNetworkName", SqlDbType.VarChar).Value = socialNetworkName;
            dbCommand.Parameters.Add("socialNetworkUserId", SqlDbType.VarChar).Value = socialNetworkUserId;
            dbCommand.Parameters.Add("newEmail", SqlDbType.VarChar).Value = newEmail;

            int result = 0;
            try
            {
                result = dbCommand.ExecuteNonQuery();
                dbTransaction.Commit();
            }
            catch (SqlException)
            {
                dbTransaction.Rollback();
            }

            dbConnection.Close();
            return result > 0;
        }

        public bool UpdateUserCountry(int userId, string countryCode)
        {
            SqlConnection dbConnection = DBUtils.getDbConnection();
            SqlTransaction dbTransaction = dbConnection.BeginTransaction();

            StringBuilder sql = new StringBuilder();
            sql.Append(" UPDATE tblUsers SET us_cnt_id = (SELECT TOP 1 cnt_id FROM prmCountry WHERE cnt_code = @code) ");
            sql.Append(" WHERE us_id = @userId; ");

            SqlCommand dbCommand = new SqlCommand(sql.ToString(), dbConnection);
            dbCommand.Transaction = dbTransaction;
            dbCommand.Parameters.Add("userId", SqlDbType.Int).Value = userId;
            dbCommand.Parameters.Add("code", SqlDbType.VarChar).Value = countryCode;

            int result = 0;
            try
            {
                result = dbCommand.ExecuteNonQuery();
                dbTransaction.Commit();
            }
            catch (SqlException) {
                dbTransaction.Rollback();
            }

            dbConnection.Close();
            return result == 1;
        }
    }
}