using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PubComp.NoSql.Core;

namespace PubComp.NoSql.AdaptorTests.Mock
{
    public class Dates : IEntity<Guid>
    {
        public Guid Id { get; set; }
        
        [DateOnly]
        public DateTime Date1 { get; set; }
        
        public DateTime Date2 { get; set; }

        [DateOnly]
        public DateTime? Date3 { get; set; }

        public DateTime? Date4 { get; set; }
    }
}
