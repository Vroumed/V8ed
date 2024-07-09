using Microsoft.AspNetCore.Mvc.Formatters;
using System.Reflection;
using System.Text;
using Vroumed.V8ed.Controllers;
using Vroumed.V8ed.Controllers.Attributes;
using Vroumed.V8ed.Dependencies;
using System.Linq;

namespace Vroumed.V8ed.Managers;

public class MigrationManager : IDependencyCandidate
{

  [Resolved] private DatabaseManager DatabaseManager { get; }

  private List<string> CreateInstructions { get; } = [];

  private List<string> ConstraintsInstructions { get; } = [];

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

  [ResolvedLoader] private void Load()
  {
    Assembly assembly = Assembly.GetExecutingAssembly();
    Type[] assemblyTypes = assembly.GetTypes().Where(t => !t.IsAbstract && t.IsAssignableFrom(typeof(Crud))).ToArray();
    foreach (Type type in assemblyTypes)
    {
      CreateInstructions.Add(GenerateCreateInstructionsFor(type));
      ConstraintsInstructions.Add(GenerateConstraintsInstructionsFor(type));
    }

    Task.FromResult(async () =>
    {
      await DatabaseManager.OpenTransaction();
      try
      {
        foreach (string instruction in CreateInstructions)
        {
          await DatabaseManager.Execute(instruction);
        }

        foreach (string instruction in ConstraintsInstructions)
        {
          await DatabaseManager.Execute(instruction);
        }
      }
      catch (Exception) 
      { 
        await DatabaseManager.RollbackTransaction();
        throw;
      }
    });
  }

  private string GenerateCreateInstructionsFor(Type type)
  {
    CrudTable table = type.GetCustomAttributes().FirstOrDefault(a => a is CrudTable) as CrudTable 
      ?? throw new InvalidOperationException($"Type {type.Name} does not have the requeired attribute {nameof(CrudTable)}");

    string tableName = table.Name;

    PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    List<CrudColumn> columns = [];

    StringBuilder sb = new();
    sb.AppendLine($"CREATE TABLE IF NOT EXISTS {tableName} (");

    foreach (PropertyInfo property in properties)
    {
      if (property.GetCustomAttributes().FirstOrDefault(c => c is CrudColumn) is CrudColumn c)
        columns.Add(c);
      foreach ((CrudColumn column, string sqlType) in from column in columns
                                        let sqlType = GetSqlTypeMappings.TryGetValue(property.PropertyType, out string? typeMapping) ? typeMapping 
                                          : throw new InvalidOperationException($"No SQL type mapping found for {property.PropertyType}")
                                        select (column, sqlType))
      {
        sb.Append($"  {column.Name} {sqlType}");
        if (column.PrimaryKey)
          sb.Append(" PRIMARY KEY");
        if (column.IsAutoIncrement)
          sb.Append(" IDENTITY(1,1)");
        sb.AppendLine(",");
      }

      sb.Length--; // Remove the last comma
      sb.AppendLine(");");

    }

    return sb.ToString();
  }

  private string GenerateConstraintsInstructionsFor(Type type)
  {
    CrudTable table = type.GetCustomAttributes().FirstOrDefault(a => a is CrudTable) as CrudTable
        ?? throw new InvalidOperationException($"Type {type.Name} does not have the required attribute {nameof(CrudTable)}");

    string tableName = table.Name;

    PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    List<CrudColumn> columns = new();
    foreach (PropertyInfo property in properties)
    {
      if (property.GetCustomAttributes().FirstOrDefault(c => c is CrudColumn) is CrudColumn column)
      {
        columns.Add(column);
      }
    }

    StringBuilder sb = new();
    foreach (CrudColumn? column in from column in columns
                           where !string.IsNullOrEmpty(column.ForeignKey)
                           select column)
    {
      sb.AppendLine($"ALTER TABLE {tableName} ADD CONSTRAINT FK_{tableName}_{column.Name} FOREIGN KEY ({column.Name}) REFERENCES {column.ForeignKey};");
    }

    return sb.ToString();
  }
}
