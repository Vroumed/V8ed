namespace Vroumed.V8ed.Controllers.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class CrudColumn : Attribute
{
  public string Name { get; }
  public bool PrimaryKey { get; }
  public bool IsAutoIncrement { get; }
  public string ForeignKey { get; }

  public CrudColumn(string name, bool primaryKey = false,bool isAutoIncrement = false, string foreignKey = null) 
  {
    Name = name;
    PrimaryKey = primaryKey;
    IsAutoIncrement = isAutoIncrement;
    ForeignKey = foreignKey;
  }
}
