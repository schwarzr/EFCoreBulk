using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.EntityFrameworkCore.Update;

namespace Microsoft.EntityFrameworkCore.SqlServer.Bulk.Internal
{
    public class ModificationCommandSetupProvider : IColumnSetupProvider
    {
        private readonly ImmutableList<IColumnSetup> _columns;

        public ModificationCommandSetupProvider(IEnumerable<ModificationCommand> commands)
        {
            var columns = new ConcurrentDictionary<string, IColumnSetup>();

            foreach (var command in commands)
            {
                if (TableName == null)
                {
                    TableName = command.TableName;
                    SchemaName = command.Schema;
                }

                foreach (var modification in command.ColumnModifications)
                {
                    var name = modification.ColumnName;
                    var direction = ValueDirection.None;
                    if (modification.IsWrite)
                    {
                        direction = direction | ValueDirection.Write;
                    }
                    if (modification.IsRead)
                    {
                        direction = direction | ValueDirection.Read;
                    }

                    columns.GetOrAdd(name, p => new DelegateColumnSetup(columns.Count, p, modification.Property.ClrType, x => GetColumnValue(x, name), (x, y) => { }, direction));
                }
            }

            _columns = columns.Values.ToImmutableList();
        }

        public string SchemaName { get; }

        public string TableName { get; }

        public IEnumerable<IColumnSetup> Build()
        {
            return _columns;
        }

        public void PropagateValues(object entity, IDictionary<IColumnSetup, object> values)
        {
            ((ModificationCommand)entity).PropagateResults(new Storage.ValueBuffer(values.OrderBy(p => p.Key.Ordinal).Select(p => p.Value).ToArray()));
        }

        private static object GetColumnValue(object parma, string name)
        {
            var command = parma as ModificationCommand;
            if (command != null)
            {
                foreach (var item in command.ColumnModifications)
                {
                    if (item.ColumnName == name)
                    {
                        return item.Value;
                    }
                }
            }

            return null;
        }
    }
}