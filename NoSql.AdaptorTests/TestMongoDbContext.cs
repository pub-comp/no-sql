using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.NoSql.Core;
using PubComp.NoSql.MongoDbDriver;
using PubComp.NoSql.AdaptorTests.Mock;
using PubComp.NoSql.AdaptorTests.MocksForAccessTest;

namespace PubComp.NoSql.AdaptorTests
{
    [TestClass]
    public class TestMongoDbContext : TestContextBase
    {
        public TestMongoDbContext()
            : base(
                () => new MockMongoDbContext(ParametersForTests.MongoDbConnectionInfo),
                (Status statusGetA, Status statusModA, Status statusDelA)
                    => new MockMongoDbForAccessTests(
                        ParametersForTests.MongoDbConnectionInfo,
                        statusGetA, statusModA, statusDelA),
                ctx => ((MongoDbContext)ctx).DeleteAll(),
                ctx => ((MongoDbContext)ctx).DeleteAll())
        {
        }

        [TestInitialize]
        public override void TestInit()
        {
            base.TestInit();
        }

        public override TestContext TestContext
        {
            get;
            set;
        }

        #region Indexes

        [TestMethod]
        public void TestUpdateIndexes()
        {
            using (var uow = new MockMongoDbContext(ParametersForTests.MongoDbConnectionInfo))
            {
                uow.UpdateIndexes(true);
                var set = (uow.EntitiesWithGuid as MongoDbDriver.MongoDbContext.EntitySet);
                var indexes = set.GetIndexes();
                Assert.AreEqual(2, indexes.Count());
            }
        }

        #endregion

        #region Files

        private string CreateRandomFile(string relativePath, int length)
        {
            byte[] buffer;
            return CreateRandomFile(relativePath, length, out buffer);
        }

        private string CreateRandomFile(string relativePath, int length, out byte[] buffer)
        {
            var path = this.TestContext.TestDeploymentDir + '\\' + relativePath;
            buffer = CreateRandomBuffer(length);

            using (var outputStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
            {
                outputStream.Write(buffer, 0, buffer.Length);
            }

            return path;
        }

        private Stream CreateRandomStream(int length)
        {
            byte[] buffer;
            return CreateRandomStream(length, out buffer);
        }

        private Stream CreateRandomStream(int length, out byte[] buffer)
        {
            buffer = CreateRandomBuffer(length);
            return new MemoryStream(buffer);
        }

        private byte[] CreateRandomBuffer(int length)
        {
            Random rand = new Random();
            byte[] buffer = new byte[length];
            rand.NextBytes(buffer);
            return buffer;
        }

        [TestMethod]
        public void TestFsStreamInStreamOut()
        {
            var id1 = new Guid("{D0462465-9820-4C44-B93A-8A442429E762}");
            byte[] srcBuffer, outBuffer;

            using (var uow = new MockMongoDbContext(ParametersForTests.MongoDbConnectionInfo))
            {
                uow.Files.Delete(id1);

                using (var stream = CreateRandomStream(1000, out srcBuffer))
                {
                    uow.Files.Store(stream, this.TestContext.TestName, id1);
                }
            }

            using (var uow = new MockMongoDbContext(ParametersForTests.MongoDbConnectionInfo))
            {
                using (var stream = new MemoryStream())
                {
                    uow.Files.Retreive(stream, id1);

                    stream.Seek(0, SeekOrigin.Begin);
                    outBuffer = new byte[stream.Length];
                    stream.Read(outBuffer, 0, outBuffer.Length);
                }
            }

            CollectionAssert.AreEqual(srcBuffer, outBuffer);
        }

        #endregion

        #region Reduction

        [TestMethod]
        public void TestReduction()
        {
            // Note: The reduction failed when I attempted to use Decimal instead of Double

            Guid id1 = Guid.NewGuid();
            Guid id2 = Guid.NewGuid();

            int numberOfTransactions = 1000;
            double total1, total2;
            PrepareReductionData(id1, id2, numberOfTransactions, out total1, out total2);

            using (var context = getTestContext() as MockMongoDbContext)
            {
                context.UpdateIndexes(true);

                // Select function for iterator
                const string map =
                    @"function() {
                        var transaction = this;
                        emit(transaction.OwnerId, { NumberOfTransactions: 1, Money: transaction.Money });
                    }";

                // Aggregation of values into a single result
                const string reduce =
                    @"function(key, values) {
                        var result = { NumberOfTransactions: 0, Money: 0 }; // initial value
                        values.forEach(function(value) { // aggregation function
                            result.NumberOfTransactions += value.NumberOfTransactions;
                            result.Money += value.Money;
                        });
                        return result;
                    }";

                // Enables normalizing result e.g. converting sum into average
                const string finalize =
                    @"function(key, value){
                        value.Money = value.Money - 0.1; // Usage fees
                        return value;
                    }";

                IEnumerable<ReductionResult<Guid, AccountStatus>> reduction;
                
                var set = (MongoDbContext.EntitySet<Guid, EntityForCalc>)context.EntitiesForCalc;

                set.Reduce(
                    e => e.OwnerId == id1, map, reduce, finalize, true, out reduction, ReduceStoreMode.None);

                var results = reduction.ToList();

                Assert.AreEqual(1, results.Count);

                Assert.AreEqual(id1, results[0].Id);

                Assert.AreEqual(numberOfTransactions, results[0].value.NumberOfTransactions);
                Assert.IsTrue(Math.Abs(total1 - 0.1 - results[0].value.Money) <= 1);
            }
        }

        [TestMethod]
        public void TestReductionWithSortInput()
        {
            // Note: The reduction failed when I attempted to use Decimal instead of Double

            Guid id1 = Guid.NewGuid();
            Guid id2 = Guid.NewGuid();

            int numberOfTransactions = 1000;
            double total1, total2;
            PrepareReductionData(id1, id2, numberOfTransactions, out total1, out total2);

            using (var context = getTestContext() as MockMongoDbContext)
            {
                context.UpdateIndexes(true);

                // Select function for iterator
                const string map =
                    @"function() {
                        var transaction = this;
                        emit(transaction.OwnerId, { NumberOfTransactions: 1, Money: transaction.Money });
                    }";

                // Aggregation of values into a single result
                const string reduce =
                    @"function(key, values) {
                        var result = { NumberOfTransactions: 0, Money: 0 }; // initial value
                        values.forEach(function(value) { // aggregation function
                            result.NumberOfTransactions += value.NumberOfTransactions;
                            result.Money += value.Money;
                        });
                        return result;
                    }";

                // Enables normalizing result e.g. converting sum into average
                const string finalize =
                    @"function(key, value){
                        value.Money = value.Money - 0.1; // Usage fees
                        return value;
                    }";

                IEnumerable<ReductionResult<Guid, AccountStatus>> reduction;

                var set = (MongoDbContext.EntitySet<Guid, EntityForCalc>)context.EntitiesForCalc;

                set.Reduce(
                    e => e.OwnerId == id1, map, reduce, finalize, true, out reduction,
                    ReduceStoreMode.None, sortByExpression: e => e.OwnerId);

                var results = reduction.ToList();

                Assert.AreEqual(1, results.Count);

                Assert.AreEqual(id1, results[0].Id);

                Assert.AreEqual(numberOfTransactions, results[0].value.NumberOfTransactions);
                Assert.IsTrue(Math.Abs(total1 - 0.1 - results[0].value.Money) <= 1);
            }
        }

        [TestMethod]
        public void TestReductionToDifferentDb()
        {
            // Note: The reduction failed when I attempted to use Decimal instead of Double

            Guid id1 = Guid.NewGuid();
            Guid id2 = Guid.NewGuid();

            int numberOfTransactions = 1000;
            double total1, total2;
            PrepareReductionData(id1, id2, numberOfTransactions, out total1, out total2);

            using (var context = getTestContext() as MockMongoDbContext)
            {
                context.UpdateIndexes(true);

                // Select function for iterator
                const string map =
                    @"function() {
                        var transaction = this;
                        emit(transaction.OwnerId, { NumberOfTransactions: 1, Money: transaction.Money });
                    }";

                // Aggregation of values into a single result
                const string reduce =
                    @"function(key, values) {
                        var result = { NumberOfTransactions: 0, Money: 0 }; // initial value
                        values.forEach(function(value) { // aggregation function
                            result.NumberOfTransactions += value.NumberOfTransactions;
                            result.Money += value.Money;
                        });
                        return result;
                    }";

                // Enables normalizing result e.g. converting sum into average
                const string finalize =
                    @"function(key, value){
                        value.Money = value.Money - 0.1; // Usage fees
                        return value;
                    }";

                IEnumerable<ReductionResult<Guid, AccountStatus>> reduction;

                var set = (MongoDbContext.EntitySet<Guid, EntityForCalc>)context.EntitiesForCalc;

                set.Reduce(
                    e => e.OwnerId == id1, map, reduce, finalize, true, out reduction,
                    ReduceStoreMode.NewSet, resultSet: "Results1", resultDbName: "ResultsDb1");

                var resultsSet = context.GetEntitySet<Guid, ReductionResult<Guid, AccountStatus>>(
                    "ResultsDb1", "Results1");

                var results = resultsSet.AsQueryable().ToList();

                Assert.AreEqual(1, results.Count);

                Assert.AreEqual(id1, results[0].Id);

                Assert.AreEqual(numberOfTransactions, results[0].value.NumberOfTransactions);
                Assert.IsTrue(Math.Abs(total1 - 0.1 - results[0].value.Money) <= 1);
            }
        }

        [TestMethod]
        public void TestReductionWithoutQueryAndFinalize()
        {
            // Note: The reduction failed when I attempted to use Decimal instead of Double

            Guid id1 = Guid.NewGuid();
            Guid id2 = Guid.NewGuid();

            int numberOfTransactions = 1000;
            double total1, total2;
            PrepareReductionData(id1, id2, numberOfTransactions, out total1, out total2);

            using (var context = getTestContext() as MockMongoDbContext)
            {
                context.UpdateIndexes(true);

                // Select function for iterator
                const string map =
                    @"function() {
                        var transaction = this;
                        emit(transaction.OwnerId, { NumberOfTransactions: 1, Money: transaction.Money });
                    }";

                // Aggregation of values into a single result
                const string reduce =
                    @"function(key, values) {
                        var result = { NumberOfTransactions: 0, Money: 0 }; // initial value
                        values.forEach(function(value) { // aggregation function
                            result.NumberOfTransactions += value.NumberOfTransactions;
                            result.Money += value.Money;
                        });
                        return result;
                    }";

                IEnumerable<ReductionResult<Guid, AccountStatus>> reduction;

                var set = (MongoDbContext.EntitySet<Guid, EntityForCalc>)context.EntitiesForCalc;

                set.Reduce(
                    null, map, reduce, null, true, out reduction, ReduceStoreMode.None);

                var results = reduction.ToList();

                Assert.AreEqual(2, results.Count);

                Assert.IsTrue(results.Any(r => r.Id == id1));
                Assert.IsTrue(results.Any(r => r.Id == id2));

                Assert.AreEqual(numberOfTransactions, results.First(r => r.Id == id1).value.NumberOfTransactions);
                Assert.IsTrue(Math.Abs(total1 - results.First(r => r.Id == id1).value.Money) <= 1);

                Assert.AreEqual(numberOfTransactions, results.First(r => r.Id == id2).value.NumberOfTransactions);
                Assert.IsTrue(Math.Abs(total2 - results.First(r => r.Id == id2).value.Money) <= 1);
            }
        }

        [TestMethod]
        public void TestReductionWithJoin()
        {
            // Note: The reduction failed when I attempted to use Decimal instead of Double

            Guid id1 = Guid.NewGuid();
            Guid id2 = Guid.NewGuid();

            int numberOfTransactions = 1000;
            double total1, total2;
            PrepareReductionData(id1, id2, numberOfTransactions, out total1, out total2);

            using (var context = getTestContext() as MockMongoDbContext)
            {
                context.UpdateIndexes(true);

                // Select function for iterator
                const string map1 =
                    @"function() {
                        var e = this;
                        emit(e._id, { Name: e.Name, NumberOfTransactions: 0, Money: 0 });
                    }";

                // Select function for iterator
                const string map2 =
                    @"function() {
                        var e = this;
                        emit(e.OwnerId, { Name: null, NumberOfTransactions: 1, Money: e.Money });
                    }";

                // Aggregation of values into a single result
                const string reduce =
                    @"function(key, values) {
                        var result = { Name: null, NumberOfTransactions: 0, Money: 0 }; // initial value
                        values.forEach(function(value) { // aggregation function
                            result.Name = result.Name || value.Name;
                            result.NumberOfTransactions += value.NumberOfTransactions;
                            result.Money += value.Money;
                        });
                        return result;
                    }";

                // Enables normalizing result e.g. converting sum into average
                string finalize =
                    @"function(key, value){
                        value.Money = value.Money - 0.1; // Usage fees
                        return value;
                    }";

                IEnumerable<ReductionResult<Guid, AccountStatusWithName>> reduction;

                var set1 = (MongoDbContext.EntitySet<Guid, EntityWithGuid>)context.EntitiesWithGuid;

                set1.Reduce(
                    e => e.Id == id1, map1, reduce, finalize, false, out reduction,
                    ReduceStoreMode.NewSet, "reductionResults");

                var set2 = (MongoDbContext.EntitySet<Guid, EntityForCalc>)context.EntitiesForCalc;

                set2.Reduce(
                    e => e.OwnerId == id1, map2, reduce, finalize, true, out reduction,
                    ReduceStoreMode.Combine, "reductionResults");

                var results = reduction.ToList();

                Assert.AreEqual(1, results.Count);

                Assert.AreEqual(id1, results[0].Id);

                Assert.AreEqual("a", results[0].value.Name);
                Assert.AreEqual(numberOfTransactions, results[0].value.NumberOfTransactions);
                Assert.IsTrue(Math.Abs(total1 - 0.1 - results[0].value.Money) <= 1);
            }
        }

        [TestMethod]
        public void TestReductionOnDynamicallyNamedCollection()
        {
            // Note: The reduction failed when I attempted to use Decimal instead of Double

            Guid id1 = Guid.NewGuid();
            Guid id2 = Guid.NewGuid();

            int numberOfTransactions = 1000;
            double total1, total2;
            PrepareReductionData(id1, id2, numberOfTransactions, out total1, out total2);

            using (var context = getTestContext() as MockMongoDbContext)
            {
                context.UpdateIndexes(true);

                // Select function for iterator
                const string map =
                    @"function() {
                        var transaction = this;
                        emit(transaction.OwnerId, { NumberOfTransactions: 1, Money: transaction.Money });
                    }";

                // Aggregation of values into a single result
                const string reduce =
                    @"function(key, values) {
                        var result = { NumberOfTransactions: 0, Money: 0 }; // initial value
                        values.forEach(function(value) { // aggregation function
                            result.NumberOfTransactions += value.NumberOfTransactions;
                            result.Money += value.Money;
                        });
                        return result;
                    }";

                // Enables normalizing result e.g. converting sum into average
                const string finalize =
                    @"function(key, value){
                        value.Money = value.Money - 0.1; // Usage fees
                        return value;
                    }";

                IEnumerable<ReductionResult<Guid, AccountStatus>> reduction;

                context.MapReduce<EntityForCalc, ReductionResult<Guid, AccountStatus>>(
                    @"entityforcalc", e => e.OwnerId == id1,
                    map, reduce, finalize, true, out reduction, ReduceStoreMode.None);

                var results = reduction.ToList();

                Assert.AreEqual(1, results.Count);

                Assert.AreEqual(id1, results[0].Id);

                Assert.AreEqual(numberOfTransactions, results[0].value.NumberOfTransactions);
                Assert.IsTrue(Math.Abs(total1 - 0.1 - results[0].value.Money) <= 1);
            }
        }

        public class AccountStatus
        {
            public int NumberOfTransactions { get; set; }
            public double Money { get; set; }
        }

        public class AccountStatusWithName
        {
            public string Name { get; set; }
            public int NumberOfTransactions { get; set; }
            public double Money { get; set; }
        }

        #endregion

        #region Atomic Operations

        [TestMethod]
        public void TestUpdateField()
        {
            var id = Guid.NewGuid();

            using (var context = new MockMongoDbContext(ParametersForTests.MongoDbConnectionInfo))
            {
                context.EntitiesForUpdates.Add(new EntityForUpdates
                    {
                        Id = id,
                        Count = 7,
                        Text = "qwerty",
                    });
            }

            using (var context = new MockMongoDbContext(ParametersForTests.MongoDbConnectionInfo))
            {
                (context.EntitiesForUpdates as MongoDbDriver.MongoDbContext.EntitySet<Guid, EntityForUpdates>)
                    .UpdateField(new EntityForUpdates
                    {
                        Id = id,
                        Count = 9,
                        Text = "test",
                    }, "Text");
            }

            using (var context = new MockMongoDbContext(ParametersForTests.MongoDbConnectionInfo))
            {
                var entity = context.EntitiesForUpdates.Get(id);
                Assert.AreEqual(id, entity.Id);
                Assert.AreEqual(7, entity.Count);
                Assert.AreEqual("test", entity.Text);
            }
        }

        [TestMethod]
        public void TestAdd_UpdateSingleField()
        {
            var id = Guid.NewGuid();

            using (var context = getTestContext())
            {
                var set = context.EntitiesForUpdates;

                var o1 = new EntityForUpdates
                {
                    Id = id,
                    Text = "o1",
                    Inners = new List<InnerClass>
                    {
                        new InnerClass
                        {
                            Property = 2,
                        },
                    },
                };

                set.Add(o1);
            }

            using (var context = getTestContext())
            {
                var set = context.EntitiesForUpdates as MongoDbContext.EntitySet<Guid, EntityForUpdates>;

                var o2 = new EntityForUpdates
                {
                    Id = id,
                    Text = "o1-updated",
                    Inners = new List<InnerClass>
                    {
                        new InnerClass
                        {
                            Property = 3,
                        },
                        new InnerClass
                        {
                            Property = 5,
                        },
                    },
                };

                set.UpdateField(o2, "Inners");
            }

            using (var context = getTestContext())
            {
                var set = context.EntitiesForUpdates;

                var o3 = set.Get(id);

                Assert.AreEqual("o1", o3.Text);
                Assert.IsNotNull(o3.Inners.Count);
                Assert.AreEqual(2, o3.Inners.Count);
                Assert.IsTrue(o3.Inners.Any(i => i.Property == 3));
                Assert.IsTrue(o3.Inners.Any(i => i.Property == 5));
            }
        }

        [TestMethod]
        public void TestAdd_UpdateSingleFields()
        {
            var id = Guid.NewGuid();

            using (var context = getTestContext())
            {
                var set = context.EntitiesForUpdates;

                var o1 = new EntityForUpdates
                {
                    Id = id,
                    Text = "o1",
                    Inners = new List<InnerClass>
                    {
                        new InnerClass
                        {
                            Property = 2,
                        },
                    },
                    Count = 0,
                };

                set.Add(o1);
            }

            using (var context = getTestContext())
            {
                var set = context.EntitiesForUpdates as MongoDbContext.EntitySet<Guid, EntityForUpdates>;

                var o2 = new EntityForUpdates
                {
                    Id = id,
                    Text = "o1-updated",
                    Inners = new List<InnerClass>
                    {
                        new InnerClass
                        {
                            Property = 3,
                        },
                        new InnerClass
                        {
                            Property = 5,
                        },
                    },
                    Count = 1,
                };

                set.UpdateFields(o2, "Inners", "Count");
            }

            using (var context = getTestContext())
            {
                var set = context.EntitiesForUpdates;

                var o3 = set.Get(id);

                Assert.AreEqual("o1", o3.Text);
                Assert.IsNotNull(o3.Inners.Count);
                Assert.AreEqual(2, o3.Inners.Count);
                Assert.IsTrue(o3.Inners.Any(i => i.Property == 3));
                Assert.IsTrue(o3.Inners.Any(i => i.Property == 5));
                Assert.AreEqual(1, o3.Count);
            }
        }

        [TestMethod]
        public void TestIncrementField()
        {
            var id = Guid.NewGuid();

            using (var context = new MockMongoDbContext(ParametersForTests.MongoDbConnectionInfo))
            {
                context.EntitiesForUpdates.Add(new EntityForUpdates
                    {
                        Id = id,
                        Count = 7,
                        Text = "qwerty",
                    });
            }

            using (var context = new MockMongoDbContext(ParametersForTests.MongoDbConnectionInfo))
            {
                (context.EntitiesForUpdates as MongoDbDriver.MongoDbContext.EntitySet<Guid, EntityForUpdates>)
                    .IncrementField(id, "Count", 3);
            }

            using (var context = new MockMongoDbContext(ParametersForTests.MongoDbConnectionInfo))
            {
                var entity = context.EntitiesForUpdates.Get(id);
                Assert.AreEqual(id, entity.Id);
                Assert.AreEqual(10, entity.Count);
                Assert.AreEqual("qwerty", entity.Text);
            }
        }

        #endregion

        #region Known Types

        [TestMethod]
        public void TestMongoDbKnownTypesResolver()
        {
            var context = new MongoDbInheritancesContext1(ParametersForTests.MongoDbConnectionInfo);

            Assert.IsTrue(MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(TypeB1)));
            Assert.IsTrue(MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(TypeC1.TypeD)));
            Assert.IsTrue(MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(TypeC1.TypeD.TypeE)));
            Assert.IsTrue(MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(TypeF1.TypeI.TypeK)));
            //Assert.IsFalse(MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(TypeC1)));
            //Assert.IsFalse(MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(TypeF1)));
            //Assert.IsFalse(MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(TypeF1.TypeG)));
            //Assert.IsFalse(MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(TypeF1.TypeG.TypeH)));
            //Assert.IsFalse(MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(TypeF1.TypeI)));
            //Assert.IsFalse(MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(TypeF1.TypeI.TypeJ)));
        }

        [TestMethod]
        public void TestMongoDbKnownTypes()
        {
            var context = new MongoDbInheritancesContext2(ParametersForTests.MongoDbConnectionInfo);

            Assert.IsTrue(MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(TypeB2)));
            Assert.IsTrue(MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(TypeC2.TypeD)));
            //Assert.IsFalse(MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(TypeC2.TypeD.TypeE)));
            //Assert.IsFalse(MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(TypeF2.TypeI.TypeK)));
            //Assert.IsFalse(MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(TypeC1)));
            //Assert.IsFalse(MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(TypeF1)));
            //Assert.IsFalse(MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(TypeF1.TypeG)));
            //Assert.IsFalse(MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(TypeF1.TypeG.TypeH)));
            //Assert.IsFalse(MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(TypeF1.TypeI)));
            //Assert.IsFalse(MongoDB.Bson.Serialization.BsonClassMap.IsClassMapRegistered(typeof(TypeF1.TypeI.TypeJ)));
        }

        #endregion

        #region Named EntitySets

        [TestMethod]
        public void TestNamedEntitySets()
        {
            using (var context = getTestContext())
            {
                var set1 = ((MongoDbContext)context).GetEntitySet<string, EntityWithString>("collection1");
                ((MongoDbContext.EntitySet)set1).DeleteAll();

                var set2 = ((MongoDbContext)context).GetEntitySet<string, EntityWithString>("collection2");
                ((MongoDbContext.EntitySet)set2).DeleteAll();

                set1.Add(new EntityWithString { Id = "id1", Name = "name1" });
                set2.Add(new EntityWithString { Id = "id1", Name = "name2" });
            }

            using (var context = getTestContext())
            {
                var set1 = ((MongoDbContext)context).GetEntitySet<string, EntityWithString>("collection1");
                var set2 = ((MongoDbContext)context).GetEntitySet<string, EntityWithString>("collection2");
                var obj1 = set1.Get("id1");
                var obj2 = set2.Get("id1");
                Assert.IsNotNull(obj1);
                Assert.IsNotNull(obj2);
                Assert.AreEqual("name1", obj1.Name);
                Assert.AreEqual("name2", obj2.Name);
            }
        }

        #endregion

        #region Capped EntitySets

        [TestMethod]
        public void TestMaxSize()
        {
            var sourceDocuments = new List<Tag>(100);

            for (int cnt = 0; cnt < 100; cnt++)
            {
                sourceDocuments.Add(
                    new Tag
                    {
                        Id = Guid.NewGuid(),
                        Name = cnt.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    });
            }

            using (var context = (MockMongoDbContext)getTestContext())
            {
                for (int cnt = 0; cnt < sourceDocuments.Count; cnt++)
                    context.TagsMaxSize.Add(sourceDocuments[cnt]);
            }

            List<Tag> readDocuments;

            using (var context = (MockMongoDbContext)getTestContext())
            {
                readDocuments = context.TagsMaxSize.AsQueryable().ToList();
            }

            Assert.IsTrue(readDocuments.Count < sourceDocuments.Count);
        }

        [TestMethod]
        public void TestMaxDocuments()
        {
            var sourceDocuments = new List<Tag>(11);

            for (int cnt = 0; cnt < 11; cnt++)
            {
                sourceDocuments.Add(
                    new Tag
                    {
                        Id = Guid.NewGuid(),
                        Name = cnt.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    });
            }

            using (var context = (MockMongoDbContext)getTestContext())
            {
                for (int cnt = 0; cnt < sourceDocuments.Count; cnt++)
                    context.TagsMaxCount.Add(sourceDocuments[cnt]);
            }

            List<Tag> readDocuments;

            using (var context = (MockMongoDbContext)getTestContext())
            {
                readDocuments = context.TagsMaxCount.AsQueryable().ToList();
            }

            Assert.AreEqual(10, readDocuments.Count);

            for (int cnt = 1; cnt < sourceDocuments.Count; cnt++)
            {
                Assert.IsTrue(
                    readDocuments.Any(d => d.Id == sourceDocuments[cnt].Id && d.Name == sourceDocuments[cnt].Name));
            }
        }

        #endregion

        #region Update, Delete by query

        [TestMethod]
        public void TestDeleteByQuery()
        {
            var sourceDocuments = new List<Tag>(100);

            for (int cnt = 0; cnt < 100; cnt++)
            {
                sourceDocuments.Add(
                    new Tag
                    {
                        Id = Guid.NewGuid(),
                        Name = (cnt % 10).ToString(System.Globalization.CultureInfo.InvariantCulture)
                    });
            }

            using (var context = (MockMongoDbContext)getTestContext())
            {
                for (int cnt = 0; cnt < sourceDocuments.Count; cnt++)
                    context.Tags2.Add(sourceDocuments[cnt]);
            }

            using (var context = (MockMongoDbContext)getTestContext())
            {
                context.Tags2.Delete(t => t.Name == "3");
            }

            List<Tag> readDocuments;

            using (var context = (MockMongoDbContext)getTestContext())
            {
                readDocuments = context.Tags2.AsQueryable().ToList();
            }

            Assert.AreEqual(90, readDocuments.Count);

            for (int cnt = 0; cnt < sourceDocuments.Count; cnt++)
            {
                if (cnt % 10 == 3)
                    continue;

                Assert.IsTrue(
                    readDocuments.Any(d => d.Id == sourceDocuments[cnt].Id && d.Name == sourceDocuments[cnt].Name));
            }
        }

        [TestMethod]
        public void TestUpdateByQuery()
        {
            var sourceDocuments = new List<Tag>(100);

            for (int cnt = 0; cnt < 100; cnt++)
            {
                sourceDocuments.Add(
                    new Tag
                    {
                        Id = Guid.NewGuid(),
                        Name = (cnt % 10).ToString(System.Globalization.CultureInfo.InvariantCulture)
                    });
            }

            using (var context = (MockMongoDbContext)getTestContext())
            {
                for (int cnt = 0; cnt < sourceDocuments.Count; cnt++)
                    context.Tags2.Add(sourceDocuments[cnt]);
            }

            using (var context = (MockMongoDbContext)getTestContext())
            {
                context.Tags2.Update(t => t.Name == "3", new KeyValuePair<string, object>("Name", "333"));
            }

            List<Tag> readDocuments;

            using (var context = (MockMongoDbContext)getTestContext())
            {
                readDocuments = context.Tags2.AsQueryable().ToList();
            }

            Assert.AreEqual(100, readDocuments.Count);

            for (int cnt = 0; cnt < sourceDocuments.Count; cnt++)
            {
                if (cnt % 10 != 3)
                {
                    Assert.IsTrue(
                        readDocuments.Any(d => d.Id == sourceDocuments[cnt].Id && d.Name == sourceDocuments[cnt].Name));
                }
                else
                {
                    Assert.IsTrue(
                        readDocuments.Any(d => d.Id == sourceDocuments[cnt].Id && d.Name == "333"));
                }
            }
        }

        #endregion
    }
}
