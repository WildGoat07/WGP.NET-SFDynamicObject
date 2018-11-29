using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WGP.SFDynamicObject
{
    public interface IBaseElement
    {
        #region Public Properties

        Guid ID { get; }
        string Name { get; }

        #endregion Public Properties
    }
}