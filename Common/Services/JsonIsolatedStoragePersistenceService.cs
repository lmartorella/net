using System;
using System.IO;
using System.Runtime.Serialization.Json;

namespace Lucky.Services
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class JsonIsolatedStoragePersistenceService : ServiceBase, IPersistenceService
    {
        private string _isolatedStorageFolder;

        public void InitAppRoot(string appRoot)
        {
            _isolatedStorageFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _isolatedStorageFolder = Path.Combine(_isolatedStorageFolder, "Home\\" + appRoot);
            if (!Directory.Exists(_isolatedStorageFolder))
            {
                Directory.CreateDirectory(_isolatedStorageFolder);
            }
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
            serializer.WriteObject(file.OpenWrite(), value);
        }
    }
}