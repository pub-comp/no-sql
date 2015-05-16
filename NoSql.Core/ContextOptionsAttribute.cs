using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubComp.NoSql.Core
{
    /// <summary>
    /// Only the options supported by the selected DB type will be used
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ContextOptionsAttribute : Attribute
    {
        public EntitySetNamingMode EntitySetDefaultNamingMode { get; set; }
    }
}
