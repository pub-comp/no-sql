using System;
using PubComp.NoSql.Core;

namespace PubComp.NoSql.AdaptorTests.Mock
{
    public class BaseEntity : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }

    public class SpecificEntity : BaseEntity
    {
        public string SpecificData { get; set; }
    }
}
