using System;
using System.IO;

namespace Lucky.Services
{
    public class PersistenceService : ServiceBase
    {
        public string GetAppFolderPath(string root)
        {
            var ret = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            ret = Path.Combine(ret, "Home/" + root);
            if (!Directory.Exists(ret))
            {
                Directory.CreateDirectory(ret);
            }
            return ret;
        }
    }
}
