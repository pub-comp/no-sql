using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PubComp.NoSql.Core;

namespace PubComp.NoSql.AdaptorTests.Mock
{
    public class EntityWithGuid : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public class EntityWithGuid1 : EntityWithGuid
    {
        public string Prop1 { get; set; }
    }

    public class EntityWithGuid2 : EntityWithGuid
    {
        public string Prop2 { get; set; }
    }
}
