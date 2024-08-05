namespace Vroumed.V8ed.Dependencies.DependencyTypes;

public class SingletonDependency<T> : IDependency
{
  public object GetObject()
  {
    return Object ?? throw new InvalidOperationException($"Object of type {typeof(T).Name} (Singleton) is null therefore dependency is invalid.");
  }

  public required T Object { get; init; }

  public required Type Type { get; init; }
}