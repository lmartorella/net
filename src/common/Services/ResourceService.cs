using System.Globalization;
using System.Reflection;
using System.Resources;

namespace Lucky.Home.Services;

public class ResourceService
{
    private Dictionary<Assembly, ResourceManager> resourceManagers = new Dictionary<Assembly, ResourceManager>();

    public ResourceService()
    {
        var culture = CultureInfo.GetCultureInfo("it-IT");
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }
    
    public string GetString(Type type, string key)
    {
        return GetString(type.Assembly, key);
    }

    public string GetString(Assembly assembly, string key)
    {
        ResourceManager resourceManager;
        if (!resourceManagers.TryGetValue(assembly, out resourceManager)) 
        {
            resourceManager = new ResourceManager("Strings", assembly);
            resourceManagers[assembly] = resourceManager;
        }
        return resourceManager.GetString(key)!;
    }
}