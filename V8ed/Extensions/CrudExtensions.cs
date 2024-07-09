using System.Reflection;
using Vroumed.V8ed.Controllers.Attributes;

namespace Vroumed.V8ed.Extensions;

public static class CrudExtensions
{
  public static PropertyInfo GetPrimaryKey(this Type t)
  {
    return t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
             .FirstOrDefault(s => s
               .GetCustomAttributes()
               .Any(s => s is CrudColumn c && c.PrimaryKey))
           ?? throw new InvalidOperationException($"Type {t.Name} has no primary key");
  }

  public static string ToSqlOperator(this ComparisonType comparisonType)
  {
    return comparisonType switch
    {
      ComparisonType.EQUAL => "=",
      ComparisonType.NOT_EQUAL => "<>",
      ComparisonType.GREATER_THAN => ">",
      ComparisonType.LESS_THAN => "<",
      ComparisonType.GREATER_THAN_OR_EQUAL => ">=",
      ComparisonType.LESS_THAN_OR_EQUAL => "<=",
      _ => throw new ArgumentOutOfRangeException(nameof(comparisonType), comparisonType, null)
    };
  }
}
