using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PubComp.NoSql.Core;
using PubComp.NoSql.ServiceStackRedis;

namespace PubComp.NoSql.AdaptorTests.MocksForAccessTest
{
    public class MockRedisForAccessTest : RedisContext, IMockContextForAccessTests
    {
        private Status _statusGetA, _statusModA, _statusDelA;

        public MockRedisForAccessTest(RedisConnectionInfo connectionInfo,
            Status statusGetA, Status statusModA, Status statusDelA)
            : base(connectionInfo)
        {
            _statusGetA = statusGetA;
            _statusModA = statusModA;
            _statusDelA = statusDelA;

            this.As.OnModifying += As_OnModifying;
            this.As.OnDeleting += As_OnDeleting;
            this.As.OnGetting += As_OnGetting;
        }

        private void As_OnModifying(object sender, AccessEventArgs<EntityA> e)
        {
            if (e.Entity == null || e.Entity.Status < _statusModA)
                e.CanAccess = false;
        }

        private void As_OnDeleting(object sender, AccessEventArgs<EntityA> e)
        {
            if (e.Entity == null || e.Entity.Status < _statusDelA)
                e.CanAccess = false;
        }

        private void As_OnGetting(object sender, AccessEventArgs<EntityA> e)
        {
            if (e.Entity == null || e.Entity.Status < _statusGetA)
                e.CanAccess = false;
        }

        public IEntitySet<Guid, EntityA> As { get; private set; }
    }
}
