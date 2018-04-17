namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk
{
    public interface IShadowPropertyAccessor
    {
        object GetValue(object entity, string property);

        void StoreValue(object entity, string property, object value);
    }
}