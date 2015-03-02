using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PubComp.NoSql.Core;
using PubComp.NoSql.MongoDbDriver;

namespace PubComp.NoSql.AdaptorTests.Mock
{
    public class MockMongoDbContext : MongoDbContext, IMockContext
    {
        public MockMongoDbContext(MongoDbConnectionInfo connectionInfo)
            : base(connectionInfo)
        {
        }

        public IEntitySet<Guid, EntityWithGuid> EntitiesWithGuid { get; private set; }

        private static IndexDefinition EntitiesWithGuidByName
        {
            get
            {
                return new IndexDefinition(
                    typeof(EntityWithGuid),
                    new[] { new KeyProperty("Name", Direction.Ascending) },
                    false,
                    true);
            }
        }

        private static IndexDefinition EntitiesForCalcByOwnerId
        {
            get
            {
                return new IndexDefinition(
                    typeof(EntityForCalc),
                    new[] { new KeyProperty("OwnerId", Direction.Ascending) },
                    false,
                    true);
            }
        }

        public IEntitySet<int, EntityWithInt> EntitiesWithInt { get; private set; }

        public IEntitySet<string, EntityWithString> EntitiesWithString { get; private set; }

        public IEntitySet<Guid, MultiIDEntity> MultiIDEntities { get; private set; }

        public IEntitySet<Guid, EntityWithNavigation> EntitiesWithNavigation { get; private set; }

        public IEntitySet<Guid, InfoBase> Infos { get; private set; }

        public IEntitySet<Guid, Tag> Tags { get; private set; }

        public IEntitySet<Guid, EntityForCalc> EntitiesForCalc { get; private set; }

        public IEntitySet<Guid, EntityForUpdates> EntitiesForUpdates { get; private set; }

        public IEntitySet<Guid, InheritanceBase1> InheritanceEntities { get; private set; }

        public IEntitySet<Guid, EntityWithIgnoredData> EntityWithIgnoredData { get; private set; }

        public IEntitySet<Guid, InhertianceEntityWithIgnoredData> InhertianceEntityWithIgnoredData { get; private set; }

        public IEntitySet<Guid, Dates> Dates { get; private set; }

        public new IFileSet<Guid> Files { get; private set; }
    }
}
