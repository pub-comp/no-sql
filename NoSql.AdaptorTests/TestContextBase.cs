using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.NoSql.Core;
using PubComp.NoSql.AdaptorTests.Mock;
using PubComp.NoSql.AdaptorTests.MocksForAccessTest;

namespace PubComp.NoSql.AdaptorTests
{
    public abstract class TestContextBase
    {
        protected readonly Func<IMockContext> getMockContext;
        protected GetMockContextForAccessTestsDelegate getMockContextForAccessTests;
        protected readonly Action<IMockContext> deleteMockContext;
        protected readonly Action<IMockContextForAccessTests> deleteMockContextForAccessTests;

        protected TestContextBase(
            Func<IMockContext> getMockContext,
            GetMockContextForAccessTestsDelegate getMockContextForAccessTests,
            Action<IMockContext> deleteMockContext,
            Action<IMockContextForAccessTests> deleteMockContextForAccessTests)
        {
            this.getMockContext = getMockContext;
            this.getMockContextForAccessTests = getMockContextForAccessTests;
            this.deleteMockContext = deleteMockContext;
            this.deleteMockContextForAccessTests = deleteMockContextForAccessTests;
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
            using (var uow = getMockContext())
            {
                deleteMockContext(uow);
            }

            using (var uow = getMockContextForAccessTests(Status.Private, Status.Private, Status.Private))
            {
                deleteMockContextForAccessTests(uow);
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

            using (var uow = getMockContext())
            {
                var set = uow.EntitiesWithGuid;

                var o1 = new EntityWithGuid
                {
                    Id = id,
                    Name = "o1",
                };

                set.Add(o1);
            }

            using (var uow = getMockContext())
            {
                var set = uow.EntitiesWithGuid;

                var o2 = set.Get(id);

                Assert.AreEqual("o1", o2.Name);
            }
        }

        [TestMethod]
        public void TestAdd_Update()
        {
            var id = Guid.NewGuid();

            using (var uow = getMockContext())
            {
                var set = uow.EntitiesWithGuid;

                var o1 = new EntityWithGuid
                {
                    Id = id,
                    Name = "o1",
                };

                set.Add(o1);
            }

            using (var uow = getMockContext())
            {
                var set = uow.EntitiesWithGuid;

                var o2 = new EntityWithGuid
                {
                    Id = id,
                    Name = "o1-updated",
                };

                set.Update(o2);
            }

            using (var uow = getMockContext())
            {
                var set = uow.EntitiesWithGuid;

                var o3 = set.Get(id);

                Assert.AreEqual("o1-updated", o3.Name);
            }
        }

        [TestMethod]
        public void TestAdd_DeleteById()
        {
            var id = Guid.NewGuid();

            using (var uow = getMockContext())
            {
                var set = uow.EntitiesWithGuid;

                var o1 = new EntityWithGuid
                {
                    Id = id,
                    Name = "o1",
                };

                set.Add(o1);
            }

            using (var uow = getMockContext())
            {
                var set = uow.EntitiesWithGuid;

                set.Delete(id);
            }

            using (var uow = getMockContext())
            {
                var set = uow.EntitiesWithGuid;

                var o3 = set.Get(id);

                Assert.IsNull(o3);
            }
        }

        [TestMethod]
        public void TestAdd_DeleteByObj()
        {
            var id = Guid.NewGuid();

            EntityWithGuid o1;

            using (var uow = getMockContext())
            {
                var set = uow.EntitiesWithGuid;

                o1 = new EntityWithGuid
                {
                    Id = id,
                    Name = "o1",
                };

                set.Add(o1);
            }

            using (var uow = getMockContext())
            {
                var set = uow.EntitiesWithGuid;

                set.Delete(o1);
            }

            using (var uow = getMockContext())
            {
                var set = uow.EntitiesWithGuid;

                var o3 = set.Get(id);

                Assert.IsNull(o3);
            }
        }

        [TestMethod]
        public void TestAdd_AddIfNotExists()
        {
            var id = Guid.NewGuid();

            using (var uow = getMockContext())
            {
                var set = uow.EntitiesWithGuid;

                var o1 = new EntityWithGuid
                {
                    Id = id,
                    Name = "o1",
                };

                set.Add(o1);
            }

            using (var uow = getMockContext())
            {
                var set = uow.EntitiesWithGuid;

                var o2 = new EntityWithGuid
                {
                    Id = id,
                    Name = "o1-updated",
                };

                var added = set.AddIfNotExists(o2);
                Assert.IsFalse(added);
            }

            using (var uow = getMockContext())
            {
                var set = uow.EntitiesWithGuid;

                var o3 = set.Get(id);

                Assert.AreEqual("o1", o3.Name);
            }
        }

        [TestMethod]
        public void TestAddIfNotExists()
        {
            var id = Guid.NewGuid();

            using (var uow = getMockContext())
            {
                var set = uow.EntitiesWithGuid;

                var o2 = new EntityWithGuid
                {
                    Id = id,
                    Name = "o2",
                };

                var added = set.AddIfNotExists(o2);
                Assert.IsTrue(added);
            }

            using (var uow = getMockContext())
            {
                var set = uow.EntitiesWithGuid;

                var o3 = set.Get(id);

                Assert.AreEqual("o2", o3.Name);
            }
        }

        [TestMethod]
        public void TestAdd_AddOrUpdate()
        {
            var id = Guid.NewGuid();

            using (var uow = getMockContext())
            {
                var set = uow.EntitiesWithGuid;

                var o1 = new EntityWithGuid
                {
                    Id = id,
                    Name = "o1",
                };

                set.Add(o1);
            }

            using (var uow = getMockContext())
            {
                var set = uow.EntitiesWithGuid;

                var o2 = new EntityWithGuid
                {
                    Id = id,
                    Name = "o1-updated",
                };

                set.AddOrUpdate(o2);
            }

            using (var uow = getMockContext())
            {
                var set = uow.EntitiesWithGuid;

                var o3 = set.Get(id);

                Assert.AreEqual("o1-updated", o3.Name);
            }
        }

        [TestMethod]
        public void TestAddOrUpdate()
        {
            var id = Guid.NewGuid();

            using (var uow = getMockContext())
            {
                var set = uow.EntitiesWithGuid;

                var o2 = new EntityWithGuid
                {
                    Id = id,
                    Name = "o2",
                };

                set.AddOrUpdate(o2);
            }

            using (var uow = getMockContext())
            {
                var set = uow.EntitiesWithGuid;

                var o3 = set.Get(id);

                Assert.AreEqual("o2", o3.Name);
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

            using (var uow = getMockContext())
            {
                Assert.AreEqual(0, uow.MultiIDEntities.AsQueryable().Count());

                var item = new MultiIDEntity
                {
                    Id = id1,
                    Item1Id = id2,
                    Item2Id = id3,
                    Flags = MyFlags.One,
                };

                var existing = uow.MultiIDEntities.GetOrAdd(item);
                Assert.IsNull(existing);
            }

            using (var uow = getMockContext())
            {
                var item = new MultiIDEntity
                {
                    Id = id1,
                    Flags = MyFlags.Two,
                };

                var existing = uow.MultiIDEntities.GetOrAdd(item);
                Assert.IsNotNull(existing);
                Assert.AreEqual(MyFlags.One, existing.Flags);
            }

            using (var uow = getMockContext())
            {
                var item = uow.MultiIDEntities.Get(id1);
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

            using (var uow = getMockContext())
            {
                Assert.AreEqual(0, uow.MultiIDEntities.AsQueryable().Count());

                var item = new MultiIDEntity
                {
                    Id = id1,
                    Item1Id = id2,
                    Item2Id = id3,
                    Flags = MyFlags.One,
                };

                Assert.IsTrue(uow.MultiIDEntities.AddIfNotExists(item));
            }

            using (var uow = getMockContext())
            {
                var item = new MultiIDEntity
                {
                    Id = id1,
                    Flags = MyFlags.Two,
                };

                Assert.IsFalse(uow.MultiIDEntities.AddIfNotExists(item));
            }

            using (var uow = getMockContext())
            {
                var item = uow.MultiIDEntities.Get(id1);
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

            using (var uow = getMockContext())
            {
                Assert.AreEqual(0, uow.MultiIDEntities.AsQueryable().Count());

                var item = new MultiIDEntity
                {
                    Id = id1,
                    Item1Id = id2,
                    Item2Id = id3,
                    Flags = MyFlags.One,
                };

                var existing = uow.MultiIDEntities.GetOrAdd(item);
                Assert.IsNull(existing);
            }

            using (var uow = getMockContext())
            {
                var item = new MultiIDEntity
                {
                    Id = id1,
                    Flags = MyFlags.Two,
                };

                uow.MultiIDEntities.AddOrUpdate(item);
            }

            using (var uow = getMockContext())
            {
                var item = uow.MultiIDEntities.Get(id1);
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

            using (var uow = getMockContext())
            {
                Assert.AreEqual(0, uow.MultiIDEntities.AsQueryable().Count());

                var item = new MultiIDEntity
                {
                    Id = id1,
                    Item1Id = id2,
                    Item2Id = id3,
                    Flags = MyFlags.None,
                };

                uow.MultiIDEntities.Add(item);
            }

            using (var uow = getMockContext())
            {
                var item = uow.MultiIDEntities.AsQueryable().Where(e => e.Item1Id == id2 && e.Item2Id == id3).SingleOrDefault();
                Assert.IsNotNull(item);

                item.Flags |= MyFlags.One;

                uow.MultiIDEntities.Update(item);
            }

            using (var uow = getMockContext())
            {
                var item = uow.MultiIDEntities.Get(id1);
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

            using (var uow = getMockContext())
            {
                Assert.AreEqual(0, uow.EntitiesWithGuid.AsQueryable().Count());

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

                uow.EntitiesWithGuid.Add(new[] { aaaa, bbbb, cccc });
            }

            using (var uow = getMockContext())
            {
                Assert.AreEqual(3, uow.EntitiesWithGuid.AsQueryable().Count());

                var result = uow.EntitiesWithGuid.Get(id2);

                Assert.AreEqual("bbbb", result.Name);

                Assert.IsTrue(uow.EntitiesWithGuid.Contains(id1));
                Assert.IsTrue(uow.EntitiesWithGuid.Contains(id2));
                Assert.IsTrue(uow.EntitiesWithGuid.Contains(id3));

                uow.EntitiesWithGuid.Delete(new[] { id1, id2, id3 });
                Assert.AreEqual(0, uow.EntitiesWithGuid.AsQueryable().Count());
            }
        }

        [TestMethod]
        public void BasicTestsOfTestUnitOfWorkWithIntKeys()
        {
            var id1 = 5;
            var id2 = 3;
            var id3 = 7;

            using (var uow = getMockContext())
            {
                Assert.AreEqual(0, uow.EntitiesWithInt.AsQueryable().Count());

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

                uow.EntitiesWithInt.Add(new[] { aaaa, bbbb, cccc });
            }

            using (var uow = getMockContext())
            {
                Assert.AreEqual(3, uow.EntitiesWithInt.AsQueryable().Count());

                var result = uow.EntitiesWithInt.Get(id2);

                Assert.AreEqual("bbbb", result.Name);

                Assert.IsTrue(uow.EntitiesWithInt.Contains(id1));
                Assert.IsTrue(uow.EntitiesWithInt.Contains(id2));
                Assert.IsTrue(uow.EntitiesWithInt.Contains(id3));

                uow.EntitiesWithInt.Delete(new[] { id1, id2, id3 });
                Assert.AreEqual(0, uow.EntitiesWithInt.AsQueryable().Count());
            }
        }

        [TestMethod]
        public void BasicTestsOfTestUnitOfWorkWithStringKeys()
        {
            var id1 = "ttttttttttttttttttt";
            var id2 = "bbbbbbbbbbbbbbbbb";
            var id3 = "xxxxxxxxxxxxxxxxxxxxxxx";

            using (var uow = getMockContext())
            {
                Assert.AreEqual(0, uow.EntitiesWithString.AsQueryable().Count());

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

                uow.EntitiesWithString.Add(new[] { aaaa, bbbb, cccc });
            }

            using (var uow = getMockContext())
            {
                Assert.AreEqual(3, uow.EntitiesWithString.AsQueryable().Count());

                var result = uow.EntitiesWithString.Get(id2);

                Assert.AreEqual("bbbb", result.Name);

                Assert.IsTrue(uow.EntitiesWithString.Contains(id1));
                Assert.IsTrue(uow.EntitiesWithString.Contains(id2));
                Assert.IsTrue(uow.EntitiesWithString.Contains(id3));

                uow.EntitiesWithString.Delete(new[] { id1, id2, id3 });
                Assert.AreEqual(0, uow.EntitiesWithString.AsQueryable().Count());
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

            using (var uow = getMockContext())
            {
                Assert.AreEqual(0, uow.EntitiesWithGuid.AsQueryable().Count());

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

                uow.EntitiesWithGuid.Add(new[] { aaaa, bbbb, cccc });
            }

            using (var uow = getMockContext())
            {
                Assert.AreEqual(3, uow.EntitiesWithGuid.AsQueryable().Count());

                var result = uow.EntitiesWithGuid.Get(id2);

                Assert.AreEqual("bbbb", result.Name);

                Assert.IsTrue(uow.EntitiesWithGuid.Contains(id1));
                Assert.IsTrue(uow.EntitiesWithGuid.Contains(id2));
                Assert.IsTrue(uow.EntitiesWithGuid.Contains(id3));

                uow.EntitiesWithGuid.Delete(id1);
                uow.EntitiesWithGuid.Delete(id2);
                uow.EntitiesWithGuid.Delete(id3);
                Assert.AreEqual(0, uow.EntitiesWithString.AsQueryable().Count());
            }
        }

        #endregion

        #region Special attributes

        [TestMethod]
        public void TestDbIgnore()
        {
            var id1 = new Guid("{A8EA2462-B641-40AE-A4AC-AF8373DF9B75}");

            using (var uow = getMockContext())
            {
                Assert.AreEqual(0, uow.MultiIDEntities.AsQueryable().Count());

                var item = new Mock.EntityWithAdditionalData
                {
                    Id = id1,
                    Name = "xxx",
                    AdditionalData = "yyy",
                };

                uow.EntitiesWithAdditionalData.Add(item);
            }

            using (var uow = getMockContext())
            {
                var item = uow.EntitiesWithAdditionalData.Get(id1);
                Assert.AreEqual("xxx", item.Name);
                Assert.AreEqual(null, item.AdditionalData);
            }
        }

        [TestMethod]
        public void TestNavigation()
        {
            var id1 = new Guid("{1467E76D-F3C0-4314-ADA8-BB5FC1028CC2}");
            var id2 = new Guid("{1A5F098D-D5C7-4D19-99AB-6155F72A77E5}");
            var id3 = new Guid("{3AC03C43-8C89-4870-A4B0-F21F7459E1D0}");
            var id4 = new Guid("{A284498A-2F05-4449-8E91-9D471B598C9C}");

            using (var uow = getMockContext())
            {
                Assert.AreEqual(0, uow.EntitiesWithNavigation.AsQueryable().Count());

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

                uow.EntitiesWithNavigation.Add(item);
                uow.EntitiesWithNavigation.SaveNavigation(new[] { item }, new[] { "Info", "Tags" });
            }

            using (var uow = getMockContext())
            {
                var item = uow.EntitiesWithNavigation.Get(id4);
                Assert.AreEqual("xxx", item.Name);
                Assert.AreNotEqual(Guid.Empty, item.InfoId);
                Assert.IsNull(item.Info);
                Assert.IsTrue(item.TagIds != null && item.TagIds.Count() == 2);
                Assert.IsTrue(item.Tags == null || !item.Tags.Any());

                uow.EntitiesWithNavigation.LoadNavigation(new[] { item }, new[] { "Info", "Tags" });
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

            using (var uow = getMockContext())
            {
                int modifyCount = 0;
                int deleteCount = 0;
                int getCount = 0;

                uow.EntitiesWithGuid.OnModifying += (obj, e) => modifyCount++;
                uow.EntitiesWithGuid.OnDeleting += (obj, e) => deleteCount++;
                uow.EntitiesWithGuid.OnGetting += (obj, e) => getCount++;

                Assert.AreEqual(0, uow.EntitiesWithGuid.AsQueryable().Count());

                var entity = new Mock.EntityWithGuid
                {
                    Id = id1,
                    Name = "access-test",
                };

                uow.EntitiesWithGuid.Add(entity);

                Assert.AreEqual(1, modifyCount);
                Assert.AreEqual(0, deleteCount);
                Assert.AreEqual(0, getCount);

                var item = uow.EntitiesWithGuid.Get(id1);
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

            using (var uow = getMockContext())
            {
                int modifyCount = 0;
                int deleteCount = 0;
                int getCount = 0;

                uow.EntitiesWithGuid.OnModifying += (obj, e) => modifyCount++;
                uow.EntitiesWithGuid.OnDeleting += (obj, e) => deleteCount++;
                uow.EntitiesWithGuid.OnGetting += (obj, e) => getCount++;

                Assert.AreEqual(0, uow.EntitiesWithGuid.AsQueryable().Count());

                var entity = new Mock.EntityWithGuid
                {
                    Id = id1,
                    Name = "access-test",
                };

                uow.EntitiesWithGuid.Add(entity);

                Assert.AreEqual(1, modifyCount);
                Assert.AreEqual(0, deleteCount);
                Assert.AreEqual(0, getCount);

                var item = uow.EntitiesWithGuid.Get(id1);
                Assert.AreEqual("access-test", item.Name);

                Assert.AreEqual(1, modifyCount);
                Assert.AreEqual(0, deleteCount);
                Assert.AreEqual(1, getCount);

                uow.EntitiesWithGuid.Delete(entity);

                Assert.AreEqual(1, modifyCount);
                Assert.AreEqual(1, deleteCount);
                Assert.AreEqual(1, getCount);
            }
        }

        [TestMethod]
        public void TestEventsWithDeleteById()
        {
            var id1 = new Guid("{B09F4B8E-1853-4C01-84D8-57C87A1F7F31}");

            using (var uow = getMockContext())
            {
                int modifyCount = 0;
                int deleteCount = 0;
                int getCount = 0;

                uow.EntitiesWithGuid.OnModifying += (obj, e) => modifyCount++;
                uow.EntitiesWithGuid.OnDeleting += (obj, e) => deleteCount++;
                uow.EntitiesWithGuid.OnGetting += (obj, e) => getCount++;

                Assert.AreEqual(0, uow.EntitiesWithGuid.AsQueryable().Count());

                var entity = new Mock.EntityWithGuid
                {
                    Id = id1,
                    Name = "access-test",
                };

                uow.EntitiesWithGuid.Add(entity);

                Assert.AreEqual(1, modifyCount);
                Assert.AreEqual(0, deleteCount);
                Assert.AreEqual(0, getCount);

                var item = uow.EntitiesWithGuid.Get(id1);
                Assert.AreEqual("access-test", item.Name);

                Assert.AreEqual(1, modifyCount);
                Assert.AreEqual(0, deleteCount);
                Assert.AreEqual(1, getCount);

                uow.EntitiesWithGuid.Delete(id1);

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

            using (var uow = getMockContextForAccessTests(
                Status.Private, Status.Private, Status.Private))
            {
                Assert.AreEqual(0, uow.As.AsQueryable().Count());

                a1 = new MocksForAccessTest.EntityA
                {
                    Id = ida1,
                    Text = "a1",
                    Status = Status.Public,
                };
                uow.As.Add(a1);

                a2 = new MocksForAccessTest.EntityA
                {
                    Id = ida2,
                    Text = "a2",
                    Status = Status.Private,
                };
                uow.As.Add(a2);
            }
        }

        [TestMethod]
        public void TestAccessRestriction_FullAccess()
        {
            Guid id1, id2;
            EntityA a1, a2;
            PrepareAccessTestsData(out id1, out id2, out a1, out a2);

            using (var uow = getMockContextForAccessTests(Status.Private, Status.Private, Status.Private))
            {
                var set = uow.As;

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

            using (var uow = getMockContextForAccessTests(Status.Public, Status.Public, Status.Public))
            {
                var set = uow.As;

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

            using (var uow = getMockContextForAccessTests(Status.Public, Status.Public, Status.Public))
            {
                var set = uow.As;

                var o1 = set.Get(id2);
            }
        }

        [TestMethod, ExpectedException(typeof(DalAccessRestrictionFailure))]
        public void TestAccessRestriction_PartialAccess_Update_Fail()
        {
            Guid id1, id2;
            EntityA a1, a2;
            PrepareAccessTestsData(out id1, out id2, out a1, out a2);

            using (var uow = getMockContextForAccessTests(Status.Public, Status.Public, Status.Public))
            {
                var set = uow.As;

                set.Update(a2);
            }
        }

        [TestMethod, ExpectedException(typeof(DalAccessRestrictionFailure))]
        public void TestAccessRestriction_PartialAccess_Delete_Fail()
        {
            Guid id1, id2;
            EntityA a1, a2;
            PrepareAccessTestsData(out id1, out id2, out a1, out a2);

            using (var uow = getMockContextForAccessTests(Status.Public, Status.Public, Status.Public))
            {
                var set = uow.As;

                set.Delete(id2);
            }
        }

        [TestMethod, ExpectedException(typeof(DalAccessRestrictionFailure))]
        public void TestAccessRestriction_PartialAccess_Add_Fail()
        {
            Guid id1, id2;
            EntityA a1, a2;
            PrepareAccessTestsData(out id1, out id2, out a1, out a2);

            using (var uow = getMockContextForAccessTests(Status.Public, Status.Public, Status.Public))
            {
                var set = uow.As;

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

            using (var uow = getMockContextForAccessTests(Status.Public, Status.Public, Status.Public))
            {
                var set = uow.As;

                set.AddIfNotExists(a2);
            }
        }

        [TestMethod, ExpectedException(typeof(DalAccessRestrictionFailure))]
        public void TestAccessRestriction_PartialAccess_AddIfNotExists_NotExists_Fail()
        {
            Guid id1, id2;
            EntityA a1, a2;
            PrepareAccessTestsData(out id1, out id2, out a1, out a2);

            using (var uow = getMockContextForAccessTests(Status.Public, Status.Public, Status.Private))
            {
                var set = uow.As;

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

            using (var uow = getMockContextForAccessTests(Status.Public, Status.Public, Status.Public))
            {
                var set = uow.As;

                set.AddOrUpdate(a2);
            }
        }

        [TestMethod, ExpectedException(typeof(DalAccessRestrictionFailure))]
        public void TestAccessRestriction_PartialAccess_AddOrUpdate_Add_Fail()
        {
            Guid id1, id2;
            EntityA a1, a2;
            PrepareAccessTestsData(out id1, out id2, out a1, out a2);

            using (var uow = getMockContextForAccessTests(Status.Public, Status.Public, Status.Public))
            {
                var set = uow.As;

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
            using (var context = getMockContext())
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

            using (var context = getMockContext())
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

            using (var context = getMockContext())
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

            using (var context = getMockContext())
            {
                context.EntitiesWithNavigation.Add(entity);
            }

            using (var context = getMockContext())
            {
                context.EntitiesWithNavigation.Update(entity);
            }
        }

        #endregion
    }
}
