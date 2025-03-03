using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magic.GeneralSystem.Toolkit.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class MagicSettingInfoAttribute : Attribute
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public MagicSettingInfoAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
