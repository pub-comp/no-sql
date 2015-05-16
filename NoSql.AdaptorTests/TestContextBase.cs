using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.NoSql.Core;
using PubComp.NoSql.AdaptorTests.Mock;
using PubComp.NoSql.AdaptorTests.MocksForAccessTest;

namespace PubComp.NoSql.AdaptorTests
{
    public abstract class TestContextBase
    {
        protected readonly Func<IMockContext> getTestContext;
        protected GetMockContextForAccessTestsDelegate getTestContextForAccessTests;
        protected readonly Action<IMockContext> deleteTestContext;
        protected readonly Action<IMockContextForAccessTests> deleteTestContextForAccessTests;

        protected TestContextBase(
            Func<IMockContext> getTestContext,
            GetMockContextForAccessTestsDelegate getTestContextForAccessTests,
            Action<IMockContext> deleteTestContext,
            Action<IMockContextForAccessTests> deleteTestContextForAccessTests)
        {
            this.getTestContext = getTestContext;
            this.getTestContextForAccessTests = getTestContextForAccessTests;
            this.deleteTestContext = deleteTestContext;
            this.deleteTestContextForAccessTests = deleteTestContextForAccessTests;
        }

        protected delegate IMockContextForAccessTests GetMockContextForAccessTestsDelegate(
                Status statusGetA, Status statusModA, Status statusDelA);

        public virtual TestContext TestContext
        {
            get;
            set;
        }

        private void DeleteAll()
        {
            using (var context = getTestContext())
            {
                deleteTestContext(context);
            }

            using (var context = getTestContextForAccessTests(Status.Private, Status.Private, Status.Private))
            {
                deleteTestContextForAccessTests(context);
            }
        }

        public virtual void TestInit()
        {
            DeleteAll();
        }

        #region Basic Functionality One by One, single key

        [TestMethod]
        public void TestAdd_Get()
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
                            Property = 1,
                        },
                    },
                };

                set.Add(o1);
            }

            using (var context = getTestContext())
            {
                var set = context.EntitiesForUpdates;

                var o2 = set.Get(id);

                Assert.AreEqual("o1", o2.Text);
                Assert.IsNotNull(o2.Inners.Count);
                Assert.AreEqual(1, o2.Inners.Count);
                Assert.IsTrue(o2.Inners.Any(i => i.Property == 1));
            }
        }

        [TestMethod]
        public void TestAdd_Update()
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
                var set = context.EntitiesForUpdates;

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

                set.Update(o2);
            }

            using (var context = getTestContext())
            {
                var set = context.EntitiesForUpdates;

                var o3 = set.Get(id);

                Assert.AreEqual("o1-updated", o3.Text);
                Assert.IsNotNull(o3.Inners.Count);
                Assert.AreEqual(2, o3.Inners.Count);
                Assert.IsTrue(o3.Inners.Any(i => i.Property == 3));
                Assert.IsTrue(o3.Inners.Any(i => i.Property == 5));
            }
        }

        [TestMethod]
        public void TestAdd_Update_SpecificEntity()
        {
            var id = Guid.NewGuid();

            using (var context = getTestContext())
            {
                var o1 = new SpecificEntity
                {
                    Id = id,
                    Name = "n1",
                    SpecificData = "d1",
                };

                context.SpecificEntities.Add(o1);
            }

            using (var context = getTestContext())
            {
                var o2 = new SpecificEntity
                {
                    Id = id,
                    Name = "n2",
                    SpecificData = "d2",
                };

                context.SpecificEntities.Update(o2);
            }

            using (var context = getTestContext())
            {
                var o3 = context.SpecificEntities.Get(id);

                Assert.AreEqual("n2", o3.Name);
                Assert.AreEqual("d2", o3.SpecificData);
            }
        }

        [TestMethod]
        public void TestAdd_DeleteById()
        {
            var id = Guid.NewGuid();

            using (var context = getTestContext())
            {
                var set = context.EntitiesWithGuid;

                var o1 = new EntityWithGuid
                {
                    Id = id,
                    Name = "o1",
                };

                set.Add(o1);
            }

            using (var context = getTestContext())
            {
                var set = context.EntitiesWithGuid;

                set.Delete(id);
            }

            using (var context = getTestContext())
            {
                var set = context.EntitiesWithGuid;

                var o3 = set.Get(id);

                Assert.IsNull(o3);
            }
        }

        [TestMethod]
        public void TestAdd_DeleteByObj()
        {
            var id = Guid.NewGuid();

            EntityWithGuid o1;

            using (var context = getTestContext())
            {
                var set = context.EntitiesWithGuid;

                o1 = new EntityWithGuid
                {
                    Id = id,
                    Name = "o1",
                };

                set.Add(o1);
            }

            using (var context = getTestContext())
            {
                var set = context.EntitiesWithGuid;

                set.Delete(o1);
            }

            using (var context = getTestContext())
            {
                var set = context.EntitiesWithGuid;

                var o3 = set.Get(id);

                Assert.IsNull(o3);
            }
        }

        [TestMethod]
        public void TestAdd_AddIfNotExists()
        {
            var id = Guid.NewGuid();

            using (var context = getTestContext())
            {
                var set = context.EntitiesWithGuid;

                var o1 = new EntityWithGuid
                {
                    Id = id,
                    Name = "o1",
                };

                set.Add(o1);
            }

            using (var context = getTestContext())
            {
                var set = context.EntitiesWithGuid;

                var o2 = new EntityWithGuid
                {
                    Id = id,
                    Name = "o1-updated",
                };

                var added = set.AddIfNotExists(o2);
                Assert.IsFalse(added);
            }

            using (var context = getTestContext())
            {
                var set = context.EntitiesWithGuid;

                var o3 = set.Get(id);

                Assert.AreEqual("o1", o3.Name);
            }
        }

        [TestMethod]
        public void TestAddIfNotExists()
        {
            var id = Guid.NewGuid();

            using (var context = getTestContext())
            {
                var set = context.EntitiesWithGuid;

                var o2 = new EntityWithGuid
                {
                    Id = id,
                    Name = "o2",
                };

                var added = set.AddIfNotExists(o2);
                Assert.IsTrue(added);
            }

            using (var context = getTestContext())
            {
                var set = context.EntitiesWithGuid;

                var o3 = set.Get(id);

                Assert.AreEqual("o2", o3.Name);
            }
        }

        [TestMethod]
        public void TestAdd_AddOrUpdate()
        {
            var id = Guid.NewGuid();

            using (var context = getTestContext())
            {
                var set = context.EntitiesWithGuid;

                var o1 = new EntityWithGuid
                {
                    Id = id,
                    Name = "o1",
                };

                set.Add(o1);
            }

            using (var context = getTestContext())
            {
                var set = context.EntitiesWithGuid;

                var o2 = new EntityWithGuid
                {
                    Id = id,
                    Name = "o1-updated",
                };

                set.AddOrUpdate(o2);
            }

            using (var context = getTestContext())
            {
                var set = context.EntitiesWithGuid;

                var o3 = set.Get(id);

                Assert.AreEqual("o1-updated", o3.Name);
            }
        }

        [TestMethod]
        public void TestAddOrUpdate()
        {
            var id = Guid.NewGuid();

            using (var context = getTestContext())
            {
                var set = context.EntitiesWithGuid;

                var o2 = new EntityWithGuid
                {
                    Id = id,
                    Name = "o2",
                };

                set.AddOrUpdate(o2);
            }

            using (var context = getTestContext())
            {
                var set = context.EntitiesWithGuid;

                var o3 = set.Get(id);

                Assert.AreEqual("o2", o3.Name);
            }
        }

        [TestMethod]
        public void DateOnlyTest()
        {
            var id = Guid.NewGuid();

            using (var context = getTestContext())
            {
                var set = context.Dates;

                var o1 = new Dates
                {
                    Id = id,
                    Date1 = new DateTime(2005, 10, 20),
                };

                set.AddOrUpdate(o1);
            }

            using (var context = getTestContext())
            {
                var set = context.Dates;

                var o2 = set.Get(id);

                Assert.AreEqual(new DateTime(2005, 10, 20), o2.Date1);
            }
        }

        [TestMethod]
        public void DateTimeTest()
        {
            var id = Guid.NewGuid();

            using (var context = getTestContext())
            {
                var set = context.Dates;

                var o1 = new Dates
                {
                    Id = id,
                    Date2 = new DateTime(2002, 04, 08, 16, 32, 04, 128),
                };

                set.AddOrUpdate(o1);
            }

            using (var context = getTestContext())
            {
                var set = context.Dates;

                var o2 = set.Get(id);

                Assert.AreEqual(new DateTime(2002, 04, 08, 16, 32, 04, 128), o2.Date2);
            }
        }

        [TestMethod]
        public void DateOnlyNullableTest()
        {
            var id = Guid.NewGuid();

            using (var context = getTestContext())
            {
                var set = context.Dates;

                var o1 = new Dates
                {
                    Id = id,
                    Date3 = new DateTime(2005, 10, 20),
                };

                set.AddOrUpdate(o1);
            }

            using (var context = getTestContext())
            {
                var set = context.Dates;

                var o2 = set.Get(id);

                Assert.AreEqual(new DateTime(2005, 10, 20), o2.Date3);
            }
        }

        [TestMethod]
        public void DateTimeNullableTest()
        {
            var id = Guid.NewGuid();

            using (var context = getTestContext())
            {
                var set = context.Dates;

                var o1 = new Dates
                {
                    Id = id,
                    Date4 = new DateTime(2002, 04, 08, 16, 32, 04, 128),
                };

                set.AddOrUpdate(o1);
            }

            using (var context = getTestContext())
            {
                var set = context.Dates;

                var o2 = set.Get(id);

                Assert.AreEqual(new DateTime(2002, 04, 08, 16, 32, 04, 128), o2.Date4);
            }
        }

        [TestMethod]
        public void DateTimeQueryTest()
        {
            var id = Guid.NewGuid();
            var date1 = new DateTime(2002, 04, 08, 16, 32, 04, 127);
            var date2 = new DateTime(2002, 04, 08, 16, 32, 04, 128);
            var date3 = new DateTime(2002, 04, 08, 16, 32, 04, 129);

            using (var context = getTestContext())
            {
                var set = context.Dates;

                var o1 = new Dates
                {
                    Id = id,
                    Date2 = date2,
                };

                set.AddOrUpdate(o1);
            }

            using (var context = getTestContext())
            {
                var dates = context.Dates.AsQueryable().Where(d => d.Date2 > date1 && d.Date2 < date3).ToList();
                Assert.AreEqual(1, dates.Count);
                Assert.AreEqual(id, dates[0].Id);
                Assert.AreEqual(date2, dates[0].Date2);
            }
        }

        [TestMethod]
        public void DateTimeNullableQueryTest()
        {
            var id = Guid.NewGuid();
            var date1 = new DateTime(2002, 04, 08, 16, 32, 04, 127);
            var date2 = new DateTime(2002, 04, 08, 16, 32, 04, 128);
            var date3 = new DateTime(2002, 04, 08, 16, 32, 04, 129);

            using (var context = getTestContext())
            {
                var set = context.Dates;

                var o1 = new Dates
                {
                    Id = id,
                    Date4 = date2,
                };

                set.AddOrUpdate(o1);
            }

            using (var context = getTestContext())
            {
                var dates = context.Dates.AsQueryable().Where(d => d.Date4 != null && d.Date4 > date1 && d.Date4 < date3).ToList();
                Assert.AreEqual(1, dates.Count);
                Assert.AreEqual(id, dates[0].Id);
                Assert.AreEqual(true, dates[0].Date4.HasValue);
                Assert.AreEqual(date2, dates[0].Date4.Value);
            }
        }

        #endregion

        #region Slightly more complex flows

        [TestMethod]
        public void TestUnitOfWorkGetOrAdd()
        {
            var id1 = new Guid("{E0E69222-7C20-4F16-B146-83C6C44FFCCA}");
            var id2 = new Guid("{4D50A826-9F0A-473F-8B16-E2CA2F71431E}");
            var id3 = new Guid("{87FBB4F8-9699-465B-94A5-A94324693B72}");

            using (var context = getTestContext())
            {
                Assert.AreEqual(0, context.MultiIDEntities.AsQueryable().Count());

                var item = new MultiIDEntity
                {
                    Id = id1,
                    Item1Id = id2,
                    Item2Id = id3,
                    Flags = MyFlags.One,
                };

                var existing = context.MultiIDEntities.GetOrAdd(item);
                Assert.IsNull(existing);
            }

            using (var context = getTestContext())
            {
                var item = new MultiIDEntity
                {
                    Id = id1,
                    Flags = MyFlags.Two,
                };

                var existing = context.MultiIDEntities.GetOrAdd(item);
                Assert.IsNotNull(existing);
                Assert.AreEqual(MyFlags.One, existing.Flags);
            }

            using (var context = getTestContext())
            {
                var item = context.MultiIDEntities.Get(id1);
                Assert.IsNotNull(item);

                Assert.AreEqual(MyFlags.One, item.Flags);
            }
        }

        [TestMethod]
        public void TestUnitOfWorkAddIfNotExists()
        {
            var id1 = new Guid("{E0E69222-7C20-4F16-B146-83C6C44FFCCA}");
            var id2 = new Guid("{4D50A826-9F0A-473F-8B16-E2CA2F71431E}");
            var id3 = new Guid("{87FBB4F8-9699-465B-94A5-A94324693B72}");

            using (var context = getTestContext())
            {
                Assert.AreEqual(0, context.MultiIDEntities.AsQueryable().Count());

                var item = new MultiIDEntity
                {
                    Id = id1,
                    Item1Id = id2,
                    Item2Id = id3,
                    Flags = MyFlags.One,
                };

                Assert.IsTrue(context.MultiIDEntities.AddIfNotExists(item));
            }

            using (var context = getTestContext())
            {
                var item = new MultiIDEntity
                {
                    Id = id1,
                    Flags = MyFlags.Two,
                };

                Assert.IsFalse(context.MultiIDEntities.AddIfNotExists(item));
            }

            using (var context = getTestContext())
            {
                var item = context.MultiIDEntities.Get(id1);
                Assert.IsNotNull(item);

                Assert.AreEqual(MyFlags.One, item.Flags);
            }
        }

        [TestMethod]
        public void TestUnitOfWorkAddOrUpdate()
        {
            var id1 = new Guid("{E0E69222-7C20-4F16-B146-83C6C44FFCCA}");
            var id2 = new Guid("{4D50A826-9F0A-473F-8B16-E2CA2F71431E}");
            var id3 = new Guid("{87FBB4F8-9699-465B-94A5-A94324693B72}");

            using (var context = getTestContext())
            {
                Assert.AreEqual(0, context.MultiIDEntities.AsQueryable().Count());

                var item = new MultiIDEntity
                {
                    Id = id1,
                    Item1Id = id2,
                    Item2Id = id3,
                    Flags = MyFlags.One,
                };

                var existing = context.MultiIDEntities.GetOrAdd(item);
                Assert.IsNull(existing);
            }

            using (var context = getTestContext())
            {
                var item = new MultiIDEntity
                {
                    Id = id1,
                    Flags = MyFlags.Two,
                };

                context.MultiIDEntities.AddOrUpdate(item);
            }

            using (var context = getTestContext())
            {
                var item = context.MultiIDEntities.Get(id1);
                Assert.IsNotNull(item);

                Assert.AreEqual(MyFlags.Two, item.Flags);
            }
        }

        [TestMethod]
        public void TestUnitOfWorkSimpleUpdate()
        {
            var id1 = new Guid("{E0E69222-7C20-4F16-B146-83C6C44FFCCA}");
            var id2 = new Guid("{4D50A826-9F0A-473F-8B16-E2CA2F71431E}");
            var id3 = new Guid("{87FBB4F8-9699-465B-94A5-A94324693B72}");

            using (var context = getTestContext())
            {
                Assert.AreEqual(0, context.MultiIDEntities.AsQueryable().Count());

                var item = new MultiIDEntity
                {
                    Id = id1,
                    Item1Id = id2,
                    Item2Id = id3,
                    Flags = MyFlags.None,
                };

                context.MultiIDEntities.Add(item);
            }

            using (var context = getTestContext())
            {
                var item = context.MultiIDEntities.AsQueryable().Where(e => e.Item1Id == id2 && e.Item2Id == id3).SingleOrDefault();
                Assert.IsNotNull(item);

                item.Flags |= MyFlags.One;

                context.MultiIDEntities.Update(item);
            }

            using (var context = getTestContext())
            {
                var item = context.MultiIDEntities.Get(id1);
                Assert.IsNotNull(item);

                Assert.AreEqual(MyFlags.One, item.Flags);
            }
        }

        #endregion

        #region Various key types

        [TestMethod]
        public void BasicTestsOfUnitOfWorkWithGuidKeys()
        {
            var id1 = new Guid("{E0E69222-7C20-4F16-B146-83C6C44FFCCA}");
            var id2 = new Guid("{4D50A826-9F0A-473F-8B16-E2CA2F71431E}");
            var id3 = new Guid("{87FBB4F8-9699-465B-94A5-A94324693B72}");

            using (var context = getTestContext())
            {
                Assert.AreEqual(0, context.EntitiesWithGuid.AsQueryable().Count());

                var aaaa = new EntityWithGuid
                {
                    Id = id1,
                    Name = "aaaa",
                };

                var bbbb = new EntityWithGuid
                {
                    Id = id2,
                    Name = "bbbb",
                };

                var cccc = new EntityWithGuid
                {
                    Id = id3,
                    Name = "cccc",
                };

                context.EntitiesWithGuid.Add(new[] { aaaa, bbbb, cccc });
            }

            using (var context = getTestContext())
            {
                Assert.AreEqual(3, context.EntitiesWithGuid.AsQueryable().Count());

                var result = context.EntitiesWithGuid.Get(id2);

                Assert.AreEqual("bbbb", result.Name);

                Assert.IsTrue(context.EntitiesWithGuid.Contains(id1));
                Assert.IsTrue(context.EntitiesWithGuid.Contains(id2));
                Assert.IsTrue(context.EntitiesWithGuid.Contains(id3));

                context.EntitiesWithGuid.Delete(new[] { id1, id2, id3 });
                Assert.AreEqual(0, context.EntitiesWithGuid.AsQueryable().Count());
            }
        }

        [TestMethod]
        public void BasicTestsOfTestUnitOfWorkWithIntKeys()
        {
            var id1 = 5;
            var id2 = 3;
            var id3 = 7;

            using (var context = getTestContext())
            {
                Assert.AreEqual(0, context.EntitiesWithInt.AsQueryable().Count());

                var aaaa = new EntityWithInt
                {
                    Id = id1,
                    Name = "aaaa",
                };

                var bbbb = new EntityWithInt
                {
                    Id = id2,
                    Name = "bbbb",
                };

                var cccc = new EntityWithInt
                {
                    Id = id3,
                    Name = "cccc",
                };

                context.EntitiesWithInt.Add(new[] { aaaa, bbbb, cccc });
            }

            using (var context = getTestContext())
            {
                Assert.AreEqual(3, context.EntitiesWithInt.AsQueryable().Count());

                var result = context.EntitiesWithInt.Get(id2);

                Assert.AreEqual("bbbb", result.Name);

                Assert.IsTrue(context.EntitiesWithInt.Contains(id1));
                Assert.IsTrue(context.EntitiesWithInt.Contains(id2));
                Assert.IsTrue(context.EntitiesWithInt.Contains(id3));

                context.EntitiesWithInt.Delete(new[] { id1, id2, id3 });
                Assert.AreEqual(0, context.EntitiesWithInt.AsQueryable().Count());
            }
        }

        [TestMethod]
        public void BasicTestsOfTestUnitOfWorkWithStringKeys()
        {
            var id1 = "ttttttttttttttttttt";
            var id2 = "bbbbbbbbbbbbbbbbb";
            var id3 = "xxxxxxxxxxxxxxxxxxxxxxx";

            using (var context = getTestContext())
            {
                Assert.AreEqual(0, context.EntitiesWithString.AsQueryable().Count());

                var aaaa = new EntityWithString
                {
                    Id = id1,
                    Name = "aaaa",
                };

                var bbbb = new EntityWithString
                {
                    Id = id2,
                    Name = "bbbb",
                };

                var cccc = new EntityWithString
                {
                    Id = id3,
                    Name = "cccc",
                };

                context.EntitiesWithString.Add(new[] { aaaa, bbbb, cccc });
            }

            using (var context = getTestContext())
            {
                Assert.AreEqual(3, context.EntitiesWithString.AsQueryable().Count());

                var result = context.EntitiesWithString.Get(id2);

                Assert.AreEqual("bbbb", result.Name);

                Assert.IsTrue(context.EntitiesWithString.Contains(id1));
                Assert.IsTrue(context.EntitiesWithString.Contains(id2));
                Assert.IsTrue(context.EntitiesWithString.Contains(id3));

                context.EntitiesWithString.Delete(new[] { id1, id2, id3 });
                Assert.AreEqual(0, context.EntitiesWithString.AsQueryable().Count());
            }
        }

        #endregion

        #region Inheritance

        [TestMethod]
        public void BasicTestsOfTestUnitOfWorkWithGuidKeysAndInheritance()
        {
            var id1 = new Guid("{E0E69222-7C20-4F16-B146-83C6C44FFCCA}");
            var id2 = new Guid("{4D50A826-9F0A-473F-8B16-E2CA2F71431E}");
            var id3 = new Guid("{87FBB4F8-9699-465B-94A5-A94324693B72}");

            using (var context = getTestContext())
            {
                Assert.AreEqual(0, context.EntitiesWithGuid.AsQueryable().Count());

                var aaaa = new EntityWithGuid
                {
                    Id = id1,
                    Name = "aaaa",
                };

                var bbbb = new EntityWithGuid1
                {
                    Id = id2,
                    Name = "bbbb",
                    Prop1 = "1111",
                };

                var cccc = new EntityWithGuid2
                {
                    Id = id3,
                    Name = "cccc",
                    Prop2 = "2222",
                };

                context.EntitiesWithGuid.Add(new[] { aaaa, bbbb, cccc });
            }

            using (var context = getTestContext())
            {
                Assert.AreEqual(3, context.EntitiesWithGuid.AsQueryable().Count());

                var result = context.EntitiesWithGuid.Get(id2);

                Assert.AreEqual("bbbb", result.Name);

                Assert.IsTrue(context.EntitiesWithGuid.Contains(id1));
                Assert.IsTrue(context.EntitiesWithGuid.Contains(id2));
                Assert.IsTrue(context.EntitiesWithGuid.Contains(id3));

                context.EntitiesWithGuid.Delete(id1);
                context.EntitiesWithGuid.Delete(id2);
                context.EntitiesWithGuid.Delete(id3);
                Assert.AreEqual(0, context.EntitiesWithString.AsQueryable().Count());
            }
        }

        #endregion

        #region Special attributes

        [TestMethod]
        public void TestDbIgnore()
        {
            var id1 = new Guid("{A8EA2462-B641-40AE-A4AC-AF8373DF9B75}");

            using (var context = getTestContext())
            {
                Assert.AreEqual(0, context.EntityWithIgnoredData.AsQueryable().Count());

                var item = new Mock.EntityWithIgnoredData
                {
                    Id = id1,
                    Name = "xxx",
                    IgnoreThisString = "yyy",
                    IgnoreThisObject = new InnerClass { Property = 1 },
                    IgnoreThisStruct = new InnerStruct { Field = 2 },
                };

                context.EntityWithIgnoredData.Add(item);
            }

            using (var context = getTestContext())
            {
                var item = context.EntityWithIgnoredData.Get(id1);
                Assert.AreEqual("xxx", item.Name);
                Assert.AreEqual(null, item.IgnoreThisObject);
                Assert.AreEqual(default(InnerStruct), item.IgnoreThisStruct);
            }
        }

        [TestMethod]
        public void TestDbIgnoreWithInheritance()
        {
            var id1 = new Guid("{05144A99-A6A0-4D87-BB36-2CE0A91331DA}");

            using (var context = getTestContext())
            {
                Assert.AreEqual(0, context.InhertianceEntityWithIgnoredData.AsQueryable().Count());

                var item = new Mock.InhertianceEntityWithIgnoredDataChild
                {
                    Id = id1,
                    Name = "xxx",
                    IgnoreThisParentString = "abc",
                    IgnoreThisString = "yyy",
                    IgnoreThisObject = new InnerClass { Property = 1 },
                    IgnoreThisStruct = new InnerStruct { Field = 2 },
                };

                context.InhertianceEntityWithIgnoredData.Add(item);
            }

            using (var context = getTestContext())
            {
                var item = context.InhertianceEntityWithIgnoredData.Get(id1) as InhertianceEntityWithIgnoredDataChild;
                Assert.AreEqual("xxx", item.Name);
                Assert.AreEqual(null, item.IgnoreThisParentString);
                Assert.AreEqual(null, item.IgnoreThisString);
                Assert.AreEqual(null, item.IgnoreThisObject);
                Assert.AreEqual(default(InnerStruct), item.IgnoreThisStruct);
            }
        }

        [TestMethod]
        public void TestNavigation()
        {
            var id1 = new Guid("{1467E76D-F3C0-4314-ADA8-BB5FC1028CC2}");
            var id2 = new Guid("{1A5F098D-D5C7-4D19-99AB-6155F72A77E5}");
            var id3 = new Guid("{3AC03C43-8C89-4870-A4B0-F21F7459E1D0}");
            var id4 = new Guid("{A284498A-2F05-4449-8E91-9D471B598C9C}");

            using (var context = getTestContext())
            {
                Assert.AreEqual(0, context.EntitiesWithNavigation.AsQueryable().Count());

                var info = new Mock.Info
                {
                    Id = id1,
                    Name = "info-1-2-3",
                };

                var tag1 = new Mock.Tag
                {
                    Id = id2,
                    Name = "tag-a-b-c",
                };

                var tag2 = new Mock.Tag
                {
                    Id = id3,
                    Name = "tag-x-y-z",
                };

                var item = new Mock.EntityWithNavigation
                {
                    Id = id4,
                    Name = "xxx",
                    Info = info,
                    Tags = new List<Tag> { tag1, tag2 },
                };

                context.EntitiesWithNavigation.Add(item);
                context.EntitiesWithNavigation.SaveNavigation(new[] { item }, new[] { "Info", "Tags" });
            }

            using (var context = getTestContext())
            {
                var item = context.EntitiesWithNavigation.Get(id4);
                Assert.AreEqual("xxx", item.Name);
                Assert.AreNotEqual(Guid.Empty, item.InfoId);
                Assert.IsNull(item.Info);
                Assert.IsTrue(item.TagIds != null && item.TagIds.Count() == 2);
                Assert.IsTrue(item.Tags == null || !item.Tags.Any());

                context.EntitiesWithNavigation.LoadNavigation(new[] { item }, new[] { "Info", "Tags" });
                Assert.AreNotEqual(Guid.Empty, item.InfoId);
                Assert.IsNotNull(item.Info);
                Assert.IsTrue(item.TagIds != null && item.TagIds.Count() == 2);
                Assert.IsTrue(item.Tags != null && item.Tags.Count() == 2);
            }
        }

        #endregion

        #region Events - basic

        [TestMethod]
        public void TestEvents()
        {
            var id1 = new Guid("{B09F4B8E-1853-4C01-84D8-57C87A1F7F31}");

            using (var context = getTestContext())
            {
                int modifyCount = 0;
                int deleteCount = 0;
                int getCount = 0;

                context.EntitiesWithGuid.OnModifying += (obj, e) => modifyCount++;
                context.EntitiesWithGuid.OnDeleting += (obj, e) => deleteCount++;
                context.EntitiesWithGuid.OnGetting += (obj, e) => getCount++;

                Assert.AreEqual(0, context.EntitiesWithGuid.AsQueryable().Count());

                var entity = new Mock.EntityWithGuid
                {
                    Id = id1,
                    Name = "access-test",
                };

                context.EntitiesWithGuid.Add(entity);

                Assert.AreEqual(1, modifyCount);
                Assert.AreEqual(0, deleteCount);
                Assert.AreEqual(0, getCount);

                var item = context.EntitiesWithGuid.Get(id1);
                Assert.AreEqual("access-test", item.Name);

                Assert.AreEqual(1, modifyCount);
                Assert.AreEqual(0, deleteCount);
                Assert.AreEqual(1, getCount);
            }
        }

        [TestMethod]
        public void TestEventsWithDeleteByObj()
        {
            var id1 = new Guid("{B09F4B8E-1853-4C01-84D8-57C87A1F7F31}");

            using (var context = getTestContext())
            {
                int modifyCount = 0;
                int deleteCount = 0;
                int getCount = 0;

                context.EntitiesWithGuid.OnModifying += (obj, e) => modifyCount++;
                context.EntitiesWithGuid.OnDeleting += (obj, e) => deleteCount++;
                context.EntitiesWithGuid.OnGetting += (obj, e) => getCount++;

                Assert.AreEqual(0, context.EntitiesWithGuid.AsQueryable().Count());

                var entity = new Mock.EntityWithGuid
                {
                    Id = id1,
                    Name = "access-test",
                };

                context.EntitiesWithGuid.Add(entity);

                Assert.AreEqual(1, modifyCount);
                Assert.AreEqual(0, deleteCount);
                Assert.AreEqual(0, getCount);

                var item = context.EntitiesWithGuid.Get(id1);
                Assert.AreEqual("access-test", item.Name);

                Assert.AreEqual(1, modifyCount);
                Assert.AreEqual(0, deleteCount);
                Assert.AreEqual(1, getCount);

                context.EntitiesWithGuid.Delete(entity);

                Assert.AreEqual(1, modifyCount);
                Assert.AreEqual(1, deleteCount);
                Assert.AreEqual(1, getCount);
            }
        }

        [TestMethod]
        public void TestEventsWithDeleteById()
        {
            var id1 = new Guid("{B09F4B8E-1853-4C01-84D8-57C87A1F7F31}");

            using (var context = getTestContext())
            {
                int modifyCount = 0;
                int deleteCount = 0;
                int getCount = 0;

                context.EntitiesWithGuid.OnModifying += (obj, e) => modifyCount++;
                context.EntitiesWithGuid.OnDeleting += (obj, e) => deleteCount++;
                context.EntitiesWithGuid.OnGetting += (obj, e) => getCount++;

                Assert.AreEqual(0, context.EntitiesWithGuid.AsQueryable().Count());

                var entity = new Mock.EntityWithGuid
                {
                    Id = id1,
                    Name = "access-test",
                };

                context.EntitiesWithGuid.Add(entity);

                Assert.AreEqual(1, modifyCount);
                Assert.AreEqual(0, deleteCount);
                Assert.AreEqual(0, getCount);

                var item = context.EntitiesWithGuid.Get(id1);
                Assert.AreEqual("access-test", item.Name);

                Assert.AreEqual(1, modifyCount);
                Assert.AreEqual(0, deleteCount);
                Assert.AreEqual(1, getCount);

                context.EntitiesWithGuid.Delete(id1);

                Assert.AreEqual(1, modifyCount);
                Assert.AreEqual(1, deleteCount);
                Assert.AreEqual(2, getCount);
            }
        }

        #endregion

        #region Events - access management

        private void PrepareAccessTestsData(
            out Guid ida1, out Guid ida2, out EntityA a1, out EntityA a2)
        {
            ida1 = new Guid("{0FE7B0E0-F109-4807-BC36-E1C33822C798}");
            ida2 = new Guid("{C8A36DE3-F65A-4D16-9690-D7BE4F9F6976}");

            using (var context = getTestContextForAccessTests(
                Status.Private, Status.Private, Status.Private))
            {
                Assert.AreEqual(0, context.As.AsQueryable().Count());

                a1 = new MocksForAccessTest.EntityA
                {
                    Id = ida1,
                    Text = "a1",
                    Status = Status.Public,
                };
                context.As.Add(a1);

                a2 = new MocksForAccessTest.EntityA
                {
                    Id = ida2,
                    Text = "a2",
                    Status = Status.Private,
                };
                context.As.Add(a2);
            }
        }

        [TestMethod]
        public void TestAccessRestriction_FullAccess()
        {
            Guid id1, id2;
            EntityA a1, a2;
            PrepareAccessTestsData(out id1, out id2, out a1, out a2);

            using (var context = getTestContextForAccessTests(Status.Private, Status.Private, Status.Private))
            {
                var set = context.As;

                var o1 = set.Get(id1);
                var o2 = set.Get(id2);
                set.Update(o1);
                set.Update(o2);
                set.Delete(id1);
                set.Delete(id2);
                set.Add(o1);
                set.Add(o2);
                set.Delete(o1);
                set.Delete(o2);
                set.AddIfNotExists(o1);
                set.AddIfNotExists(o2);
                set.AddIfNotExists(o1);
                set.AddIfNotExists(o2);
                set.Delete(o1);
                set.Delete(o2);
                set.AddOrUpdate(o1);
                set.AddOrUpdate(o2);
                set.AddOrUpdate(o1);
                set.AddOrUpdate(o2);
            }
        }

        [TestMethod]
        public void TestAccessRestriction_PartialAccess_OK()
        {
            Guid id1, id2;
            EntityA a1, a2;
            PrepareAccessTestsData(out id1, out id2, out a1, out a2);

            using (var context = getTestContextForAccessTests(Status.Public, Status.Public, Status.Public))
            {
                var set = context.As;

                var o1 = set.Get(id1);
                set.Update(o1);
                set.Delete(id1);
                set.Add(o1);
                set.Delete(o1);
                set.AddIfNotExists(o1);
                set.AddIfNotExists(o1);
                set.Delete(o1);
                set.AddOrUpdate(o1);
                set.AddOrUpdate(o1);
            }
        }

        [TestMethod, ExpectedException(typeof(DalAccessRestrictionFailure))]
        public void TestAccessRestriction_PartialAccess_Get_Fail()
        {
            Guid id1, id2;
            EntityA a1, a2;
            PrepareAccessTestsData(out id1, out id2, out a1, out a2);

            using (var context = getTestContextForAccessTests(Status.Public, Status.Public, Status.Public))
            {
                var set = context.As;

                var o1 = set.Get(id2);
            }
        }

        [TestMethod, ExpectedException(typeof(DalAccessRestrictionFailure))]
        public void TestAccessRestriction_PartialAccess_Update_Fail()
        {
            Guid id1, id2;
            EntityA a1, a2;
            PrepareAccessTestsData(out id1, out id2, out a1, out a2);

            using (var context = getTestContextForAccessTests(Status.Public, Status.Public, Status.Public))
            {
                var set = context.As;

                set.Update(a2);
            }
        }

        [TestMethod, ExpectedException(typeof(DalAccessRestrictionFailure))]
        public void TestAccessRestriction_PartialAccess_Delete_Fail()
        {
            Guid id1, id2;
            EntityA a1, a2;
            PrepareAccessTestsData(out id1, out id2, out a1, out a2);

            using (var context = getTestContextForAccessTests(Status.Public, Status.Public, Status.Public))
            {
                var set = context.As;

                set.Delete(id2);
            }
        }

        [TestMethod, ExpectedException(typeof(DalAccessRestrictionFailure))]
        public void TestAccessRestriction_PartialAccess_Add_Fail()
        {
            Guid id1, id2;
            EntityA a1, a2;
            PrepareAccessTestsData(out id1, out id2, out a1, out a2);

            using (var context = getTestContextForAccessTests(Status.Public, Status.Public, Status.Public))
            {
                var set = context.As;

                var o1 = set.Get(id1);
                o1.Status = Status.Private;
                set.Add(o1);
            }
        }

        [TestMethod]
        public void TestAccessRestriction_PartialAccess_AddIfNotExists_Exists_OK()
        {
            Guid id1, id2;
            EntityA a1, a2;
            PrepareAccessTestsData(out id1, out id2, out a1, out a2);

            using (var context = getTestContextForAccessTests(Status.Public, Status.Public, Status.Public))
            {
                var set = context.As;

                set.AddIfNotExists(a2);
            }
        }

        [TestMethod, ExpectedException(typeof(DalAccessRestrictionFailure))]
        public void TestAccessRestriction_PartialAccess_AddIfNotExists_NotExists_Fail()
        {
            Guid id1, id2;
            EntityA a1, a2;
            PrepareAccessTestsData(out id1, out id2, out a1, out a2);

            using (var context = getTestContextForAccessTests(Status.Public, Status.Public, Status.Private))
            {
                var set = context.As;

                var o3 = new EntityA
                {
                    Id = Guid.NewGuid(),
                    Text = "o3",
                    Status = Status.Private,
                };

                set.AddIfNotExists(o3);
            }
        }

        [TestMethod, ExpectedException(typeof(DalAccessRestrictionFailure))]
        public void TestAccessRestriction_PartialAccess_AddOrUpdate_Update_Fail()
        {
            Guid id1, id2;
            EntityA a1, a2;
            PrepareAccessTestsData(out id1, out id2, out a1, out a2);

            using (var context = getTestContextForAccessTests(Status.Public, Status.Public, Status.Public))
            {
                var set = context.As;

                set.AddOrUpdate(a2);
            }
        }

        [TestMethod, ExpectedException(typeof(DalAccessRestrictionFailure))]
        public void TestAccessRestriction_PartialAccess_AddOrUpdate_Add_Fail()
        {
            Guid id1, id2;
            EntityA a1, a2;
            PrepareAccessTestsData(out id1, out id2, out a1, out a2);

            using (var context = getTestContextForAccessTests(Status.Public, Status.Public, Status.Public))
            {
                var set = context.As;

                var o3 = new EntityA
                {
                    Id = Guid.NewGuid(),
                    Text = "o3",
                    Status = Status.Private,
                };

                set.AddOrUpdate(o3);
            }
        }

        #endregion

        #region Reduction

        protected void PrepareReductionData(Guid id1, Guid id2, int numberOfTransactions, out double total1, out double total2)
        {
            using (var context = getTestContext())
            {
                context.EntitiesWithGuid.Add(new EntityWithGuid
                {
                    Id = id1,
                    Name = "a"
                });

                context.EntitiesWithGuid.Add(new EntityWithGuid
                {
                    Id = id2,
                    Name = "b"
                });

                var rand = new Random(0);
                total1 = 0.0;
                total2 = 0.0;

                for (int cnt = 0; cnt < numberOfTransactions; cnt++)
                {
                    var t1 = new EntityForCalc
                    {
                        Id = Guid.NewGuid(),
                        OwnerId = id1,
                        Date = new DateTime(2014, 1, 1).AddDays(cnt),
                        Money = rand.NextDouble() * 1000.0 - 500.0,
                    };

                    var t2 = new EntityForCalc
                    {
                        Id = Guid.NewGuid(),
                        OwnerId = id2,
                        Date = new DateTime(2014, 1, 3).AddDays(cnt),
                        Money = rand.NextDouble() * 1000.0 - 500.0,
                    };

                    context.EntitiesForCalc.Add(t1);
                    total1 += t1.Money;

                    context.EntitiesForCalc.Add(t2);
                    total2 += t2.Money;
                }
            }
        }

        [TestMethod]
        public void TestReductionViaLinq()
        {
            Guid id1 = Guid.NewGuid();
            Guid id2 = Guid.NewGuid();

            int numberOfTransactions = 1000;
            double total1, total2;
            PrepareReductionData(id1, id2, numberOfTransactions, out total1, out total2);

            using (var context = getTestContext())
            {
                var results = context.EntitiesForCalc.AsQueryable()
                    .Where(e => e.OwnerId == id1)
                    .ToList()
                    .Select(e => new { OwnerId = e.OwnerId, NumberOfTransactions = 1, Money = e.Money })
                    .GroupBy(e => e.OwnerId)
                    .Select(g => g.Aggregate((a, b) =>
                    new
                    {
                        OwnerId = a.OwnerId,
                        NumberOfTransactions = a.NumberOfTransactions + b.NumberOfTransactions,
                        Money = a.Money + b.Money
                    }))
                    .Select(r => new { OwnerId = r.OwnerId, NumberOfTransactions = r.NumberOfTransactions, Money = r.Money - 0.1 })
                    .ToList();

                Assert.AreEqual(1, results.Count);

                Assert.AreEqual(id1, results[0].OwnerId);

                Assert.AreEqual(numberOfTransactions, results[0].NumberOfTransactions);
                Assert.IsTrue(Math.Abs(total1 - 0.1 - results[0].Money) <= 0.1);
            }
        }

        [TestMethod]
        public void TestReductionWithJoinViaLinq()
        {
            Guid id1 = Guid.NewGuid();
            Guid id2 = Guid.NewGuid();

            int numberOfTransactions = 1000;
            double total1, total2;
            PrepareReductionData(id1, id2, numberOfTransactions, out total1, out total2);

            using (var context = getTestContext())
            {
                var query1 = context.EntitiesWithGuid.AsQueryable()
                    .Where(e => e.Id == id1)
                    .ToList()
                    .Select(e => new { OwnerId = e.Id, Name = e.Name, NumberOfTransactions = 0, Money = 0.0 });

                var query2 = context.EntitiesForCalc.AsQueryable()
                    .Where(e => e.OwnerId == id1)
                    .ToList()
                    .Select(e => new { OwnerId = e.OwnerId, Name = (string)null, NumberOfTransactions = 1, Money = e.Money });

                var results = query1.Concat(query2)
                    .GroupBy(e => e.OwnerId)
                    .Select(g => g.Aggregate((a, b) =>
                    new
                    {
                        OwnerId = a.OwnerId,
                        Name = a.Name ?? b.Name,
                        NumberOfTransactions = a.NumberOfTransactions + b.NumberOfTransactions,
                        Money = a.Money + b.Money
                    }))
                    .Select(r =>
                        new
                        {
                            OwnerId = r.OwnerId,
                            Name = r.Name,
                            NumberOfTransactions = r.NumberOfTransactions,
                            Money = r.Money - 0.1
                        })
                    .ToList();

                Assert.AreEqual(1, results.Count);

                Assert.AreEqual(id1, results[0].OwnerId);

                Assert.AreEqual("a", results[0].Name);
                Assert.AreEqual(numberOfTransactions, results[0].NumberOfTransactions);
                Assert.IsTrue(Math.Abs(total1 - 0.1 - results[0].Money) <= 0.1);
            }
        }

        #endregion

        #region Special cases
        
        [TestMethod]
        public void TestUpdateWithNullValue()
        {
            var entity = new EntityWithNavigation
            {
                Id = Guid.NewGuid(),
                Name = "x",
            };

            using (var context = getTestContext())
            {
                context.EntitiesWithNavigation.Add(entity);
            }

            using (var context = getTestContext())
            {
                context.EntitiesWithNavigation.Update(entity);
            }
        }

        #endregion
    }
}
