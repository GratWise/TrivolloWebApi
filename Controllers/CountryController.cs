using System.Collections;
using System.Web.Http;
using TrivolloWebApi.Jdbc;

namespace TrivolloWebApi.Controllers
{
    [RoutePrefix("country")]
    public class CountryController : ApiController
    {
        private CountryJdbc countryJdbc = new CountryJdbc();

        [System.Web.Mvc.HttpGet]
        [Route("list")]
        public ArrayList getAllCountries()
        {
            return countryJdbc.GetAllCountries();
        }
    }
}
