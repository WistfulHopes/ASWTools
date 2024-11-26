namespace BBScript.Language;

public enum BBSExpressionType
{
    INT,
    HEX,
    NEGATIVE,
    STRING,
    CONST,
    VAR,
    ENUM,
}

public interface BBSExpression : BBSAST
{
    BBSExpressionType Type { get; }
}