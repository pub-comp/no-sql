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
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class EntitySetOptionsAttribute : Attribute
    {
        public EntitySetNamingMode NamingMode { get; set; }

        public string ExplicitName { get; set; }

        /// <summary>
        /// <=0 for unlimited
        /// </summary>
        public long MaxSizeBytes { get; set; }

        /// <summary>
        /// <=0 for unlimited
        /// </summary>
        public long MaxEntities { get; set; }
    }
}
