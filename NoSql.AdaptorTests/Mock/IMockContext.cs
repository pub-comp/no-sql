using System;
using PubComp.NoSql.Core;

namespace PubComp.NoSql.AdaptorTests.Mock
{
    public interface IMockContext : IDomainContext
    {
        IEntitySet<Guid, EntityWithGuid> EntitiesWithGuid { get; }
        IEntitySet<int, EntityWithInt> EntitiesWithInt { get; }
        IEntitySet<String, EntityWithString> EntitiesWithString { get; }
        IEntitySet<Guid, MultiIDEntity> MultiIDEntities { get; }
        IEntitySet<Guid, EntityWithNavigation> EntitiesWithNavigation { get; }
        IEntitySet<Guid, InfoBase> Infos { get; }
        IEntitySet<Guid, Tag> Tags { get; }
        IEntitySet<Guid, EntityForCalc> EntitiesForCalc { get; }
        IEntitySet<Guid, EntityForUpdates> EntitiesForUpdates { get; }
        IEntitySet<Guid, EntityWithIgnoredData> EntityWithIgnoredData { get; }
        IEntitySet<Guid, InhertianceEntityWithIgnoredData> InhertianceEntityWithIgnoredData { get; }

        new IFileSet<Guid> Files { get; }
    }
}
