using System.Collections;

namespace TrivolloWebApi.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public ArrayList Emails { get; set; }
        public string CountryCode { get; set; }
        public bool EmailVerified { get; set; }
        public string ProfilePicURL { get; set; }

        public string SocialNetworkIdToken { get; set; }
        public string SocialNetworkUserId { get; set; }
    }
}