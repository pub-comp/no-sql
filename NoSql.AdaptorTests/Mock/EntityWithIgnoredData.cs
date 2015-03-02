using System;
using PubComp.NoSql.Core;

namespace PubComp.NoSql.AdaptorTests.Mock
{
    public class EntityWithIgnoredData : IEntity<Guid>
    {
        public Guid Id { get; set; }
        
        public String Name { get; set; }
        
        [DbIgnore]
        public string IgnoreThisString { get; set; }

        [DbIgnore]
        public InnerClass IgnoreThisObject { get; set; }

        [DbIgnore]
        public InnerStruct IgnoreThisStruct { get; set; }
    }

    public class InnerClass
    {
        public int Property { get; set; }
    }

    public struct InnerStruct
    {
        public int Field;
    }
}
