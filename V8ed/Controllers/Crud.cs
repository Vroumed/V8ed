using System.Reflection;
using Vroumed.V8ed.Controllers.Attributes;
using Vroumed.V8ed.Dependencies;
using Vroumed.V8ed.Managers;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Vroumed.V8ed.Extensions;
using System.Collections;

namespace Vroumed.V8ed.Controllers;

public abstract class Crud : IDependencyCandidate
{
  [Resolved]
  private DatabaseManager DatabaseManager { get; set;  }

  [Resolved]
  private DependencyInjector DependencyInjector { get; set;  }

  [ResolvedLoader]
  private void Load()
  {
    PropertyInfo[] properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    bool doInit = false;
    (PropertyInfo prop, CrudColumn pk)? initBy = null;

    List<(PropertyInfo, CrudColumn)> columns = new();
    foreach (PropertyInfo property in properties)
      if (property.GetCustomAttributes().FirstOrDefault(c => c is CrudColumn) is CrudColumn column)
      {
        columns.Add((property, column));
        if (column.PrimaryKey && property.GetValue(this) != default)
        {
          doInit = true;
          initBy = (property, column);
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
        if (prop.PropertyType.IsAssignableTo(typeof(Crud)))
        {
          Type type = prop.PropertyType;

          if (Activator.CreateInstance(type) is not Crud child)
            throw new InvalidOperationException(
              $"Type {type.Name} linked from {prop.Name} ({col.Name}) is not a Valid Foreign Key");

          PropertyInfo pk = type.GetPrimaryKey();

          pk.SetValue(child, data[col.Name]);

          DependencyInjector.Resolve(child);

          prop.SetValue(this, child);
        }
        else
          prop.SetValue(this, data[col.Name]);
    });
  }

  private async Task Insert()
  {
    CrudTable table = GetType().GetCustomAttributes().FirstOrDefault(a => a is CrudTable) as CrudTable
                      ?? throw new InvalidOperationException($"Type {GetType().Name} does not have the required attribute {nameof(CrudTable)}");

    string tableName = table.Name;
    List<(PropertyInfo prop, CrudColumn column)> columns = GetColumns();
    (PropertyInfo prop, CrudColumn column) autoIncrementColumn = columns.FirstOrDefault(c => c.column.IsAutoIncrement);

    if (autoIncrementColumn.prop != null && autoIncrementColumn.prop.GetValue(this) != null)
      throw new InvalidOperationException($"Column {autoIncrementColumn.column.Name} is auto-increment and its value must be null before insert.");

    string columnNames = string.Join(", ", columns.Where(c => !c.column.IsAutoIncrement).Select(c => c.column.Name));
    string paramNames = string.Join(", ", columns.Where(c => !c.column.IsAutoIncrement).Select(c => "@" + c.column.Name));

    string query = $"INSERT INTO {tableName} ({columnNames}) VALUES ({paramNames})";

    Dictionary<string, object?> parameters = columns.Where(c => !c.column.IsAutoIncrement).ToDictionary(c => c.column.Name, c =>
    {
      if (!c.prop.PropertyType.IsAssignableTo(typeof(Crud)))
        return c.prop.GetValue(this)!;

      if (c.prop.GetValue(this) is not Crud obj)
        return null;

      obj.Insert().Wait();
      return c.prop.PropertyType.GetPrimaryKey().GetValue(obj);
    });

    await DatabaseManager.Execute(query, parameters);

    // Retrieve the auto-incremented ID
    if (autoIncrementColumn.prop != null)
    {
      string lastInsertIdQuery = "SELECT LAST_INSERT_ID()";
      Dictionary<string, object>? lastInsertId = await DatabaseManager.FetchOne(lastInsertIdQuery, null);
      autoIncrementColumn.prop.SetValue(this, Convert.ChangeType(lastInsertId.Values.First(), autoIncrementColumn.prop.PropertyType));
    }

    await SaveEnumerables();
  }

  private async Task SaveEnumerables(bool deleteOnly = true) 
  {
    CrudTable table = GetType().GetCustomAttributes().FirstOrDefault(a => a is CrudTable) as CrudTable
                      ?? throw new InvalidOperationException($"Type {GetType().Name} does not have the required attribute {nameof(CrudTable)}");

    string tableName = table.Name;

    PropertyInfo[] properties = GetType()
      .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
      .Where(s => s
        .GetCustomAttributes<CrudEnumerableWhere>().Any() && s.PropertyType.GetInterfaces().Contains(typeof(IEnumerable))).ToArray();

    foreach (PropertyInfo property in properties)
    {
      IEnumerable<CrudEnumerableWhere> ens = property.GetCustomAttributes<CrudEnumerableWhere>();

      string delete = $"DELETE FROM {tableName} WHERE ";

      delete += string.Join(" AND ",
        ens.Select(e => $"{e.ColumnExtern} {e.ComparisonType.ToSqlOperator()} @{e.ColumnLocal}"));

      Dictionary<string, object?> parameters = 
        ens.ToDictionary(crudEnumerableWhere => 
          crudEnumerableWhere.ColumnLocal, 
          crudEnumerableWhere => GetColumns()
            .FirstOrDefault(s => s.column.Name == crudEnumerableWhere.ColumnLocal)
            .prop.GetValue(this));

      DatabaseManager.Execute(delete, parameters);

      if (deleteOnly) 
        continue;

      if (property.GetValue(this) is not IEnumerable enumerable)
        continue;
      foreach (object o in enumerable)
        if (o is Crud crudItem)
          await crudItem.Insert();
    }
  }

  private async Task Update()
  {
    CrudTable table = GetType().GetCustomAttributes().FirstOrDefault(a => a is CrudTable) as CrudTable
                      ?? throw new InvalidOperationException($"Type {GetType().Name} does not have the required attribute {nameof(CrudTable)}");

    string tableName = table.Name;
    List<(PropertyInfo prop, CrudColumn column)> columns = GetColumns();
    (PropertyInfo prop, CrudColumn pk)? pkc = columns.FirstOrDefault(c => c.column.PrimaryKey);
    if (pkc == null)
      throw new InvalidOperationException($"Type {GetType().Name} does not have a primary key column.");

    (PropertyInfo prop, CrudColumn pk) primaryKeyColumn = pkc.Value;

    // Building the SET part of the query
    string setClause = string.Join(", ", columns.Where(c => !c.column.PrimaryKey).Select(c => $"{c.column.Name} = @{c.column.Name}"));

    string query = $"UPDATE {tableName} SET {setClause} WHERE {primaryKeyColumn.pk.Name} = @{primaryKeyColumn.pk.Name}";

    Dictionary<string, object?> parameters = columns.ToDictionary(c => c.column.Name, c =>
    {
      if (!c.prop.PropertyType.IsAssignableTo(typeof(Crud))) 
        return c.prop.GetValue(this)!;

      if (c.prop.GetValue(this) is not Crud obj)
        return null;

      obj.Update();
      return c.prop.PropertyType.GetPrimaryKey().GetValue(obj);

    });

    await DatabaseManager.Execute(query, parameters);

    await SaveEnumerables();
  }

  private async Task Delete()
  {
    CrudTable table = GetType().GetCustomAttributes().FirstOrDefault(a => a is CrudTable) as CrudTable
                      ?? throw new InvalidOperationException($"Type {GetType().Name} does not have the required attribute {nameof(CrudTable)}");

    string tableName = table.Name;
    List<(PropertyInfo prop, CrudColumn column)> columns = GetColumns();
    (PropertyInfo prop, CrudColumn pk)? pkc = columns.FirstOrDefault(c => c.column.PrimaryKey);

    if (pkc == null)
      throw new InvalidOperationException($"Type {GetType().Name} does not have a primary key column.");

    (PropertyInfo prop, CrudColumn pk) primaryKeyColumn = pkc.Value;

    string query = $"DELETE FROM {tableName} WHERE {primaryKeyColumn.pk.Name} = @{primaryKeyColumn.pk.Name}";

    Dictionary<string, object?> parameters = new()
    {
      [primaryKeyColumn.pk.Name] = primaryKeyColumn.prop.GetValue(this)
    };

    await DatabaseManager.Execute(query, parameters);

    await SaveEnumerables(true);
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