using PubComp.NoSql.Core;

namespace PubComp.NoSql.MongoDbDriver
{
    public class ReductionResult<TId, TResult> : IEntity<TId>
    {
        public TId Id { get; set; }
        public TResult value { get; set; }
    }
}
