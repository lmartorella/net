using System;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Lucky.Home.Services
{
    /// <summary>
    /// Access isolated storage, using JSON as data language
    /// </summary>
    internal class JsonIsolatedStorageService : ServiceBase, IIsolatedStorageService
    {
        private string _isolatedStorageFolder;

        public void InitAppRoot(string appRoot)
        {
            _isolatedStorageFolder = Manager.GetService<PersistenceService>().GetAppFolderPath(appRoot);
        }

        public T GetState<T>(string serviceName, Func<T> defaultProvider)
        {
            FileInfo file = new FileInfo(Path.Combine(_isolatedStorageFolder, serviceName + ".json"));
            if (file.Exists)
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
                Stream stream = null;
                try
                {
                    stream = file.OpenRead();
                    return (T) serializer.ReadObject(stream);
                }
                catch
                {
                    // Broken file.
                    if (stream != null) stream.Dispose();
                    file.Delete();

                    var state = defaultProvider();
                    SetState(serviceName, state);
                    return state;
                }
                finally
                {
                    if (stream != null) stream.Dispose();
                }
            }
            else
            {
                var state = defaultProvider();
                SetState(serviceName, state);
                return state;
            }
        }

        public void SetState<T>(string serviceName, T value)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
            FileInfo file = new FileInfo(Path.Combine(_isolatedStorageFolder, serviceName + ".json"));
            using (var fileStream = file.Open(FileMode.Create))
            {
                using (var writer = JsonReaderWriterFactory.CreateJsonWriter(fileStream, Encoding.UTF8, true, true))
                {
                    serializer.WriteObject(writer, value);
                }
            }
        }
    }
}