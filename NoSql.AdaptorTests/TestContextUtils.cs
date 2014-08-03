using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.NoSql.Core;
using PubComp.NoSql.AdaptorTests.Mock;

namespace PubComp.NoSql.AdaptorTests
{
    [TestClass]
    public class TestContextUtils
    {
        [TestMethod]
        public void TestFindInheritingTypes()
        {
            var types = ContextUtils.FindInheritingTypes(this.GetType().Assembly, new[] { typeof(InheritanceBase1) });

            Assert.AreEqual(4, types.Count());

            Assert.IsTrue(types.Contains(typeof(TypeB1)));
            Assert.IsTrue(types.Contains(typeof(TypeC1.TypeD)));
            Assert.IsTrue(types.Contains(typeof(TypeC1.TypeD.TypeE)));
            Assert.IsTrue(types.Contains(typeof(TypeF1.TypeI.TypeK)));
        }
    }
}
