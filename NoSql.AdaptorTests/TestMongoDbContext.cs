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

            using (var context = getMockContext() as MockMongoDbContext)
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
                
                var set = (MongoDbDriver.MongoDbContext.EntitySet<Guid, EntityForCalc>)context.EntitiesForCalc;

                set.Reduce<ReductionResult<Guid, AccountStatus>>(
                    e => e.OwnerId == id1, map, reduce, finalize, true, out reduction, ReduceStoreMode.None);

                var results = reduction.ToList();

                Assert.AreEqual(1, results.Count);

                Assert.AreEqual(id1, results[0].Id);

                Assert.AreEqual(numberOfTransactions, results[0].value.NumberOfTransactions);
                Assert.IsTrue(Math.Abs(total1 - 0.1 - results[0].value.Money) <= 1);
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

            using (var context = getMockContext() as MockMongoDbContext)
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

                var set1 = (MongoDbDriver.MongoDbContext.EntitySet<Guid, EntityWithGuid>)context.EntitiesWithGuid;

                set1.Reduce<ReductionResult<Guid, AccountStatusWithName>>(
                    e => e.Id == id1, map1, reduce, finalize, false, out reduction,
                    ReduceStoreMode.NewSet, "reductionResults");

                var set2 = (MongoDbDriver.MongoDbContext.EntitySet<Guid, EntityForCalc>)context.EntitiesForCalc;

                set2.Reduce<ReductionResult<Guid, AccountStatusWithName>>(
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

            using (var context = getMockContext() as MockMongoDbContext)
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

            using (var context = getMockContext())
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

            using (var context = getMockContext())
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

            using (var context = getMockContext())
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
    }
}
