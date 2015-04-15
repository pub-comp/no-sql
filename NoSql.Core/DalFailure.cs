using System;
using System.Collections.Generic;

namespace PubComp.NoSql.Core
{
    public class DalFailure : Failure
    {
        public IEnumerable<IEntity> Entities
        {
            private set;
            get;
        }

        public DalOperation Operation
        {
            private set;
            get;
        }

        public DalFailure(
            String message = null, DalOperation operation = DalOperation.Undefined, Exception innerException = null)
            : base(message, innerException)
        {
            this.Entities = null;
            this.Operation = operation;
        }

        public DalFailure(
            String message, IEnumerable<IEntity> entities, DalOperation operation = DalOperation.Undefined, Exception innerException = null)
            : base(message, innerException)
        {
            this.Entities = entities;
            this.Operation = operation;
        }

        public DalFailure(
            String message, IEntity entity, DalOperation operation = DalOperation.Undefined, Exception innerException = null)
            : base(message, innerException)
        {
            this.Entities = entity != null ? new [] { entity } : null;
            this.Operation = operation;
        }
    }

    public enum DalOperation
    {
        Undefined,
        Get,
        Add,
        Update,
        Delete
    }
}
