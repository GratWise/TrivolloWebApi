using Google.Apis.Oauth2.v2;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Web;
using System.Web.Http;
using TrivolloWebApi.Config;
using TrivolloWebApi.Jdbc;
using TrivolloWebApi.Models;

namespace TrivolloWebApi.Controllers
{
    [RoutePrefix("user")]
    public class UserController : ApiController
    {
        private UserJdbc userJdbc = new UserJdbc();

        [HttpGet]
        [Route("activate/email/{mailEncrypted}")]
        public HttpResponseMessage ActivateMail(string mailEncrypted)
        {
            string viewPath;
            try
            {

                SimpleAES encryptor = new SimpleAES();
                string email = encryptor.DecryptString(mailEncrypted);

                userJdbc.RemoveUserIfActivationExpired(email);

                if (userJdbc.ActivateEmail(email))
                {
                    //Email has been activated
                    viewPath = HttpContext.Current.Server.MapPath(@"~/Views/Activation/success.html");
                }
                else
                {
                    //User does not exist anymore
                    viewPath = HttpContext.Current.Server.MapPath(@"~/Views/Activation/fail.html");
                }
            }
            catch (Exception)
            {
                //Could not decrypt
                viewPath = HttpContext.Current.Server.MapPath(@"~/Views/Activation/error.html");
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var template = File.ReadAllText(viewPath);
            response.Content = new StringContent(template);
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/html");
            return response;
        }

        [HttpPost]
        [Route("connect/{socialNetworkName}")]
        public User ConnectViaSocialNetwork(User user, string socialNetworkName)
        {
            var _socialNetworkClientId = "";
            var _socialNetworkUserId = "";
            var _issuedTo = "";

            if (object.Equals(socialNetworkName, "Google"))
            {
                _socialNetworkClientId = SecurityUtils.GOOGLE_OAUTH_WEB_CLIENT_ID;

                var tokeninfo_request = new Oauth2Service().Tokeninfo();
                tokeninfo_request.IdToken = user.SocialNetworkIdToken;
                var tokeninfo = tokeninfo_request.Execute();

                _socialNetworkUserId = tokeninfo.UserId;
                _issuedTo = tokeninfo.IssuedTo;
            }
            else
            {
                //Social network does not exist
                return null;
            }

            if (object.Equals(user.SocialNetworkUserId, _socialNetworkUserId) && object.Equals(_socialNetworkClientId, _issuedTo))
            {
                // Credentials match.
                
                // Check if google user ID already exists in the system.
                string currentEmail = userJdbc.GetEmailBySocialNetworkUserId(socialNetworkName, user.SocialNetworkUserId);

                if (String.IsNullOrWhiteSpace(currentEmail))
                {
                    // Google user ID does not exist in the system.
                    // If email already exists but has not been confirmed, delete it.
                    userJdbc.RemoveUserIfActivationExpired((string)user.Emails[0]);

                    //Generate random alphanumeric password
                    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                    var random = new Random();
                    string randomPass = new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());
                    user.Password = SecurityUtils.CreatePasswordHash(randomPass);

                    //Make sure username is not null
                    if (String.IsNullOrWhiteSpace(user.Username))
                    {
                        var mail = new MailAddress((string) user.Emails[0]);
                        user.Username = mail.User;
                    }

                    //Set profile pic
                    if (String.IsNullOrWhiteSpace(user.ProfilePicURL))
                    {
                        user.ProfilePicURL = Utilities.WS_API_URL + "images/profile_pics/profile_default.jpg";
                    }

                    //Create New User (or update existing one)
                    int userId = userJdbc.CreateUserViaSocialNetwork(user, socialNetworkName);
                    if (userId <= 0)
                    {
                        return null;
                    }

                }
                else if (!object.Equals(currentEmail, user.Emails[0]))
                {
                    // Google user ID already exists in the system but email has changed (probably a user changed email on social network).
                    // If new email already exists but has not been confirmed, delete it.
                    userJdbc.RemoveUserIfActivationExpired((string)user.Emails[0]);
                    // Add another email to the user.
                    userJdbc.UpdateSocialNetworkEmail(socialNetworkName, user.SocialNetworkUserId, (string)user.Emails[0]);
                }

                // Return user.
                return userJdbc.GetUserByEmail((string)user.Emails[0]);
            }
            else
            {
                // Credentials did not match.
                return null;
            }
        }

        [HttpPost]
        [Route("register/email")]
        public User CreateUserFromEmail(User user)
        {
            userJdbc.RemoveUserIfActivationExpired((string)user.Emails[0]);

            user.Password = SecurityUtils.CreatePasswordHash(user.Password);
            int userId = userJdbc.CreateUserFromEmail(user);
            if (userId > 0)
            {
                user.Id = userId;
                SendActivationMail(user);
            }
            else
            {
                user = new User();
            }
            return user;
        }

        [HttpPost]
        [Route("login/email")]
        public User LoginUserByEmail(User user)
        {
            userJdbc.RemoveUserIfActivationExpired((string)user.Emails[0]);

            string correctHash = userJdbc.GetUserPasswordByEmail((string)user.Emails[0]);
            if (SecurityUtils.ValidatePassword(user.Password, correctHash))
            {
                return userJdbc.GetUserByEmail((string)user.Emails[0]);
            }
            else
            {
                return null;
            }
        }

        [HttpPost]
        [Route("forgotPassword")]
        public bool RequestPasswordReset([System.Web.Http.FromBody] string email)
        {
            if (userJdbc.GetUserIdByEmail(email) <= 0)
            {
                return false;
            }

            userJdbc.RemoveUserIfActivationExpired(email);

            string requestCode = userJdbc.GenerateOrReturnPasswordResetCode(email);
            if (requestCode != null)
            {
                String emailBody = System.IO.File.ReadAllText(System.Web.HttpContext.Current.Server.MapPath("/EmailTemplates/passwordReset.htm"));
                emailBody = emailBody.Replace("#ResetCode#", requestCode);
                Utilities.sendMail(email, "Password Reset", emailBody);
                return true;
            }
            return false;
        }

        [HttpPost]
        [Route("resetPassword/{code}")]
        public User ResetPassword(User user, string code)
        {
            if (userJdbc.GetUserIdByEmail((string) user.Emails[0]) <= 0)
            {
                return null;
            }

            userJdbc.RemoveUserIfActivationExpired((string)user.Emails[0]);

            user.Password = SecurityUtils.CreatePasswordHash(user.Password);
            if (userJdbc.ResetUserPassword((string)user.Emails[0], code, user.Password))
            {
                return userJdbc.GetUserByEmail((string)user.Emails[0]);
            }
            else
            {
                return null;
            }
        }

        [HttpPost]
        [Route("activationEmail")]
        public void SendActivationMail(User user)
        {
            SimpleAES encryptor = new SimpleAES();
            string activationLink = "http://trivollo.gratwise.com/user/activate/email/" + encryptor.EncryptToString((string) user.Emails[0]);
            String emailBody = System.IO.File.ReadAllText(System.Web.HttpContext.Current.Server.MapPath("/EmailTemplates/activation.htm"));
            emailBody = emailBody.Replace("#UserName#", user.Username);
            emailBody = emailBody.Replace("#ActionString#", activationLink);
            Utilities.sendMail((string)user.Emails[0], "Trivollo Registration", emailBody);
        }

        [HttpPost]
        [Route("updateCountry/{userId}/{countryCode}")]
        public bool UpdateUserCountry(int userId, string countryCode)
        {
            return userJdbc.UpdateUserCountry(userId, countryCode);
        }
    }
}