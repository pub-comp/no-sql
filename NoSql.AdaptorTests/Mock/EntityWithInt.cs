using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PubComp.NoSql.Core;

namespace PubComp.NoSql.AdaptorTests.Mock
{
    public class EntityWithInt : IEntity<int>
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
