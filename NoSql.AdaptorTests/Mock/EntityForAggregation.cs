using System;
using PubComp.NoSql.Core;

namespace PubComp.NoSql.AdaptorTests.Mock
{
    public class EntityForAggregation : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public Guid EntityId { get; set; }
        public String Name { get; set; }
        public int Count { get; set; }
    }
}
