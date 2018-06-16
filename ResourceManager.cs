using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML;

namespace WGP.SFDynamicObject
{
    public class ResourceManager : Dictionary<string, ResourceManager.Resource>
    {
        public struct Resource
        {
            public ObjectBase Data { get; set; }
            public string Path { get; set; }
        }
        public ResourceManager() : base() { }
        public ResourceManager(int capacity) : base(capacity) { }
        public ResourceManager(IEqualityComparer<string> comparer) : base(comparer) { }
        public ResourceManager(IDictionary<string, Resource> dictionary) : base(dictionary) { }
        public ResourceManager(int capacity, IEqualityComparer<string> comparer) : base(capacity, comparer) { }
        public ResourceManager(IDictionary<string, Resource> dictionary, IEqualityComparer<string> comparer) : base(dictionary, comparer) { }

        internal struct Duo
        {
            public string ID { get; set; }
            public string Path { get; set; }
            public string Type { get; set; }
        }

        public void SaveToFile(string path)
        {
            var resourceList = new List<Duo>();
            foreach (var resource in this)
            {
                var duo = new Duo();
                duo.ID = resource.Key;
                var resFile = new Uri(System.IO.Path.GetFullPath(resource.Value.Path));
                var refFile = new Uri(System.IO.Path.GetFullPath(path));
                duo.Path = Uri.UnescapeDataString(refFile.MakeRelativeUri(resFile).OriginalString);
                if (resource.Value.Data is SFML.Graphics.Texture)
                {
                    duo.Type = "Texture";
                    resourceList.Add(duo);
                }
                else if (resource.Value.Data is SFML.Graphics.Font)
                {
                    duo.Type = "Font";
                    resourceList.Add(duo);
                }
                else if (resource.Value.Data is SFML.Audio.SoundBuffer)
                {
                    duo.Type = "SoundBuffer";
                    resourceList.Add(duo);
                }
            }
            var result = Newtonsoft.Json.JsonConvert.SerializeObject(resourceList.ToArray());
            try
            {
                var stream = new System.IO.StreamWriter(path);
                stream.Write(result);
                stream.Close();
            }
            catch (Exception e)
            {
                throw new Exception("Unable to save the ResourceManager to the file \"" + path + "\"", e);
            }
        }

        public void LoadFromFile(string path)
        {
            var reader = new System.IO.StreamReader(path);

            Clear();

            Duo[] array = Newtonsoft.Json.JsonConvert.DeserializeObject<Duo[]>(reader.ReadToEnd());

            foreach (var duo in array)
            {
                if (duo.Type == "Texture")
                {
                    try
                    {
                        SFML.Graphics.Texture tex = new SFML.Graphics.Texture(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), duo.Path));

                        var res = new Resource();
                        res.Path = duo.Path;
                        res.Data = tex;
                        Add(duo.ID, res);
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Unable to load the texture \"" + duo.Path + "\"", e);
                    }
                }
                if (duo.Type == "Font")
                {
                    try
                    {
                        SFML.Graphics.Font tex = new SFML.Graphics.Font(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), duo.Path));

                        var res = new Resource();
                        res.Path = duo.Path;
                        res.Data = tex;
                        Add(duo.ID, res);
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Unable to load the font \"" + duo.Path + "\"", e);
                    }
                }
                if (duo.Type == "SoundBuffer")
                {
                    try
                    {
                        SFML.Audio.SoundBuffer tex = new SFML.Audio.SoundBuffer(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), duo.Path));

                        var res = new Resource();
                        res.Path = duo.Path;
                        res.Data = tex;
                        Add(duo.ID, res);
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Unable to load the sound buffer \"" + duo.Path + "\"", e);
                    }
                }
            }
        }
    }
}
