using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PubComp.NoSql.Core;

namespace PubComp.NoSql.AdaptorTests.Mock
{
    public class InheritanceBase2 : IEntity<Guid>
    {
        public Guid Id { get; set; }
    }

    public class TypeB2 : InheritanceBase2
    {
        public String B { get; set; }
    }

    public class TypeC2
    {
        public class TypeD : InheritanceBase2
        {
            public String D { get; set; }

            public class TypeE : InheritanceBase2
            {
                public String E { get; set; }
            }
        }
    }

    public class TypeF2
    {
        public class TypeG
        {
            public class TypeH
            {
            }
        }

        public class TypeI
        {
            public class TypeJ
            {
            }

            public class TypeK : InheritanceBase2
            {
                public String K { get; set; }
            }
        }
    }
}
