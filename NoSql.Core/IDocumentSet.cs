using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace PubComp.NoSql.Core
{
    public interface IDocumentSet : IEntitySet
    {
        
    }

    public interface IDocumentSet<TKey, TEntity> : IEntitySet<TKey, TEntity>, IDocumentSet where TEntity : class, IEntity<TKey>
    {
        void UpdateField(TEntity entity, string fieldName);

        void UpdateFields(TEntity entity, params string[] fieldNames);

        void Update(Expression<Func<TEntity, bool>> queryExpression, params KeyValuePair<string, object>[] propertyValues);

        void Delete(Expression<Func<TEntity, bool>> queryExpression);
    }
}
