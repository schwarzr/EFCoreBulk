using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        public IEnumerable<IColumnSetup> Build()
        {
            return _columns;
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