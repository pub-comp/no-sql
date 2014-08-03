using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubComp.NoSql.AdaptorTests
{
    public static class ParametersForTests
    {
        public static ServiceStackRedis.RedisConnectionInfo RedisConnectionInfo
        {
            get
            {
                return new ServiceStackRedis.RedisConnectionInfo();
            }
        }

        public static MongoDbDriver.MongoDbConnectionInfo MongoDbConnectionInfo
        {
            get
            {
                return new MongoDbDriver.MongoDbConnectionInfo();
            }
        }
    }
}
