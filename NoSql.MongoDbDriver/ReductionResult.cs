using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubComp.NoSql.MongoDbDriver
{
    public class ReductionResult<TId, TResult>
    {
        public TId _id { get; set; }
        public TResult value { get; set; }
    }
}
