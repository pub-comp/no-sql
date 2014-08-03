using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PubComp.NoSql.Core;

namespace PubComp.NoSql.AdaptorTests.Mock
{
    public class MultiIDEntity : IEntity<Guid>
    {
        public Guid Id
        {
            get;
            set;
        }

        public Guid Item1Id
        {
            get;
            set;
        }

        public Guid Item2Id
        {
            get;
            set;
        }

        public MyFlags Flags
        {
            get;
            set;
        }
    }

    [Flags]
    public enum MyFlags
    {
        None = 0,
        One = 1,
        Two = 2,
        Three = 3,
    }
}
