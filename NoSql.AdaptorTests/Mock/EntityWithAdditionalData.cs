using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PubComp.NoSql.Core;

namespace PubComp.NoSql.AdaptorTests.Mock
{
    public class EntityWithAdditionalData : IEntity<Guid>
    {
        public Guid Id { get; set; }
        
        public String Name { get; set; }
        
        [DbIgnore]
        public string AdditionalData { get; set; }
    }
}
