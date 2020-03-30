using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Bulk.Test.Model
{
    public abstract class BaseTphTable
    {
        public int Id { get; set; }

        [StringLength(50)]
        public string Name { get; set; }
    }
}