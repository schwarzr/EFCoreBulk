using System;
using System.ComponentModel.DataAnnotations;
using NetTopologySuite.Geometries;

namespace Bulk.Test.Model
{
    public class SimpleTableWithSpatialProperty
    {
        public int Id { get; set; }

        [Required]
        public Point GeoLocation { get; set; }

        public Point BackupLocation { get; set; }
    }
}
