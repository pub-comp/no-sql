using System;
using PubComp.NoSql.Core;

namespace PubComp.NoSql.AdaptorTests.Mock
{
    public abstract class InhertianceEntityWithIgnoredData : IEntity<Guid>
    {
        public Guid Id { get; set; }

        public String Name { get; set; }

        [DbIgnore]
        public string IgnoreThisParentString { get; set; }
    }

    public class InhertianceEntityWithIgnoredDataChild : InhertianceEntityWithIgnoredData
    {
        [DbIgnore]
        public string IgnoreThisString { get; set; }

        [DbIgnore]
        public InnerClass IgnoreThisObject { get; set; }

        [DbIgnore]
        public InnerStruct IgnoreThisStruct { get; set; }
    }
}
