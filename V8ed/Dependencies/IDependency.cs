namespace Vroumed.V8ed.Dependencies;

public interface IDependency
{
  public object GetObject();
  public Type Type { get; }
}