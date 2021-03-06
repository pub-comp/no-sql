﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using PubComp.NoSql.Core;

namespace PubComp.NoSql.MongoDbDriver
{
    public abstract class MongoDbContext : IDomainContext
    {
        protected readonly MongoDB.Driver.MongoClient innerContext;
        private readonly IEnumerable<IEntitySet> entitySets;
        private readonly IFileSet fileSet;
        protected readonly string db;
        private static readonly ConcurrentDictionary<String, MongoDB.Bson.Serialization.BsonClassMap> Mappers
            = new ConcurrentDictionary<string, MongoDB.Bson.Serialization.BsonClassMap>();

        #region Constructor

        public MongoDbContext(MongoDbConnectionInfo connectionInfo)
            : this(connectionInfo.ConnectionString, connectionInfo.Db)
        {
        }

        public MongoDbContext(string connectionString, string dbName)
        {
            var concreteType = this.GetType();

            this.db = dbName;
            this.innerContext = new MongoDB.Driver.MongoClient(connectionString);
            // ReSharper disable once LocalVariableHidesMember
            var entitySets = new List<IEntitySet>();

            var entitySetProperties =
                concreteType.GetProperties()
                    .Where(p => p.PropertyType.IsGenericType
                        && (p.PropertyType.GetGenericTypeDefinition() == typeof(IEntitySet<,>)
                        || p.PropertyType.GetGenericTypeDefinition() == typeof(IDocumentSet<,>)
                        || p.PropertyType.GetGenericTypeDefinition() == typeof(EntitySet<,>)
                        ));

            var contextAttribute = concreteType.GetCustomAttribute(typeof(ContextOptionsAttribute))
                as ContextOptionsAttribute;

            var contextNamingMode = EntitySetNamingMode.NameByTypeLowerCase;
            if (contextAttribute != null)
                contextNamingMode = contextAttribute.EntitySetDefaultNamingMode;

            foreach (var prop in entitySetProperties)
            {
                var keyType = prop.PropertyType.GetGenericArguments()[0];
                var entityType = prop.PropertyType.GetGenericArguments()[1];

                var createEntitySetMethod = typeof(EntitySet<,>).MakeGenericType(new[] { keyType, entityType })
                    .GetConstructor(
                        BindingFlags.Instance | BindingFlags.NonPublic,
                        null, new[] { typeof(MongoDbContext), typeof(string), typeof(string), typeof(EntitySetOptionsAttribute) }, null);

                var setNamingMode = contextNamingMode;
                var setAttribute = prop.GetCustomAttribute(typeof(EntitySetOptionsAttribute))
                    as EntitySetOptionsAttribute;
                
                if (setAttribute != null)
                {
                    setNamingMode = setAttribute.NamingMode;
                }

                string setName;

                switch (setNamingMode)
                {
                    case EntitySetNamingMode.NameByProperty:
                        setName = prop.Name;
                        break;
                    case EntitySetNamingMode.NameByPropertyLowerCase:
                        setName = prop.Name.ToLower();
                        break;
                    case EntitySetNamingMode.NameByType:
                        setName = entityType.Name;
                        break;
                    default:
                        setName = entityType.Name.ToLower();
                        break;
                }

                if (setAttribute != null && !string.IsNullOrEmpty(setAttribute.ExplicitName))
                    setName = setAttribute.ExplicitName;

                var entitySet = createEntitySetMethod.Invoke(new object[] { this, this.db, setName, setAttribute });
                prop.SetValue(this, entitySet, new object[] { });

                entitySets.Add(entitySet as IEntitySet);
            }

            this.entitySets = entitySets;

            var knownTypesResolverAttrs = concreteType.GetCustomAttributes(typeof(KnownDataTypesResolverAttribute));
            foreach (KnownDataTypesResolverAttribute knownTypesResolverAttr in knownTypesResolverAttrs)
            {
                foreach (var type in knownTypesResolverAttr.TypesToUseForSearchingAssemblies)
                {
                    var concreteEntityTypes =
                        ContextUtils.FindInheritingTypes(type.Assembly, entitySets.Select(s => s.EntityType));

                    RegisterKnownTypes(concreteEntityTypes);
                }
            }

            var knownTypesAttrs = concreteType.GetCustomAttributes(typeof(KnownDataTypesAttribute));
            foreach (KnownDataTypesAttribute knownTypesAttr in knownTypesAttrs)
            {
                RegisterKnownTypes(knownTypesAttr.Types);
            }

            var fileSetProperty = concreteType.GetProperties()
                                        .Where(p => p.PropertyType.IsGenericType
                                            && p.PropertyType.GetGenericTypeDefinition() == typeof(IFileSet<>))
                                        .SingleOrDefault();

            if (fileSetProperty != null)
            {
                var keyType = fileSetProperty.PropertyType.GetGenericArguments()[0];

                var createFileSetMethod = typeof(FileSet<>).MakeGenericType(new[] { keyType })
                    .GetConstructor(
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null, new[] { typeof(MongoDbContext), typeof(string) }, null);

                // ReSharper disable once LocalVariableHidesMember
                var fileSet = createFileSetMethod.Invoke(new object[] { this, this.db });
                fileSetProperty.SetValue(this, fileSet, new object[] { });

                this.fileSet = fileSet as IFileSet;
            }
        }

        #endregion

        private void RegisterKnownTypes(Type[] types)
        {
            foreach (var type in types)
                MongoDB.Bson.Serialization.BsonClassMap.LookupClassMap(type);
        }

        public void Dispose()
        {
            // The official driver maintains a connection pool internally.
            // You do not need to dispose of any connections or even establish new connections.
        }

        public IEnumerable<IEntitySet> EntitySets
        {
            get
            {
                return this.entitySets;
            }
        }

        public IEntitySet<TKey, TEntity> GetEntitySet<TKey, TEntity>() where TEntity : class, IEntity<TKey>
        {
            var set = this.entitySets.Where(s => s.KeyType == typeof(TKey) && s.EntityType == typeof(TEntity))
                .FirstOrDefault() as IEntitySet<TKey, TEntity>;

            return set;
        }

        public IFileSet Files
        {
            get
            {
                return this.fileSet;
            }
        }

        public void DeleteAll()
        {
            foreach (var entitySet in this.entitySets)
            {
                if (!((EntitySet)entitySet).IsCapped)
                    ((EntitySet)entitySet).DeleteAll();
            }
        }

#if DEBUG
        public void SuperDeleteAll()
        {
            var database = MongoDB.Driver.MongoClientExtensions.GetServer(this.innerContext).GetDatabase(this.db);
            var sets = database.GetCollectionNames();

            foreach (var set in sets)
            {
                var collection = database.GetCollection(set);
                if (!collection.IsCapped())
                    collection.RemoveAll();
            }
        }
#endif
        public void UpdateIndexes(bool removeStaleIndexes)
        {
            foreach (var entitySet in entitySets)
                ((EntitySet)entitySet).UpdateIndexes(removeStaleIndexes);
        }

        internal MongoDB.Driver.MongoClient InnerContext
        {
            get
            {
                return this.innerContext;
            }
        }

        private static MongoDB.Bson.BsonValue IdToKey<TKey>(TKey key)
        {
            var result = MongoDB.Bson.BsonValue.Create(key);
            return result;
        }

        #region Convert Expressions

        protected internal static MongoDB.Driver.IMongoQuery ToMongoQuery<TEntity>(
            Expression<Func<TEntity, bool>> queryExpression)
        {
            var query = (queryExpression != null)
                ? new MongoDB.Driver.Builders.QueryBuilder<TEntity>().Where(queryExpression)
                : null;

            return query;
        }

        protected internal static MongoDB.Driver.IMongoSortBy ToMongoSortBy<TEntity>(
            Expression<Func<TEntity, object>> sortExpression, bool ascending = true)
        {
            var sortBy = (sortExpression != null)
                ? ((ascending)
                    ? new MongoDB.Driver.Builders.SortByBuilder<TEntity>().Ascending(sortExpression)
                    : new MongoDB.Driver.Builders.SortByBuilder<TEntity>().Descending(sortExpression))
                : null;

            return sortBy;
        }

        #endregion

        #region GetEntitySet

        public IEntitySet<TKey, TEntity> GetEntitySet<TKey, TEntity>(string collectionName)
            where TEntity : class, IEntity<TKey>
        {
            var entitySet = new EntitySet<TKey, TEntity>(this, this.db, collectionName);
            return entitySet;
        }

        public IEntitySet<TKey, TEntity> GetEntitySet<TKey, TEntity>(string dbName, string collectionName)
            where TEntity : class, IEntity<TKey>
        {
            var entitySet = new EntitySet<TKey, TEntity>(this, dbName, collectionName);
            return entitySet;
        }

        public IEntitySet<TKey, TEntity> GetEntitySet<TKey, TEntity>(string collectionName, EntitySetOptionsAttribute options)
            where TEntity : class, IEntity<TKey>
        {
            var entitySet = new EntitySet<TKey, TEntity>(this, this.db, collectionName, options);
            return entitySet;
        }

        public IEntitySet<TKey, TEntity> GetEntitySet<TKey, TEntity>(string dbName, string collectionName, EntitySetOptionsAttribute options)
            where TEntity : class, IEntity<TKey>
        {
            var entitySet = new EntitySet<TKey, TEntity>(this, dbName, collectionName, options);
            return entitySet;
        }

        #endregion

        #region MapReduce

        public void MapReduce<TEntity, TResult>(
            string collectionName,
            Expression<Func<TEntity, bool>> queryExpression,
            string mapFunction, string reduceFunction, string finalizeFunction,
            bool doGetResults, out IEnumerable<TResult> results,
            ReduceStoreMode storeMode = ReduceStoreMode.None,
            string resultSet = null, string resultDbName = null,
            Expression<Func<TEntity, Object>> sortByExpression = null,
            ReduceOptions options = null)
        {
            var collection = MongoDB.Driver.MongoClientExtensions.GetServer(this.innerContext).GetDatabase(db)
                .GetCollection<TEntity>(collectionName);

            var query = ToMongoQuery(queryExpression);
            var sortBy = ToMongoSortBy(sortByExpression);

            MapReduceInner(
                collection, query, sortBy,
                mapFunction, reduceFunction, finalizeFunction,
                doGetResults, out results, storeMode, resultSet, resultDbName, options);
        }

        private static void MapReduceInner<TResult>(
            MongoDB.Driver.MongoCollection collection,
            MongoDB.Driver.IMongoQuery query, MongoDB.Driver.IMongoSortBy sortBy,
            string mapFunction, string reduceFunction, string finalizeFunction,
            bool doGetResults, out IEnumerable<TResult> results,
            ReduceStoreMode storeMode, string resultSet, string resultDbName,
            ReduceOptions options = null)
        {
            if (string.IsNullOrEmpty(resultSet))
                storeMode = ReduceStoreMode.None;

            MongoDB.Driver.MapReduceOutputMode outputMode;
            String outputCollectionName;

            switch (storeMode)
            {
                case ReduceStoreMode.NewSet:
                    outputMode = MongoDB.Driver.MapReduceOutputMode.Replace;
                    outputCollectionName = resultSet;
                    break;
                case ReduceStoreMode.ReplaceItems:
                    outputMode = MongoDB.Driver.MapReduceOutputMode.Merge;
                    outputCollectionName = resultSet;
                    break;
                case ReduceStoreMode.Combine:
                    outputMode = MongoDB.Driver.MapReduceOutputMode.Reduce;
                    outputCollectionName = resultSet;
                    break;
                default:
                    outputMode = MongoDB.Driver.MapReduceOutputMode.Inline;
                    outputCollectionName = null;
                    break;
            }

            var args = new MongoDB.Driver.MapReduceArgs
            {
                MapFunction = mapFunction,
                ReduceFunction = reduceFunction,
                OutputMode = outputMode,
                OutputCollectionName = outputCollectionName,
                JsMode = options != null && options.DoUseJsMode,
            };

            if (query != null)
                args.Query = query;

            if (finalizeFunction != null)
                args.FinalizeFunction = finalizeFunction;

            if (resultDbName != null)
                args.OutputDatabaseName = resultDbName;

            if (sortBy != null)
                args.SortBy = sortBy;

            var reductionResults = collection.MapReduce(args);

            if (!string.IsNullOrEmpty(reductionResults.ErrorMessage))
                throw new DalFailure(reductionResults.ErrorMessage, DalOperation.Reduce);

            results = (doGetResults) ?
                    ((storeMode == ReduceStoreMode.None)
                        ? reductionResults.GetInlineResultsAs<TResult>()
                        : reductionResults.GetResultsAs<TResult>())
                    : null;
        }

        #endregion

        #region Aggregate

        public void Aggregate<TEntity, TResult>(
            string collectionName,
            AggregateOuputMode outputMode,
            bool doGetResults, out IEnumerable<TResult> results,
            IEnumerable<AggregateStep> pipelineSteps,
            AggregateOptions options = null)
        {
            var collection = MongoDB.Driver.MongoClientExtensions.GetServer(this.innerContext)
                .GetDatabase(db)
                .GetCollection<TEntity>(collectionName);

            AggregateInner(
                collection, outputMode, doGetResults, out results,
                pipelineSteps, options);
        }

        private static void AggregateInner<TResult>(
            MongoDB.Driver.MongoCollection collection,
            AggregateOuputMode outputMode, bool doGetResults, out IEnumerable<TResult> results,
            IEnumerable<AggregateStep> pipelineSteps,
            AggregateOptions options = null)
        {
            var pipelineStages = pipelineSteps.Select(step => step.BsonDocument).ToList();

            var args = new MongoDB.Driver.AggregateArgs
            {
                AllowDiskUse = options != null ? options.DoAllowDiskUsage : null,
                BatchSize = options != null ? options.BatchSize : null,
                MaxTime = options != null ? options.Timeout : null,
                OutputMode = (outputMode == AggregateOuputMode.Inline
                                ? MongoDB.Driver.AggregateOutputMode.Inline
                                : MongoDB.Driver.AggregateOutputMode.Cursor),
                Pipeline = pipelineStages,
            };

            var aggregationResults = collection.Aggregate(args);

            results = doGetResults
                ? aggregationResults.Select(d =>
                    MongoDB.Bson.Serialization.BsonSerializer.Deserialize<TResult>(d))
                : null;
        }

        #endregion

        #region EntitySet

        public abstract class EntitySet
        {
            internal abstract void UpdateIndexes(bool removeStaleIndexes);
            internal abstract void UpdateIndexes(bool removeStaleIndexes, bool inForeground);
            public abstract IndexDefinition[] GetIndexes();
            public abstract void DeleteAll();
            internal abstract bool IsCapped { get; }
        }

        public class EntitySet<TKey, TEntity> : EntitySet, IDocumentSet<TKey, TEntity>
            where TEntity : class, IEntity<TKey>
        {
            // ReSharper disable once StaticMemberInGenericType
            protected static readonly List<PropertyInfo> UpdatableProperties;
            protected readonly MongoDbContext parent;
            protected readonly MongoDB.Driver.MongoCollection<TEntity> innerSet;
            protected readonly Type parentType;
            protected readonly bool isCapped;
            
            static EntitySet()
            {
                UpdatableProperties = new List<PropertyInfo>();

                var mapper = GetMapper(typeof(TEntity));

                var properties = ContextUtils.GetProperiesOfTypeAndSubTypes(typeof(TEntity));

                foreach (var prop in properties)
                {
                    if (prop.Name == "Id" || !prop.CanRead || !prop.CanWrite || prop.GetIndexParameters().Any())
                        continue;

                    var propType = prop.DeclaringType;
                    var currentMapper = GetMapper(propType);

                    if (prop.GetCustomAttributes(typeof(DbIgnoreAttribute), true).Any()
                        || prop.GetCustomAttributes(typeof(NavigationAttribute), true).Any())
                    {
                        currentMapper.UnmapProperty(prop.Name);
                        continue;
                    }

                    if (prop.PropertyType == typeof(DateTime))
                    {
                        currentMapper.GetMemberMap(prop.Name).SetSerializer(
                            new MongoDB.Bson.Serialization.Serializers.DateTimeSerializer(
                                DateTimeKind.Local));
                    }
                    else if (prop.PropertyType == typeof(DateTime?))
                    {
                        currentMapper.GetMemberMap(prop.Name).SetSerializer(
                            new MongoDB.Bson.Serialization.Serializers.NullableSerializer<DateTime>(
                                new MongoDB.Bson.Serialization.Serializers.DateTimeSerializer(
                                    DateTimeKind.Local)));
                    }

                    UpdatableProperties.Add(prop);
                }
            }

            private static MongoDB.Bson.Serialization.BsonClassMap GetMapper(Type type)
            {
                return Mappers.GetOrAdd(type.FullName, k => GetMapperInner(type));
            }

            private static MongoDB.Bson.Serialization.BsonClassMap GetMapperInner(Type type)
            {
                var genericMethod = typeof(EntitySet<TKey, TEntity>).GetMethod("GenericGetMapper",
                    BindingFlags.NonPublic | BindingFlags.Static);

                var specificMethod = genericMethod.MakeGenericMethod(type);
                var result = specificMethod.Invoke(null, new object[0]);
                return result as MongoDB.Bson.Serialization.BsonClassMap;
            }

            // ReSharper disable once UnusedMember.Local
            private static MongoDB.Bson.Serialization.BsonClassMap GenericGetMapper<T>()
            {
                MongoDB.Bson.Serialization.BsonClassMap mapper;

                if (!MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(T)))
                {
                    mapper = MongoDB.Bson.Serialization.BsonClassMap.RegisterClassMap<T>(cm =>
                    {
                        cm.AutoMap();
                    });
                }
                else
                {
                    mapper = MongoDB.Bson.Serialization.BsonClassMap.GetRegisteredClassMaps()
                        .First(cm => cm.ClassType == typeof(T));
                }

                return mapper;
            }

            internal EntitySet(MongoDbContext parent, string db, string collectionName)
                : this(parent, db, collectionName, null, null)
            {
            }

            internal EntitySet(MongoDbContext parent, string db, string collectionName, EntitySetOptionsAttribute options)
                : this(parent, db, collectionName,
                    options != null ? options.MaxSizeBytes : (long?)null,
                    options != null ? options.MaxEntities : (long?)null)
            {
            }

            internal EntitySet(MongoDbContext parent, string db, string collectionName, long? maxBytes, long? maxCount)
            {
                var mongoDatabase = MongoDB.Driver.MongoClientExtensions.GetServer(parent.innerContext)
                    .GetDatabase(db);

                if (maxBytes != null && maxBytes > 0 || maxCount != null && maxCount > 0)
                {
                    isCapped = true;

                    if (!mongoDatabase.CollectionExists(collectionName))
                    {

                        MongoDB.Driver.Builders.CollectionOptionsBuilder options = null;

                        if (maxBytes != null && maxBytes > 0)
                        {
                            options = MongoDB.Driver.Builders.CollectionOptions.SetCapped(true)
                                .SetMaxSize(maxBytes.Value);
                        }

                        if (maxCount != null && maxCount > 0)
                        {
                            if (options != null)
                                options = options.SetMaxDocuments(maxCount.Value);
                            else
                                options = MongoDB.Driver.Builders.CollectionOptions.SetMaxDocuments(maxCount.Value);
                        }
                        
                        mongoDatabase.CreateCollection(collectionName, options);
                    }
                }

                this.parent = parent;
                this.innerSet = mongoDatabase.GetCollection<TEntity>(collectionName);
                this.parentType = parent.GetType();
            }

            internal override bool IsCapped { get { return isCapped; } }

            internal override void UpdateIndexes(bool removeStaleIndexes)
            {
                UpdateIndexes(removeStaleIndexes, false);
            }

            internal override void UpdateIndexes(bool removeStaleIndexes, bool inForeground)
            {
                var indexDefinitions = new List<IndexDefinition>();

                foreach (var prop in parentType.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!prop.CanRead || prop.PropertyType != typeof(IndexDefinition) || prop.GetIndexParameters().Any())
                        continue;

                    var indexDefinition = prop.GetValue(null, new object[] { }) as IndexDefinition;
                    if (indexDefinition.EntityType != typeof(TEntity))
                        continue;

                    indexDefinitions.Add(indexDefinition);
                }

                if (removeStaleIndexes)
                {
                    var staleIndexes = this.innerSet.GetIndexes().Where(
                        i => i.Name != "_id_" && indexDefinitions.All(d => IsEqualToIndex(d, i) == false)).ToList();

                    foreach (var index in staleIndexes)
                        this.innerSet.DropIndex(index.Key);
                }

                foreach (var indexDefinition in indexDefinitions)
                    this.CreateIndex(indexDefinition, false);
            }

            public void AddIndex(IndexDefinition indexDefinition)
            {
                AddIndex(indexDefinition, false);
            }

            public void AddIndex(IndexDefinition indexDefinition, bool inForeground)
            {
                this.CreateIndex(indexDefinition, inForeground);
            }

            private bool IsEqualToIndex(IndexDefinition indexDefintion, MongoDB.Driver.IndexInfo indexInfo)
            {
                var names1 = indexDefintion.Fields.Select(f => f.Name).ToList();
                var names2 = indexInfo.Key.Elements.Select(e => e.Name).ToList();

                if (names1.Count != names2.Count)
                    return false;

                for (int cnt = 0; cnt < names1.Count; cnt++)
                {
                    if (names1[cnt] != names2[cnt])
                        return false;
                }

                return true;
            }

            public Type KeyType
            {
                get
                {
                    return typeof(TKey);
                }
            }

            public Type EntityType
            {
                get
                {
                    return typeof(TEntity);
                }
            }

            public IQueryable<TEntity> AsQueryable()
            {
                return MongoDB.Driver.Linq.LinqExtensionMethods.AsQueryable(this.innerSet);
            }

            IQueryable<IEntity> IEntitySet.AsQueryable()
            {
                return this.AsQueryable();
            }

            public IEnumerable<TEntity> Query(Expression<Func<TEntity, bool>> filter)
            {
                var query = ToMongoQuery(filter);
                return this.innerSet.Find(query);
            }

            public bool AddIfNotExists(TEntity entity)
            {
                if (EqualityComparer<TKey>.Default.Equals(entity.Id, default(TKey)))
                    throw new DalNullIdFailure("Could not add entity - entity.Id is undefined.", entity, DalOperation.Add);

                var doesContain = Contains(entity.Id);
                if (doesContain)
                    return false;

                CheckIfCanModify(entity);

                Add(entity);
                return true;
            }

            bool IEntitySet.AddIfNotExists(IEntity entity)
            {
                if (entity is TEntity == false)
                    throw new InvalidOperationFailure();

                return this.AddIfNotExists((TEntity)entity);
            }

            public void AddOrUpdate(TEntity entity)
            {
                if (EqualityComparer<TKey>.Default.Equals(entity.Id, default(TKey)))
                    throw new DalNullIdFailure("Could not add entity - entity.Id is undefined.", entity, DalOperation.Add);

                CheckIfCanModify(entity);

                var result = this.innerSet.Save(entity);
                if (result.HasLastErrorMessage)
                    throw new DalFailure(result.LastErrorMessage, entity, DalOperation.Add);
            }

            void IEntitySet.AddOrUpdate(IEntity entity)
            {
                if (entity is TEntity == false)
                    throw new InvalidOperationFailure();

                this.AddOrUpdate((TEntity)entity);
            }

            public TEntity GetOrAdd(TEntity entity)
            {
                if (EqualityComparer<TKey>.Default.Equals(entity.Id, default(TKey)))
                    throw new DalNullIdFailure("Could not add entity - entity.Id is undefined.", entity, DalOperation.Add);

                var existing = Get(entity.Id);

                if (existing != null)
                    return ReturnAfterCheck(existing);

                CheckIfCanModify(entity);
                Add(entity);
                return null;
            }

            IEntity IEntitySet.GetOrAdd(IEntity entity)
            {
                if (entity is TEntity == false)
                    throw new InvalidOperationException();

                return this.GetOrAdd((TEntity)entity);
            }

            public void Add(TEntity entity)
            {
                if (EqualityComparer<TKey>.Default.Equals(entity.Id, default(TKey)))
                    throw new DalNullIdFailure("Could not add entity - entity.Id is undefined.", entity, DalOperation.Add);

                CheckIfCanModify(entity);

                var result = this.innerSet.Insert(entity);
                if (result.HasLastErrorMessage)
                    throw new DalFailure(result.LastErrorMessage, entity, DalOperation.Add);
            }

            void IEntitySet.Add(IEntity entity)
            {
                if (entity is TEntity == false)
                    throw new InvalidOperationException();

                this.Add((TEntity)entity);
            }

            public void Add(IEnumerable<TEntity> entities)
            {
                if (!entities.Any())
                    return;

                if (entities.Any(entity => EqualityComparer<TKey>.Default.Equals(entity.Id, default(TKey))))
                    throw new DalNullIdFailure("Could not add entities - entity.Id is undefined for at least one entity.", default(TEntity), DalOperation.Add);

                CheckIfCanModify(entities);

                var results = this.innerSet.InsertBatch(entities);
                var hasError = results.Select(r => r.HasLastErrorMessage).Aggregate((a, b) => a || b);
                if (hasError)
                {
                    var errors = string.Join(Environment.NewLine, results.Select(r => r.LastErrorMessage));
                    throw new DalFailure(errors, entities, DalOperation.Add);
                }
            }

            void IEntitySet.Add(IEnumerable<IEntity> entities)
            {
                var typedEntities = entities.OfType<TEntity>().ToList();
                if (typedEntities.Count < entities.Count())
                    throw new InvalidOperationFailure();

                this.Add(typedEntities);
            }

            public TEntity Get(TKey key)
            {
                return ReturnAfterCheck(this.innerSet.FindOneById(IdToKey(key)));
            }

            IEntity IEntitySet.Get(Object key)
            {
                if (key is TKey == false)
                    throw new InvalidOperationFailure();

                return this.Get((TKey)key);
            }

            public bool Contains(TKey key)
            {
                return this.innerSet.Find(GetQueryById(key)).Any();
            }

            bool IEntitySet.Contains(Object key)
            {
                if (key is TKey == false)
                    throw new InvalidOperationFailure();

                return this.Contains((TKey)key);
            }

            public IEnumerable<TEntity> Get(IEnumerable<TKey> keys)
            {
                if (!keys.Any())
                    return new TEntity[0];

                return Filter(AsQueryable().Where(entity => keys.Contains(entity.Id)));
            }

            IEnumerable<IEntity> IEntitySet.Get(IEnumerable keys)
            {
                var typedKeys = keys.OfType<TKey>().ToList();
                return this.Get(typedKeys);
            }

            public void Update(TEntity entity)
            {
                if (EqualityComparer<TKey>.Default.Equals(entity.Id, default(TKey)))
                    throw new DalNullIdFailure("Could not update entity - entity.Id is undefined.", entity, DalOperation.Update);

                if (!Contains(entity.Id))
                    throw new DalItemNotFoundFailure("Could not update entity - entity not found in DB.", entity, DalOperation.Update);

                CheckIfCanModify(entity);

                UpdateExisting(entity);
            }

            public void Update(
                Expression<Func<TEntity, bool>> queryExpression, params KeyValuePair<string, object>[] propertyValues)
            {
                if (this.OnModifying != null)
                    throw new DalAccessRestrictionFailure("Can not run query based update when OnModifying event handler is set");

                var mongoQuery = ToMongoQuery(queryExpression);

                var result = this.innerSet.Update(
                    mongoQuery,
                    GetUpdateFields(propertyValues),
                    MongoDB.Driver.UpdateFlags.Multi);

                if (result.HasLastErrorMessage)
                    throw new DalFailure(result.LastErrorMessage, default(TEntity), DalOperation.Update);
            }

            void IEntitySet.Update(IEntity entity)
            {
                if (entity is TEntity == false)
                    throw new InvalidOperationFailure();

                this.Update((TEntity)entity);
            }

            private MongoDB.Driver.IMongoQuery GetQueryById(TKey key)
            {
                return MongoDB.Driver.Builders.Query<TEntity>.EQ(entity => entity.Id, key);
            }

            private MongoDB.Driver.IMongoSortBy GetSortById()
            {
                return MongoDB.Driver.Builders.SortBy<TEntity>.Ascending(EntitySet => EntitySet.Id);
            }

            private MongoDB.Bson.BsonValue ToMongoBsonValue(object objValue)
            {
                if (objValue == null)
                    return MongoDB.Bson.BsonNull.Value;

                MongoDB.Bson.BsonValue value;
                try
                {
                    // This only works for specific types, no option to check
                    if (MongoDB.Bson.BsonTypeMapper.TryMapToBsonValue(objValue, out value))
                        return value;
                }
                catch (ArgumentException)
                {
                    value = null;
                }

                // For all other types
                value = MongoDB.Bson.BsonExtensionMethods.ToBsonDocument(objValue);

                return value;
            }

            private MongoDB.Driver.IMongoUpdate GetUpdateAllButId(TEntity entity)
            {
                MongoDB.Driver.Builders.UpdateBuilder updateBuilder = null;

                foreach (var prop in UpdatableProperties)
                {
                    var declaringType = prop.DeclaringType;
                    var currentType = entity.GetType();
                    if (currentType != declaringType && !currentType.IsSubclassOf(declaringType))
                        continue;

                    var name = prop.Name;
                    var objValue = prop.GetValue(entity, new object[] { });
                    var value = ToMongoBsonValue(objValue);

                    if (updateBuilder == null)
                    {
                        updateBuilder = MongoDB.Driver.Builders.Update.Set(name, value);
                    }
                    else
                    {
                        updateBuilder.Set(name, value);
                    }
                }

                return updateBuilder;
            }

            private MongoDB.Driver.IMongoUpdate GetUpdateField(TEntity entity, string fieldName)
            {
                var prop = UpdatableProperties.FirstOrDefault(p => p.Name == fieldName);

                if (prop == null)
                    throw new DalFailure("Field " + fieldName + " does not exist in collection");

                var objValue = prop.GetValue(entity, new object[] { });
                var value = ToMongoBsonValue(objValue);

                var updateBuilder = MongoDB.Driver.Builders.Update.Set(fieldName, value);

                return updateBuilder;
            }

            private MongoDB.Driver.IMongoUpdate GetUpdateFields(TEntity entity, string[] fieldNames)
            {
                var setters = new MongoDB.Driver.Builders.UpdateBuilder[fieldNames.Length];

                for (int cnt = 0; cnt < fieldNames.Length; cnt++)
                {
                    var fieldName = fieldNames[cnt];

                    var prop = UpdatableProperties.FirstOrDefault(p => p.Name == fieldName);

                    if (prop == null)
                        throw new DalFailure("Field " + fieldName + " does not exist in collection");

                    var objValue = prop.GetValue(entity, new object[] { });
                    var value = ToMongoBsonValue(objValue);

                    setters[cnt] = MongoDB.Driver.Builders.Update.Set(fieldName, value);
                }

                var updateBuilder = MongoDB.Driver.Builders.Update.Combine(setters);

                return updateBuilder;
            }

            private MongoDB.Driver.IMongoUpdate GetUpdateFields(KeyValuePair<string, object>[] propertyValues)
            {
                var setters = new MongoDB.Driver.Builders.UpdateBuilder[propertyValues.Length];

                for (int cnt = 0; cnt < propertyValues.Length; cnt++)
                {
                    var fieldName = propertyValues[cnt].Key;

                    var prop = UpdatableProperties.FirstOrDefault(p => p.Name == fieldName);

                    if (prop == null)
                        throw new DalFailure("Field " + fieldName + " does not exist in collection");

                    var objValue = propertyValues[cnt].Value;
                    var value = ToMongoBsonValue(objValue);

                    setters[cnt] = MongoDB.Driver.Builders.Update.Set(fieldName, value);
                }

                var updateBuilder = MongoDB.Driver.Builders.Update.Combine(setters);

                return updateBuilder;
            }

            private MongoDB.Driver.IMongoUpdate GetIncrementField(string fieldName, long increment)
            {
                var prop = UpdatableProperties.FirstOrDefault(p => p.Name == fieldName);

                if (prop == null)
                    throw new DalFailure("Field " + fieldName + " does not exist in collection");

                var updateBuilder = MongoDB.Driver.Builders.Update.Inc(fieldName, increment);

                return updateBuilder;
            }

            private void UpdateExisting(TEntity entity)
            {
                var result = this.innerSet.Update(
                    GetQueryById(entity.Id),
                    GetUpdateAllButId(entity));

                if (result.HasLastErrorMessage)
                    throw new DalFailure(result.LastErrorMessage, default(TEntity), DalOperation.Update);
            }

            public void Update(IEnumerable<TEntity> entities)
            {
                CheckIfCanModify(entities);

                foreach (var entity in entities)
                    Update(entity);
            }

            public void UpdateField(TEntity entity, string fieldName)
            {
                CheckIfCanModify(entity);

                var result = this.innerSet.Update(
                    GetQueryById(entity.Id),
                    GetUpdateField(entity, fieldName));

                if (result.HasLastErrorMessage)
                    throw new DalFailure(result.LastErrorMessage, default(TEntity), DalOperation.Update);
            }

            public void UpdateFields(TEntity entity, params string[] fieldNames)
            {
                CheckIfCanModify(entity);

                var result = this.innerSet.Update(
                    GetQueryById(entity.Id),
                    GetUpdateFields(entity, fieldNames));

                if (result.HasLastErrorMessage)
                    throw new DalFailure(result.LastErrorMessage, default(TEntity), DalOperation.Update);
            }

            public void IncrementField(TKey key, string fieldName, long increment)
            {
                if (OnModifying != null)
                {
                    var entity = Get(key);
                    CheckIfCanModify(entity);
                }

                var result = this.innerSet.Update(
                    GetQueryById(key),
                    GetIncrementField(fieldName, increment));

                if (result.HasLastErrorMessage)
                    throw new DalFailure(result.LastErrorMessage, default(TEntity), DalOperation.Update);
            }

            void IEntitySet.Update(IEnumerable<IEntity> entities)
            {
                var typedEntities = entities.OfType<TEntity>().ToList();
                if (typedEntities.Count < entities.Count())
                    throw new InvalidOperationFailure();

                this.Update(typedEntities);
            }

            public void Delete(TEntity entity)
            {
                CheckIfCanDelete(entity);
                this.DeleteInner(entity.Id);
            }

            void IEntitySet.Delete(IEntity entity)
            {
                if (entity is TEntity == false)
                    throw new InvalidOperationFailure();

                this.Delete((TEntity)entity);
            }

            public void Delete(IEnumerable<TEntity> entities)
            {
                if (!entities.Any())
                    return;

                CheckIfCanDelete(entities);

                var keys = entities.Select(e => e.Id);
                this.DeleteInner(keys);
            }

            public void Delete(
                Expression<Func<TEntity, bool>> queryExpression)
            {
                if (this.OnDeleting != null)
                    throw new DalAccessRestrictionFailure("Can not run query based delete when OnDeleting event handler is set");

                var mongoQuery = ToMongoQuery(queryExpression);

                var result = this.innerSet.Remove(mongoQuery);

                if (result.HasLastErrorMessage)
                    throw new DalFailure(result.LastErrorMessage, default(TEntity), DalOperation.Update);
            }

            void IEntitySet.Delete(IEnumerable<IEntity> entities)
            {
                var typedEntities = entities.OfType<TEntity>().ToList();
                if (typedEntities.Count < entities.Count())
                    throw new InvalidOperationFailure();

                this.Delete(typedEntities);
            }

            public void Delete(TKey key)
            {
                if (EqualityComparer<TKey>.Default.Equals(key, default(TKey)))
                    throw new DalNullIdFailure("Could not delete entity - Id was null.", default(TEntity), DalOperation.Delete);

                if (OnDeleting != null)
                {
                    var entity = Get(key);
                    CheckIfCanDelete(entity);
                }

                var result = this.innerSet.Remove(GetQueryById(key));
                if (result.HasLastErrorMessage)
                    throw new DalFailure(result.LastErrorMessage, default(TEntity), DalOperation.Delete);
            }

            private void DeleteInner(TKey key)
            {
                if (EqualityComparer<TKey>.Default.Equals(key, default(TKey)))
                    throw new DalNullIdFailure("Could not delete entity - Id was null.", default(TEntity), DalOperation.Delete);

                var result = this.innerSet.Remove(GetQueryById(key));
                if (result.HasLastErrorMessage)
                    throw new DalFailure(result.LastErrorMessage, default(TEntity), DalOperation.Delete);
            }

            void IEntitySet.Delete(Object key)
            {
                if (key is TKey == false)
                    throw new InvalidOperationFailure();

                this.Delete((TKey)key);
            }

            public void Delete(IEnumerable<TKey> keys)
            {
                if (!keys.Any())
                    return;

                if (keys.Any(key => EqualityComparer<TKey>.Default.Equals(key, default(TKey))))
                    throw new DalNullIdFailure("Could not delete entities - at least one provided Id was null.", default(TEntity), DalOperation.Delete);

                if (OnDeleting != null)
                {
                    var entities = Get(keys);
                    CheckIfCanDelete(entities);
                }

                foreach (var key in keys)
                    this.Delete(key);
            }

            private void DeleteInner(IEnumerable<TKey> keys)
            {
                if (!keys.Any())
                    return;

                if (keys.Any(key => EqualityComparer<TKey>.Default.Equals(key, default(TKey))))
                    throw new DalNullIdFailure("Could not delete entities - at least one provided Id was null.", default(TEntity), DalOperation.Delete);

                this.Delete(ent => keys.Contains(ent.Id));
            }

            void IEntitySet.Delete(IEnumerable<Object> keys)
            {
                var typedKeys = keys.OfType<TKey>().ToList();
                if (typedKeys.Count < keys.Count())
                    throw new InvalidOperationFailure();

                this.Delete(typedKeys);
            }

            public override void DeleteAll()
            {
                var result = this.innerSet.RemoveAll();
                if (result.HasLastErrorMessage)
                    throw new DalFailure(result.LastErrorMessage, default(TEntity), DalOperation.Delete);
            }

            internal MongoDB.Driver.MongoCollection<TEntity> InnerSet
            {
                get
                {
                    return this.innerSet;
                }
            }

            private void CreateIndex(IndexDefinition indexDefintion, bool inForeground)
            {
                var keys = new MongoDB.Driver.Builders.IndexKeysBuilder();
                foreach (var field in indexDefintion.Fields)
                {
                    if (field.Direction == Direction.Ascending)
                        keys.Ascending(field.Name);
                    else
                        keys.Descending(field.Name);
                }

                var options = new MongoDB.Driver.Builders.IndexOptionsBuilder();
                options.SetSparse(indexDefintion.AsSparse);
                options.SetUnique(indexDefintion.AsUnique);
                options.SetBackground(!inForeground);

                this.innerSet.CreateIndex(keys, options);
            }

            private void RemoveIndex(IndexDefinition indexDefintion)
            {
                var keys = new MongoDB.Driver.Builders.IndexKeysBuilder();
                foreach (var field in indexDefintion.Fields)
                {
                    if (field.Direction == Direction.Ascending)
                        keys.Ascending(field.Name);
                    else
                        keys.Descending(field.Name);
                }

                this.innerSet.DropIndex(keys);
            }

            public override IndexDefinition[] GetIndexes()
            {
                var indexes = this.innerSet.GetIndexes();

                return indexes.Select(i =>
                    new IndexDefinition(
                        typeof(TEntity),
                        i.Key.Elements.Select(e => new KeyProperty(
                            e.Name,
                            (e.Value > 0 ? Direction.Ascending : Direction.Descending)
                            )).ToArray(),
                        i.IsUnique,
                        i.IsSparse))
                    .ToArray();
            }

            public void Reduce<TResult>(
                Expression<Func<TEntity, bool>> queryExpression,
                string mapFunction, string reduceFunction, string finalizeFunction,
                bool doGetResults, out IEnumerable<TResult> results,
                ReduceStoreMode storeMode = ReduceStoreMode.None,
                string resultSet = null, string resultDbName = null,
                Expression<Func<TEntity, Object>> sortByExpression = null,
                ReduceOptions options = null)
                where TResult : new()
            {
                var query = ToMongoQuery(queryExpression);
                var sortBy = ToMongoSortBy(sortByExpression);

                MapReduceInner(
                    this.innerSet, query, sortBy,
                    mapFunction, reduceFunction, finalizeFunction,
                    doGetResults, out results, storeMode, resultSet, resultDbName, options);
            }

            public void Aggregate<TResult>(
                AggregateOuputMode outputMode,
                bool doGetResults, out IEnumerable<TResult> results,
                IEnumerable<AggregateStep> pipelineSteps,
                AggregateOptions options = null)
            {
                AggregateInner(
                    this.innerSet, outputMode, doGetResults, out results,
                    pipelineSteps, options);
            }

            #region Navigation

            public void LoadNavigation(IEnumerable<TEntity> entities, IEnumerable<string> propertyNames)
            {
                ContextUtils.LoadNavigation<TKey, TEntity>(parent, entities, propertyNames);
            }

            public void SaveNavigation(IEnumerable<TEntity> entities, IEnumerable<string> propertyNames)
            {
                ContextUtils.SaveNavigation(parent, this, entities, propertyNames);
            }

            #endregion

            #region Events

            public event EventHandler<AccessEventArgs<TEntity>> OnModifying;

            public event EventHandler<AccessEventArgs<TEntity>> OnDeleting;

            public event EventHandler<AccessEventArgs<TEntity>> OnGetting;

            private void CheckIfCanModify(TEntity entity)
            {
                bool canAccess;
                CheckIfCanModify(entity, out canAccess);
                if (!canAccess)
                    throw new DalAccessRestrictionFailure("Modify operation for this entity is forbidden.");
            }

            private void CheckIfCanModify(IEnumerable<TEntity> entities)
            {
                var onModifying = this.OnModifying;
                if (onModifying == null)
                    return;

                bool canAccess = true;
                foreach (var entity in entities)
                {
                    var args = new AccessEventArgs<TEntity>(entity);
                    onModifying(this, args);
                    if (!args.CanAccess)
                    {
                        canAccess = false;
                        break;
                    }
                }

                if (canAccess)
                    throw new DalAccessRestrictionFailure("Modify operation for this entity is forbidden.");
            }

            private void CheckIfCanModify(TEntity entity, out bool canAccess)
            {
                canAccess = true;
                var onModifying = this.OnModifying;

                if (onModifying == null)
                    return;

                var args = new AccessEventArgs<TEntity>(entity);
                onModifying(this, args);
                canAccess = args.CanAccess;
            }

            private void CheckIfCanDelete(TEntity entity)
            {
                bool canAccess;
                CheckIfCanDelete(entity, out canAccess);
                if (!canAccess)
                    throw new DalAccessRestrictionFailure("Modify operation for this entity is forbidden.");
            }

            private void CheckIfCanDelete(IEnumerable<TEntity> entities)
            {
                var onDeleting = this.OnDeleting;
                if (onDeleting == null)
                    return;

                bool canAccess = true;
                foreach (var entity in entities)
                {
                    var args = new AccessEventArgs<TEntity>(entity);
                    onDeleting(this, args);
                    if (!args.CanAccess)
                    {
                        canAccess = false;
                        break;
                    }
                }

                if (canAccess)
                    throw new DalAccessRestrictionFailure("Modify operation for this entity is forbidden.");
            }

            private void CheckIfCanDelete(TEntity entity, out bool canAccess)
            {
                canAccess = true;
                var onDeleting = this.OnDeleting;
                if (onDeleting == null)
                    return;

                var args = new AccessEventArgs<TEntity>(entity);
                onDeleting(this, args);
                canAccess = args.CanAccess;
            }

            private TEntity ReturnAfterCheck(TEntity entity)
            {
                var onGetting = this.OnGetting;
                if (onGetting == null)
                    return entity;

                var args = new AccessEventArgs<TEntity>(entity);
                onGetting(this, args);
                if (!args.CanAccess)
                    throw new DalAccessRestrictionFailure("Modify operating for this entity is forbidden.");

                return entity;
            }

            public IEnumerable<TEntity> Filter(IEnumerable<TEntity> entities)
            {
                if (OnGetting == null)
                    return entities;

                var results = new List<TEntity>();
                foreach (var entity in entities)
                {
                    var args = new AccessEventArgs<TEntity>(entity);
                    OnGetting(this, args);
                    if (args.CanAccess)
                        results.Add(entity);
                }

                return results;
            }

            #endregion
        }

        #endregion

        #region FileSet

        public abstract class FileSet
        {
        }

        public class FileSet<TKey> : FileSet, IFileSet<TKey>
        {
            protected readonly MongoDbContext parent;
            protected readonly MongoDB.Driver.GridFS.MongoGridFS innerFS;

            internal FileSet(MongoDbContext parent, string db)
            {
                this.innerFS = MongoDB.Driver.MongoClientExtensions.GetServer(parent.innerContext).GetDatabase(db).GridFS;
            }

            public Type KeyType
            {
                get
                {
                    return typeof(TKey);
                }
            }

            public void Store(Stream inputStream, string fileName, TKey id)
            {
                var bsonID = IdToKey(id);
                var gridFsInfo = this.innerFS.Upload(inputStream, id.ToString());
            }

            public void Retreive(Stream outputStream, TKey id)
            {
                var file = this.innerFS.FindOne(id.ToString());
                this.innerFS.Download(outputStream, file);
            }

            public void Store(string inputFilePath, string fileName, TKey id)
            {
                using (var inputStream = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read))
                {
                    Store(inputStream, fileName, id);
                }
            }

            public void Retreive(string outputFilePath, TKey id)
            {
                using (var outputStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.ReadWrite))
                {
                    Retreive(outputStream, id);
                }
            }

            public void Delete(TKey id)
            {
                this.innerFS.Delete(id.ToString());
            }
        }

        #endregion
    }

    #region Reduce Options

    public enum ReduceStoreMode { None, NewSet, ReplaceItems, Combine };

    public class ReduceOptions
    {
        public bool DoUseJsMode { get; set; }
    }

    #endregion

    #region Aggregate Options

    public enum AggregateOuputMode { Cursor, Inline };

    public class AggregateOptions
    {
        public bool? DoAllowDiskUsage { get; set; }
        public int? BatchSize { get; set; }
        public TimeSpan? Timeout { get; set; }
    }

    public abstract class AggregateStep
    {
        private readonly MongoDB.Bson.BsonDocument bsonDocument;

        protected AggregateStep(string stepName, MongoDB.Bson.BsonValue bsonValue)
        {
            this.bsonDocument = new MongoDB.Bson.BsonDocument(stepName, bsonValue);
        }

        protected AggregateStep(string stepName, string jsonDocument)
            : this(stepName, MongoDB.Bson.BsonDocument.Parse(jsonDocument))
        {
        }

        protected AggregateStep(string stepName, int intValue)
            : this(stepName, new MongoDB.Bson.BsonInt32(intValue))
        {
        }

        public MongoDB.Bson.BsonDocument BsonDocument
        {
            get { return this.bsonDocument; }
        }
    }

    public class AggregateFilter : AggregateStep
    {
        private const string StepName = "$match";

        public AggregateFilter(MongoDB.Driver.IMongoQuery mongoQuery)
            : base(StepName, MongoDB.Bson.BsonExtensionMethods.ToBsonDocument(mongoQuery))
        {
        }

        public AggregateFilter(string jsonDocument) : base(StepName, jsonDocument)
        {
        }
    }

    public class AggregateFilter<TEntity> : AggregateFilter
    {
        public AggregateFilter(Expression<Func<TEntity, bool>> queryExpression)
            : base(MongoDbContext.ToMongoQuery(queryExpression))
        {
        }
    }

    public class AggregateOther : AggregateStep
    {
        public AggregateOther(string stepName, string jsonDocument) : base(stepName, jsonDocument)
        {
        }

        public AggregateOther(string stepName, object value) : base(stepName, MongoDB.Bson.BsonValue.Create(value))
        {
        }
    }

    public class AggregateProject : AggregateStep
    {
        private const string StepName = "$project";

        public AggregateProject(string jsonDocument) : base(StepName, jsonDocument)
        {
        }
    }

    public class AggregateGroup : AggregateStep
    {
        private const string StepName = "$group";

        public AggregateGroup(string jsonDocument) : base(StepName, jsonDocument)
        {
        }
    }

    public class AggregateUnwind : AggregateStep
    {
        private const string StepName = "$unwind";

        public AggregateUnwind(string jsonDocument) : base(StepName, jsonDocument)
        {
        }

        private AggregateUnwind(MongoDB.Bson.BsonString fieldPath) : base(StepName, fieldPath)
        {
        }

        public static AggregateStep FromFieldPath(string fieldPath)
        {
            return new AggregateUnwind(new MongoDB.Bson.BsonString(fieldPath));
        }
    }

    public class AggregateSort : AggregateStep
    {
        private const string StepName = "$sort";

        public AggregateSort(MongoDB.Driver.IMongoSortBy mongoSortBy)
            : base(StepName, MongoDB.Bson.BsonExtensionMethods.ToBsonDocument(mongoSortBy))
        {
        }

        public AggregateSort(string jsonDocument) : base(StepName, jsonDocument)
        {
        }
    }

    public class AggregateSort<TEntity> : AggregateSort
    {
        public AggregateSort(Expression<Func<TEntity, object>> sortByExpression)
            : base(MongoDbContext.ToMongoSortBy(sortByExpression))
        {
        }
    }

    public class AggregateSkip : AggregateStep
    {
        private const string StepName = "$skip";

        public AggregateSkip(int value) : base(StepName, value)
        {
        }
    }

    public class AggregateTake : AggregateStep
    {
        private const string StepName = "$limit";

        public AggregateTake(int value) : base(StepName, value)
        {
        }
    }

    public class AggregateOutput : AggregateStep
    {
        private const string StepName = "$out";

        public AggregateOutput(string resultSet)
            : base(StepName, new MongoDB.Bson.BsonString(resultSet))
        {
        }
    }

    #endregion
}
