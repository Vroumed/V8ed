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
}
