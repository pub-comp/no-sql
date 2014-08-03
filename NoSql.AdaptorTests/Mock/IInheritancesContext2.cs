using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PubComp.NoSql.Core;

namespace PubComp.NoSql.AdaptorTests.Mock
{
    public interface IInheritancesContext2 : IDomainContext
    {
        IEntitySet<Guid, InheritanceBase2> Entities { get; }
    }
}
