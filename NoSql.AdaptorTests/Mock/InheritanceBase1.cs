using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PubComp.NoSql.Core;

namespace PubComp.NoSql.AdaptorTests.Mock
{
    public class InheritanceBase1 : IEntity<Guid>
    {
        public Guid Id { get; set; }
    }

    public class TypeB1 : InheritanceBase1
    {
        public String B { get; set; }
    }

    public class TypeC1
    {
        public class TypeD : InheritanceBase1
        {
            public String D { get; set; }

            public class TypeE : InheritanceBase1
            {
                public String E { get; set; }
            }
        }
    }

    public class TypeF1
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

            public class TypeK : InheritanceBase1
            {
                public String K { get; set; }
            }
        }
    }
}
