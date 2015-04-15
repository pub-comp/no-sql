using System;
using System.Collections.Generic;

namespace PubComp.NoSql.Core
{
    public class DalConcurrencyFailure : DalFailure
    {
        public DalConcurrencyFailure(
            String message = null, DalOperation operation = DalOperation.Undefined, Exception innerException = null)
            : base(message, operation, innerException)
        {
        }

        public DalConcurrencyFailure(
            String message, IEnumerable<IEntity> entities, DalOperation operation = DalOperation.Undefined, Exception innerException = null)
            : base(message, entities, operation, innerException)
        {
        }

        public DalConcurrencyFailure(
            String message, IEntity entity, DalOperation operation = DalOperation.Undefined, Exception innerException = null)
            : base(message, entity, operation, innerException)
        {
        }
    }
}
