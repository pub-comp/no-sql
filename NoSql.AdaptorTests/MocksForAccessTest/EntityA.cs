using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PubComp.NoSql.Core;

namespace PubComp.NoSql.AdaptorTests.MocksForAccessTest
{
    public class EntityA : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public Status Status { get; set; }
        public String Text { get; set; }
    }
}
