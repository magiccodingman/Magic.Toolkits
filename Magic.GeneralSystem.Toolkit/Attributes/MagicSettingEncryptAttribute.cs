using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.GeneralSystem.Toolkit.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class MagicSettingEncryptAttribute : Attribute
    {
        public MagicSettingEncryptAttribute()
        {
        }
    }
}
