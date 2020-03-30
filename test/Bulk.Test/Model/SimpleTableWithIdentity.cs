using System;
using System.Collections.Generic;
using System.Text;

namespace Bulk.Test.Model
{
    public class SimpleTableWithIdentity
    {
        public DateTime CreateTime { get; set; }

        public int Id { get; set; }

        public DateTime ModifyTime { get; set; }

        public string Title { get; set; }

        public string Whatever { get; set; }
    }
}