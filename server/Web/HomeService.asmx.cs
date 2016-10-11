using System.Web.Script.Services;
using System.Web.Services;

namespace Web
{
    public class Data
    {
        public string Ret = "It really works now.";
    }

    [WebService(Namespace = "uri://luckysoft")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ScriptService]
    public class HomeService : WebService
    {
        [WebMethod]
        public Data GetData()
        {
            return new Data();
        }
    }
}
