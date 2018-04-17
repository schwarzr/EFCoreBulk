using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Bulk.Test.Model
{
    public class SimpleTableWithShadowProperty : IShadowPropertyEntity
    {
        private readonly Dictionary<string, object> _shadowValues = new Dictionary<string, object>();

        public int Id { get; set; }

        [StringLength(100)]
        public string Title { get; set; }

        public object GetValue(string property)
        {
            if (_shadowValues.TryGetValue(property, out var value))
            {
                return value;
            }
            return null;
        }

        public void StoreValue(string property, object value)
        {
            _shadowValues[property] = value;
        }
    }
}