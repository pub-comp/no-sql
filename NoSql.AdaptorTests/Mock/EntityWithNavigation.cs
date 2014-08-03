using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PubComp.NoSql.Core;

namespace PubComp.NoSql.AdaptorTests.Mock
{
    public class EntityWithNavigation : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public String Name { get; set; }
        
        public Guid InfoId { get; set; }
        
        [Navigation("InfoId", typeof(InfoBase))]
        public Info Info { get; set; }

        public IEnumerable<Guid> TagIds { get; set; }

        [Navigation("TagIds", typeof(Tag))]
        public IEnumerable<Tag> Tags { get; set; }
    }

    public abstract class InfoBase : IEntity<Guid>
    {
        public Guid Id { get; set; }
    }

    public class Info : InfoBase
    {
        public string Name { get; set; }
    }

    public class Tag : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}
