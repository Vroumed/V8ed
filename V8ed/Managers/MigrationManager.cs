using Microsoft.AspNetCore.Mvc.Formatters;
using System.Reflection;
using System.Text;
using Vroumed.V8ed.Controllers;
using Vroumed.V8ed.Controllers.Attributes;
using Vroumed.V8ed.Dependencies;
using System.Linq;
using Vroumed.V8ed.Extensions;

namespace Vroumed.V8ed.Managers;

public class MigrationManager : IDependencyCandidate
{
  [Resolved]
  private DatabaseManager DatabaseManager { get; set; }

  private List<string> CreateInstructions { get; } = new();

  private List<string> ConstraintsInstructions { get; } = new();

  public static Dictionary<Type, string> GetSqlTypeMappings => new()
    {
        { typeof(byte), "TINYINT" },
        { typeof(short), "SMALLINT" },
        { typeof(int), "INT" },
        { typeof(long), "BIGINT" },
        { typeof(float), "REAL" },
        { typeof(double), "FLOAT" },
        { typeof(decimal), "DECIMAL" },
        { typeof(bool), "BIT" },
        { typeof(char), "CHAR(1)" },
        { typeof(string), "NVARCHAR(MAX)" },
        { typeof(DateTime), "DATETIME" },
        { typeof(DateTimeOffset), "DATETIMEOFFSET" },
        { typeof(TimeSpan), "TIME" },
        { typeof(Guid), "UNIQUEIDENTIFIER" },
        { typeof(byte[]), "VARBINARY(MAX)" }
    };

  [ResolvedLoader]
  private void Load()
  {
    Assembly assembly = Assembly.GetExecutingAssembly();
    Type[] assemblyTypes = assembly.GetTypes().Where(t => !t.IsAbstract && t.IsAssignableTo(typeof(Crud))).ToArray();
    foreach (Type type in assemblyTypes)
    {
      CreateInstructions.Add(GenerateCreateInstructionsFor(type));
      ConstraintsInstructions.Add(GenerateConstraintsInstructionsFor(type));
    }

    Task.Run(async () =>
    {

      foreach (string instruction in CreateInstructions)
      {
        Console.WriteLine(instruction);
        await DatabaseManager.Execute(instruction);
      }

      foreach (string instruction in ConstraintsInstructions)
      {
        Console.WriteLine(instruction);
        await DatabaseManager.Execute(instruction);
      }
      
    }).GetAwaiter().GetResult();
  }

  private string GenerateCreateInstructionsFor(Type type)
  {
    CrudTable table = type.GetCustomAttributes().FirstOrDefault(a => a is CrudTable) as CrudTable
        ?? throw new InvalidOperationException($"Type {type.Name} does not have the required attribute {nameof(CrudTable)}");

    string tableName = table.Name;

    PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    List<CrudColumn> columns = new();

    StringBuilder sb = new();
    sb.AppendLine($"CREATE TABLE {tableName} (");

    foreach (PropertyInfo property in properties)
      if (property.GetCustomAttributes().FirstOrDefault(c => c is CrudColumn) is CrudColumn column)
      {
        string sqlType;
        columns.Add(column);
        if (property.PropertyType.IsAssignableTo(typeof(Crud)))
          sqlType = GetSqlTypeMappings.TryGetValue(property.PropertyType.GetPrimaryKey().PropertyType, out string? typeMapping)
            ? typeMapping
            : throw new InvalidOperationException($"No SQL type mapping found for {property.PropertyType}");
        else
          sqlType = GetSqlTypeMappings.TryGetValue(property.PropertyType, out string? typeMapping)
            ? typeMapping
            : throw new InvalidOperationException($"No SQL type mapping found for {property.PropertyType}");

        sb.Append($"  {column.Name} {sqlType}");
        if (column.PrimaryKey)
          sb.Append(" PRIMARY KEY");
        if (column.IsAutoIncrement)
          sb.Append(" IDENTITY(1,1)");
        if (!column.CanBeNull)
          sb.Append(" NOT NULL");
        if (column.Default != null)
          sb.Append($" DEFAULT {column.Default}");
        sb.AppendLine(",");
      }

    sb.Length--; // Remove the last comma
    sb.AppendLine(");");

    return sb.ToString();
  }

  private string GenerateConstraintsInstructionsFor(Type type)
  {
    CrudTable table = type.GetCustomAttributes().FirstOrDefault(a => a is CrudTable) as CrudTable
        ?? throw new InvalidOperationException($"Type {type.Name} does not have the required attribute {nameof(CrudTable)}");

    string tableName = table.Name;

    PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    List<(PropertyInfo prop, CrudColumn column)> columns = new();
    foreach (PropertyInfo property in properties)
      if (property.GetCustomAttributes().FirstOrDefault(c => c is CrudColumn) is CrudColumn column)
        columns.Add((property, column));

    StringBuilder sb = new();
    foreach ((PropertyInfo prop, CrudColumn column) in columns)
      if (typeof(Crud).IsAssignableTo(prop.PropertyType))
      {
        Type foreignType = prop.PropertyType;
        CrudTable foreignTable = foreignType.GetCustomAttributes().FirstOrDefault(a => a is CrudTable) as CrudTable
                                 ?? throw new InvalidOperationException($"Type {foreignType.Name} does not have the required attribute {nameof(CrudTable)}");

        string foreignTableName = foreignTable.Name;
        PropertyInfo foreignPrimaryKeyProp = foreignType.GetPrimaryKey();

        CrudColumn foreignPrimaryKeyColumn = foreignPrimaryKeyProp.GetCustomAttributes().FirstOrDefault(a => a is CrudColumn) as CrudColumn
                                             ?? throw new InvalidOperationException($"Primary key property {foreignPrimaryKeyProp.Name} of type {foreignType.Name} does not have the required attribute {nameof(CrudColumn)}");

        sb.AppendLine($"ALTER TABLE {tableName} ADD CONSTRAINT FK_{tableName}_{column.Name} FOREIGN KEY ({column.Name}) REFERENCES {foreignTableName}({foreignPrimaryKeyColumn.Name});");
      }

    return sb.ToString();
  }
}
