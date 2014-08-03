using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PubComp.NoSql.Core;

namespace PubComp.NoSql.AdaptorTests.Mock
{
    public class EntityForCalc : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public DateTime Date { get; set; }
        public double Money { get; set; }
    }
}
