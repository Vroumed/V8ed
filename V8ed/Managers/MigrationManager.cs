using Microsoft.AspNetCore.Mvc.Formatters;
using System.Linq;
using System.Reflection;
using System.Text;
using Vroumed.V8ed.Controllers;
using Vroumed.V8ed.Controllers.Attributes;
using Vroumed.V8ed.Dependencies;
using Vroumed.V8ed.Dependencies.Attributes;
using Vroumed.V8ed.Extensions;
using Vroumed.V8ed.Utils.Logger;

namespace Vroumed.V8ed.Managers;

public class MigrationManager : IDependencyCandidate
{
  [Resolved]
  private DatabaseManager DatabaseManager { get; set; } = null!;

  [Resolved]
  private Logger Logger { get; set; } = null!;

  private List<string> CreateInstructions { get; } = new();

  private List<string> ConstraintsInstructions { get; } = new();

  public static Dictionary<Type, string> GetSqlTypeMappings => new()
  {
    { typeof(byte), "TINYINT" },
    { typeof(short), "SMALLINT" },
    { typeof(int), "INT" },
    { typeof(long), "BIGINT" },
    { typeof(float), "FLOAT" },
    { typeof(double), "DOUBLE" },
    { typeof(decimal), "DECIMAL(18, 2)" },
    { typeof(bool), "TINYINT(1)" },
    { typeof(char), "CHAR(1)" },
    { typeof(string), "VARCHAR(255)" },
    { typeof(DateTime), "DATETIME" },
    { typeof(DateTimeOffset), "DATETIME" },
    { typeof(TimeSpan), "TIME" },
    { typeof(Guid), "CHAR(36)" },
    { typeof(byte[]), "BLOB" }
  };

  [ResolvedLoader]
  private void Load()
  {
    Assembly assembly = Assembly.GetExecutingAssembly();
    Type[] assemblyTypes = assembly.GetTypes().Where(t => !t.IsAbstract && t.IsAssignableTo(typeof(Crud))).ToArray();
    foreach (Type type in assemblyTypes)
    {
      CreateInstructions.Add(GenerateCreateInstructionsFor(type));
      string alterInstruction = GenerateConstraintsInstructionsFor(type);
      if (!string.IsNullOrWhiteSpace(alterInstruction))
        ConstraintsInstructions.Add(alterInstruction);
    }

    Task.Run(async () =>
    {

      foreach (string instruction in CreateInstructions)
      {
        Logger.Log(LogFile.Debug, instruction);
        await DatabaseManager.Execute(instruction);
      }

      foreach (string instruction in ConstraintsInstructions)
      {
        Logger.Log(LogFile.Debug, instruction);
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

    List<string> strings = new();

    foreach (PropertyInfo property in properties)
      if (property.GetCustomAttributes().FirstOrDefault(c => c is CrudColumn) is CrudColumn column)
      {
        string line = string.Empty;
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

        line += $"  {column.Name} {sqlType}";
        if (column.PrimaryKey)
          line += " PRIMARY KEY";
        if (column.IsAutoIncrement)
          line += " AUTO_INCREMENT";
        if (!column.CanBeNull)
          line += " NOT NULL";
        if (column.Default != null)
          line += $" DEFAULT {column.Default}";

        strings.Add(line);
      }

    sb.AppendLine(string.Join(", \n", strings));

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
      if (prop.PropertyType.IsAssignableTo(typeof(Crud)))
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