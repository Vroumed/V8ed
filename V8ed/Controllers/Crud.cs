using System.Reflection;
using Vroumed.V8ed.Controllers.Attributes;
using Vroumed.V8ed.Dependencies;
using Vroumed.V8ed.Managers;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Vroumed.V8ed.Controllers;

public abstract class Crud
{
  [Resolved]
  private DatabaseManager DatabaseManager { get; }

  [ResolvedLoader]
  private void Load()
  {
    PropertyInfo[] properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    bool doInit = false;
    (PropertyInfo prop, CrudColumn pk)? initBy = null;

    List<(PropertyInfo, CrudColumn)> columns = new();
    foreach (PropertyInfo property in properties)
    {
      if (property.GetCustomAttributes().FirstOrDefault(c => c is CrudColumn) is CrudColumn column)
      {
        columns.Add((property, column));
        if (column.PrimaryKey && property.GetValue(this) != default)
        {
          doInit = true;
          initBy = (property, column);
        }
      }
    }

    if (!doInit)
      return;

    CrudTable table = GetType().GetCustomAttributes().FirstOrDefault(a => a is CrudTable) as CrudTable
        ?? throw new InvalidOperationException($"Type {GetType().Name} does not have the required attribute {nameof(CrudTable)}");

    string tableName = table.Name;

    Task.FromResult(async () =>
    {
      Dictionary<string, object>? data = await DatabaseManager.FetchOne($"SELECT * FROM {tableName} WHERE {initBy!.Value.pk.Name} = @prop",
        new Dictionary<string, object>()
        {
          ["prop"] = initBy!.Value.prop.GetValue(this)!
        });

      if (data == null)
        return;
      foreach ((PropertyInfo prop, CrudColumn col) in columns)
      {
        prop.SetValue(this, data[col.Name]);
      }
    });
  }

  private async Task Insert()
  {
    CrudTable table = GetType().GetCustomAttributes().FirstOrDefault(a => a is CrudTable) as CrudTable
        ?? throw new InvalidOperationException($"Type {GetType().Name} does not have the required attribute {nameof(CrudTable)}");

    string tableName = table.Name;
    List<(PropertyInfo prop, CrudColumn column)> columns = GetColumns();

    string columnNames = string.Join(", ", columns.Select(c => c.column.Name));
    string paramNames = string.Join(", ", columns.Select(c => "@" + c.column.Name));

    string query = $"INSERT INTO {tableName} ({columnNames}) VALUES ({paramNames})";

    Dictionary<string, object> parameters = columns.ToDictionary(c => c.column.Name, c => c.prop.GetValue(this)!);

    await DatabaseManager.Execute(query, parameters);
  }

  private async Task Update()
  {
    CrudTable table = GetType().GetCustomAttributes().FirstOrDefault(a => a is CrudTable) as CrudTable
        ?? throw new InvalidOperationException($"Type {GetType().Name} does not have the required attribute {nameof(CrudTable)}");

    string tableName = table.Name;
    List<(PropertyInfo prop, CrudColumn column)> columns = GetColumns();

    string setClauses = string.Join(", ", columns.Select(c => $"{c.column.Name} = @{c.column.Name}"));
    (PropertyInfo prop, CrudColumn column) primaryKey = columns.First(c => c.column.PrimaryKey);

    string query = $"UPDATE {tableName} SET {setClauses} WHERE {primaryKey.column.Name} = @{primaryKey.column.Name}";

    Dictionary<string, object> parameters = columns.ToDictionary(c => c.column.Name, c => c.prop.GetValue(this)!);

    await DatabaseManager.Execute(query, parameters);
  }

  private async Task Delete()
  {
    CrudTable table = GetType().GetCustomAttributes().FirstOrDefault(a => a is CrudTable) as CrudTable
        ?? throw new InvalidOperationException($"Type {GetType().Name} does not have the required attribute {nameof(CrudTable)}");

    string tableName = table.Name;
    List<(PropertyInfo prop, CrudColumn column)> columns = GetColumns();
    (PropertyInfo prop, CrudColumn column) primaryKey = columns.First(c => c.column.PrimaryKey);

    string query = $"DELETE FROM {tableName} WHERE {primaryKey.column.Name} = @{primaryKey.column.Name}";

    Dictionary<string, object> parameters = new()
    {
      [primaryKey.column.Name] = primaryKey.prop.GetValue(this)!
    };

    await DatabaseManager.Execute(query, parameters);
  }

  private List<(PropertyInfo prop, CrudColumn column)> GetColumns()
  {
    return GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(property => property.GetCustomAttributes().FirstOrDefault(c => c is CrudColumn) is CrudColumn)
                    .Select(property => (property, property.GetCustomAttributes().FirstOrDefault(c => c is CrudColumn) as CrudColumn))
                    .Cast<(PropertyInfo prop, CrudColumn column)>()
                    .ToList();
  }
}