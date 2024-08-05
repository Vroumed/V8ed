using System.Reflection;
using Vroumed.V8ed.Controllers;
using Vroumed.V8ed.Controllers.Attributes;
using Vroumed.V8ed.Dependencies;

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

  public static List<(PropertyInfo prop, CrudColumn column)> GetColumns(this object o)
  {
    return GetColumns(o.GetType());
  }

  public static void MassResolve<T>(this IEnumerable<T> enumerable, DependencyInjector injector) where T : IDependencyCandidate
  {
    foreach (T dependencyCandidate in enumerable)
      injector.Resolve(dependencyCandidate);
  }

  public static List<(PropertyInfo prop, CrudColumn column)> GetColumns(this Type t)
  {
    return t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
    .Where(property => property.GetCustomAttributes().FirstOrDefault(c => c is CrudColumn) is CrudColumn)
    .Select(property => (property, property.GetCustomAttributes().FirstOrDefault(c => c is CrudColumn) as CrudColumn))
    .Cast<(PropertyInfo prop, CrudColumn column)>()
    .ToList();
  }

  public static string GetTableName(this object o)
  {
    return GetTableName(o.GetType());
  }

  public static string GetTableName(this Type t)
  {
    //get CrudTable attribute from type class
    return ((CrudTable) (t.GetCustomAttributes(true)
    .FirstOrDefault(s => s is CrudTable) 
                         ?? throw new InvalidOperationException($"type {t.Name} has no {nameof(CrudTable)} attribute")))
    .Name;
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