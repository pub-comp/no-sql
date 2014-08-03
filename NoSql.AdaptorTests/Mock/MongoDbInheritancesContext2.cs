using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PubComp.NoSql.Core;
using PubComp.NoSql.MongoDbDriver;

namespace PubComp.NoSql.AdaptorTests.Mock
{
    [KnownDataTypes(typeof(TypeB2), typeof(TypeC2.TypeD))]
    public class MongoDbInheritancesContext2 : MongoDbContext, IInheritancesContext2
    {
        public MongoDbInheritancesContext2(MongoDbConnectionInfo connectionInfo)
            : base(connectionInfo)
        {
        }

        public IEntitySet<Guid, InheritanceBase2> Entities { get; private set; }
    }
}
