using System;
using System.Collections;
using System.Reflection;

namespace Vroumed.V8ed.Dependencies;

[AttributeUsage(AttributeTargets.Property)]
public class Resolved : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public class ResolvedLoader : Attribute
{
}

public interface IDependencyCandidate
{
}

public struct Dependency
{
  public object DependencyObject { get; set; }
  public Type Type { get; set; }
}

public sealed class DependencyInjector
{
  private List<Dependency> Dependencies { get; } = new();

  /// <summary>
  /// Cache a dependency topmost type
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="dependency"></param>
  /// <exception cref="Exception"></exception>
  public void Cache<T>(T dependency)
  {
    foreach (Dependency dep in Dependencies)
    {
      if (dep.Type == dependency.GetType())
      {
        throw new Exception("Dependency already exists");
      }
    }

    Dependencies.Add(new Dependency
    {
      DependencyObject = dependency,
      Type = dependency.GetType()
    });
  }

  /// <summary>
  /// Caches a dependency as a specific type
  /// </summary>
  /// <typeparam name="T">Type to be registered in</typeparam>
  /// <param name="dependency">The dependency object to be cached</param>
  /// <exception cref="InvalidOperationException">When caching two same dependency types</exception>
  public void CacheAs<T>(T dependency)
  {
    foreach (Dependency dep in Dependencies)
    {
      if (dep.Type == typeof(T))
      {
        throw new InvalidOperationException("Dependency already exists");
      }
    }

    Dependencies.Add(new Dependency
    {
      DependencyObject = dependency,
      Type = typeof(T)
    });
  }

  /// <summary>
  /// Resolve a <see cref="IDependencyCandidate"/>'s <see cref="Resolved"/> attributes
  /// </summary>
  /// <param name="candidate"></param>
  public void Resolve(IDependencyCandidate candidate)
  {
    //Get all the properties with the Resolved attribute
    FieldInfo[] properties = candidate.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
    foreach (FieldInfo property in properties)
    {
      if (property.GetCustomAttributes(false).Any(s => s is Resolved))
      {
        property.SetValue(candidate, Retrieve(property.FieldType));
      }
    }

    MethodInfo[] methods = candidate.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    foreach (MethodInfo method in methods)
    {
      if (method.GetCustomAttributes(false).Any(s => s is ResolvedLoader))
      {
        ParameterInfo[] parameters = method.GetParameters();
        List<object> args = [];
        foreach (ParameterInfo parameter in parameters)
        {
          args.Add(Retrieve(parameter.ParameterType));
        }

        method.Invoke(candidate, args.ToArray());
      }
    }
  }

  private object Retrieve(Type type)
  {
    foreach (Dependency dep in Dependencies)
    {
      if (dep.Type == type)
      {
        return dep;
      }
    }

    throw new Exception("Dependency not found");
  }

  public T Retrieve<T>()
  {
    foreach (Dependency dep in Dependencies)
    {
      if (dep.Type == typeof(T))
      {
        return (T) dep.DependencyObject;
      }
    }

    throw new Exception($"dependency of type {typeof(T)} not found");
  }
}