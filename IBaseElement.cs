using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WGP.SFDynamicObject
{
    public interface IBaseElement
    {
        Guid ID { get; }
        string Name { get; }
    }
}