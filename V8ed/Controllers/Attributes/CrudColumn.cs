namespace Vroumed.V8ed.Controllers.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class CrudColumn : Attribute
{
  public string Name { get; }
  public bool PrimaryKey { get; }
  public bool IsAutoIncrement { get; }
  public bool CanBeNull { get; }
  public object? Default { get; }

  public CrudColumn(string name, bool primaryKey = false, bool isAutoIncrement = false, bool canBeNull = true, object @default = null)
  {
    Name = name;
    PrimaryKey = primaryKey;
    if (primaryKey)
      canBeNull = false;
    IsAutoIncrement = isAutoIncrement;
    CanBeNull = canBeNull;
    Default = @default;
  }
}