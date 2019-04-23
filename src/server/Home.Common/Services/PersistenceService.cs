using System;
using System.IO;

namespace Lucky.Services
{
    public class PersistenceService : ServiceBase
    {
        public string GetAppFolderPath(string root = null)
        {
            string wrkDir = Manager.GetService<IConfigurationService>().GetConfig("wrk");
            if (wrkDir == null)
            {
                wrkDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                wrkDir = Path.Combine(wrkDir, "Home");
            }
            if (!string.IsNullOrEmpty(root))
            {
                wrkDir = Path.Combine(wrkDir, root);
            }

            if (!Directory.Exists(wrkDir))
            {
                Directory.CreateDirectory(wrkDir);
            }
            return wrkDir;
        }
    }
}
