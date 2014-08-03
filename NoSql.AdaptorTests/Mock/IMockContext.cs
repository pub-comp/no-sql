using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PubComp.NoSql.Core;

namespace PubComp.NoSql.AdaptorTests.Mock
{
    public interface IMockContext : IDomainContext
    {
        IEntitySet<Guid, EntityWithGuid> EntitiesWithGuid { get; }
        IEntitySet<int, EntityWithInt> EntitiesWithInt { get; }
        IEntitySet<String, EntityWithString> EntitiesWithString { get; }
        IEntitySet<Guid, MultiIDEntity> MultiIDEntities { get; }
        IEntitySet<Guid, EntityWithAdditionalData> EntitiesWithAdditionalData { get; }
        IEntitySet<Guid, EntityWithNavigation> EntitiesWithNavigation { get; }
        IEntitySet<Guid, InfoBase> Infos { get; }
        IEntitySet<Guid, Tag> Tags { get; }
        IEntitySet<Guid, EntityForCalc> EntitiesForCalc { get; }
        IEntitySet<Guid, EntityForUpdates> EntitiesForUpdates { get; }

        new IFileSet<Guid> Files { get; }
    }
}
