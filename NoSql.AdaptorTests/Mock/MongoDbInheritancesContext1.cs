using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PubComp.NoSql.Core;
using PubComp.NoSql.MongoDbDriver;

namespace PubComp.NoSql.AdaptorTests.Mock
{
    [KnownDataTypesResolver(typeof(EntityWithGuid))]
    public class MongoDbInheritancesContext1 : MongoDbContext, IInheritancesContext1
    {
        public MongoDbInheritancesContext1(MongoDbConnectionInfo connectionInfo)
            : base(connectionInfo)
        {
        }

        public IEntitySet<Guid, InheritanceBase1> Entities { get; private set; }
    }
}
