using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PubComp.NoSql.Core;

namespace PubComp.NoSql.AdaptorTests.Mock
{
    public class EntityForUpdates : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public int Count { get; set; }
        public string Text { get; set; }
    }
}
