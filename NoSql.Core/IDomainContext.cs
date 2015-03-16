using System;
using System.Collections.Generic;

namespace PubComp.NoSql.Core
{
    public interface IDomainContext : IDisposable
    {
        IEnumerable<IEntitySet> EntitySets
        {
            get;
        }

        IEntitySet<TKey, TEntity> GetEntitySet<TKey, TEntity>()
            where TEntity : class, IEntity<TKey>;

        IFileSet Files
        {
            get;
        }

#if DEBUG
        void DeleteAll();
#endif
    }
}
