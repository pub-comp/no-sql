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
                return new MongoDbDriver.MongoDbConnectionInfo
                {
                    Db = "Test-NoSQL"
                };
            }
        }
    }
}
