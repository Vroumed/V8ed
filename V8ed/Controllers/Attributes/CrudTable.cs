namespace Vroumed.V8ed.Controllers.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class CrudTable : Attribute
{
  public string Name { get; }
  public CrudTable(string name)
  {
    Name = name;
  }
}