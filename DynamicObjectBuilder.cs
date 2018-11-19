using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WGP.SFDynamicObject
{
    public class DynamicObjectBuilder
    {
        private Dictionary<string, FormatData> templates;
        private Dictionary<Guid, Resource> resources;

        public void LoadObjectTemplate(string name, Stream inputStream)
        {

        }

        public void RemoveObjectTemplate(string name)
        {

        }

        public SFDynamicObject CreateObject(string name)
        {
            return null;
        }
    }
}
