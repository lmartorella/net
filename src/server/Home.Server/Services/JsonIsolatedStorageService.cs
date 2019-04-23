using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Lucky.Services
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class JsonIsolatedStorageService : ServiceBase, IIsolatedStorageService
    {
        private string _isolatedStorageFolder;

        public void InitAppRoot(string appRoot)
        {
            _isolatedStorageFolder = Manager.GetService<PersistenceService>().GetAppFolderPath(appRoot);
        }

        public T GetState<T>(string serviceName) where T : class, new()
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
                    return new T();
                }
                finally
                {
                    if (stream != null) stream.Dispose();
                }
            }
            else
            {
                return new T();
            }
        }

        public void SetState<T>(string serviceName, T value) where T : class
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
            FileInfo file = new FileInfo(Path.Combine(_isolatedStorageFolder, serviceName + ".json"));
            using (var fileStream = file.OpenWrite())
            {
                using (var writer = JsonReaderWriterFactory.CreateJsonWriter(fileStream, Encoding.UTF8, true, true))
                {
                    serializer.WriteObject(writer, value);
                }
            }
        }
    }
}