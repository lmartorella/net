using Lucky.Home;
using Lucky.Home.Admin;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Web.Script.Services;
using System.Web.Services;

namespace Web
{
    public class TechnologyData
    {
        public int NodeCount;
        public int DeviceCount;
    }

    [WebService(Namespace = "uri://luckysoft")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ScriptService]
    public class HomeService : WebService
    {
        [WebMethod]
        public async Task<TechnologyData> GetTechnologyData()
        {
            var client = new TcpClient();
            try
            {
                await client.ConnectAsync("localhost", Constants.DefaultAdminPort);

                var adminInterface = new AdminClient(() => client.GetStream(), () => { });
                var nodes = await adminInterface.GetTopology();
                var devices = await adminInterface.GetDevices();
                return new TechnologyData { NodeCount = nodes.Length, DeviceCount = devices.Length };
            }
            catch (Exception exc)
            {
                return new TechnologyData();
            }
        }
    }
}
