namespace PubComp.NoSql.ServiceStackRedis
{
    public class RedisConnectionInfo
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Password { get; set; }
        public int Db { get; set; }

        public RedisConnectionInfo()
        {
            this.Host = "localhost";
            this.Port = 6379;
            this.Password = null;
            this.Db = 0;
        }
    }
}
