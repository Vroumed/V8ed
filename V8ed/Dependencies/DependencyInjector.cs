using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Vroumed.V8ed.Dependencies.Attributes;
using Vroumed.V8ed.Dependencies.DependencyTypes;

namespace Vroumed.V8ed.Dependencies;

public sealed class DependencyInjector
{
  private List<IDependency> Dependencies { get; } = new();

  /// <summary>
  /// Cache a dependency topmost type as Singleton
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="dependency"></param>
  /// <exception cref="Exception"></exception>
  public void CacheSingleton<T>(T dependency)
  {
    if (dependency == null)
      throw new ArgumentNullException(nameof(dependency), "Cannot cache 'null'");

    if (Dependencies.Any(dep => dep.Type == dependency.GetType()))
      throw new Exception("Dependency already exists");

    Dependencies.Add(new SingletonDependency<T>()
    {
      Object = dependency,
      Type = dependency.GetType()
    });
  }

  /// <summary>
  /// Caches a dependency as a specific type as Singleton
  /// </summary>
  /// <typeparam name="T">Type to be registered in</typeparam>
  /// <param name="dependency">The dependency object to be cached</param>
  /// <exception cref="InvalidOperationException">When caching two same dependency types</exception>
  public void CacheSingletonAs<T>(T dependency)
  {
    if (dependency == null)
      throw new ArgumentNullException(nameof(dependency), "Cannot cache 'null'");

    if (Dependencies.Any(dep => dep.Type == typeof(T)))
      throw new InvalidOperationException("Dependency already exists");

    Dependencies.Add(new SingletonDependency<T>()
    {
      Object = dependency,
      Type = typeof(T)
    });
  }

  /// <summary>
  /// Caches a dependency as a specific type as Transient
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="transient"></param>
  public void CacheTransient<T>(Func<T> transient)
  {
    Dependencies.Add(new TransientDependency<T>()
    {
      Invoker = transient,
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

    Type? t = candidate.GetType();
    List<PropertyInfo> properties = new();

    while (t != null)
    {
      properties.AddRange(t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
      t = t.BaseType;
    }

    foreach (PropertyInfo property in properties)
      if (property.GetCustomAttributes(false).Any(s => s is Resolved))
        property.SetValue(candidate, Retrieve(property.PropertyType));

    t = candidate.GetType();
    List<MethodInfo> methods = new();

    while (t != null)
    {
      methods.AddRange(t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
      t = t.BaseType;
    }

    foreach (MethodInfo method in methods)
    {
      List<object> args = new();

      if (!method.GetCustomAttributes<ResolvedLoader>(false).Any())
        continue;

      ParameterInfo[] parameters = method.GetParameters();
      args.AddRange(parameters.Select(parameter => Retrieve(parameter.ParameterType)));

      method.Invoke(candidate, args.ToArray());
    }
  }

  private object Retrieve(Type type)
  {
    return Dependencies.FirstOrDefault(dep => dep.Type == type)?.GetObject() ?? throw new Exception("Dependency not found");
  }

  public T Retrieve<T>()
  {
    return (T) Dependencies.FirstOrDefault(dep => dep.Type == typeof(T))?.GetObject()! ?? throw new Exception("Dependency not found");
  }
}