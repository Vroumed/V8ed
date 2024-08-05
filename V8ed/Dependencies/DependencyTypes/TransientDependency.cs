namespace Vroumed.V8ed.Dependencies.DependencyTypes;

public class TransientDependency<T> : IDependency
{
  public object GetObject()
  {
    return Invoker.Invoke() ?? throw new InvalidOperationException($"Invoker of type {typeof(T).Name} (Transient) returned null therefore dependency is invalid.");
  }

  public required Func<T> Invoker { get; init; }

  public required Type Type { get; init; }
}
