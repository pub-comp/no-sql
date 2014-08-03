using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.NoSql.Core;
using PubComp.NoSql.ServiceStackRedis;
using PubComp.NoSql.AdaptorTests.Mock;
using PubComp.NoSql.AdaptorTests.MocksForAccessTest;

namespace PubComp.NoSql.AdaptorTests
{
    [TestClass]
    public class TestRedisContext : TestContextBase
    {
        public TestRedisContext() : base(
                () => new MockRedisContext(ParametersForTests.RedisConnectionInfo),
                (Status statusGetA, Status statusModA, Status statusDelA)
                    => new MockRedisForAccessTest(
                        ParametersForTests.RedisConnectionInfo,
                        statusGetA, statusModA, statusDelA),
                ctx => ((RedisContext)ctx).DeleteAll(),
                ctx => ((RedisContext)ctx).DeleteAll())
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
    }
}
