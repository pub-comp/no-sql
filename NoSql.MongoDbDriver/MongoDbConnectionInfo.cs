using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace PubComp.NoSql.MongoDbDriver
{
    public class MongoDbConnectionInfo
    {
        private const string Prefix = @"mongodb://";
        private const string DefaultHost = @"localhost";
        private const int DefaultPort = 27017;
        private const string DefaultDb = @"admin";

        public IList<Instance> Instances { get; set; }

        public string Host
        {
            get
            {
                if (Instances == null || !Instances.Any())
                    return DefaultHost;

                return Instances[0].Host;
            }
            set
            {
                var instances = Instances;

                if (instances == null)
                    instances = new List<Instance>();

                var instance = instances.FirstOrDefault() ?? new Instance { Port = DefaultPort };
                instance.Host = value;
                if (!instances.Any())
                    instances.Add(instance);

                Instances = instances;
            }
        }

        public int Port
        {
            get
            {
                if (Instances == null || !Instances.Any())
                    return DefaultPort;

                return Instances[0].Port;
            }
            set
            {
                var instances = Instances;

                if (instances == null)
                    instances = new List<Instance>();

                var instance = instances.FirstOrDefault() ?? new Instance { Host = DefaultHost };
                instance.Port = value;
                if (!instances.Any())
                    instances.Add(instance);

                Instances = instances;
            }
        }

        public string Username { get; set; }

        public string Password { get; set; }

        public string LoginDb { get; set; }

        public string Db { get; set; }

        public IList<KeyValuePair<string, string>> Options { get; set; }

        public string ConnectionString
        {
            get
            {
                string userPass = string.Empty;
                if (!string.IsNullOrEmpty(Username))
                {
                    userPass = WebUtility.UrlEncode(Username);
                    if (!string.IsNullOrEmpty(Password))
                        userPass += ':' + WebUtility.UrlEncode(Password);

                    userPass += '@';
                }

                string dbParams = string.Empty;
                if (!string.IsNullOrEmpty(LoginDb) || Options.Any())
                {
                    dbParams = '/' + (LoginDb ?? string.Empty);
                    if (Options != null && Options.Any())
                    {
                        dbParams += '?';
                        dbParams += string.Join(";", Options.Select(opt => opt.Key + '=' + opt.Value));
                    }
                }

                var instances = string.Join(",", this.Instances);

                var result = string.Concat(Prefix, userPass, instances, dbParams);
                return result;
            }
        }

        public MongoDbConnectionInfo()
        {
            this.Instances = new List<Instance> { new Instance { Host = DefaultHost, Port = DefaultPort } };
            this.Username = null;
            this.Password = null;
            this.LoginDb = DefaultDb;
            this.Db = DefaultDb;
            this.Options = new List<KeyValuePair<string, string>>();
        }

        public class Instance
        {
            public string Host { get; set; }
            public int Port { get; set; }

            public Instance()
            {
                this.Host = DefaultHost;
                this.Port = DefaultPort;
            }

            public Instance(string host, int port)
            {
                this.Host = host;
                this.Port = port;
            }

            public override string ToString()
            {
                return string.Concat(this.Host, ':', this.Port);
            }
        }
    }
}
