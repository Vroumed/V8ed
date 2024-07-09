namespace Vroumed.V8ed.Controllers.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class CrudEnumerableWhere : Attribute
{
  public string ColumnLocal { get; }
  public string ColumnExtern { get; }
  public ComparisonType ComparisonType { get; }

  public CrudEnumerableWhere(string columnLocal, ComparisonType comparisonType, string columnExtern)
  {
    ColumnLocal = columnLocal;
    ComparisonType = comparisonType;
    ColumnExtern = columnExtern;
  }
}

public enum ComparisonType
{
  EQUAL = 0,
  NOT_EQUAL = 1,
  GREATER_THAN = 2,
  LESS_THAN = 3,
  GREATER_THAN_OR_EQUAL = 4,
  LESS_THAN_OR_EQUAL = 5,
}
