using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PubComp.NoSql.Core;

namespace PubComp.NoSql.AdaptorTests.Mock
{
    public class EntityWithString : IEntity<String>
    {
        public String Id { get; set; }
        public string Name { get; set; }
    }
}
