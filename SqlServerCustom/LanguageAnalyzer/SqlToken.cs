using CustomSqlServer.LanguageAnalyzer;

public class SqlToken
{
    public SqlToken(SqlTokenKind kind, string text)
    {
        Kind = kind;
        Text = text;
    }

    public SqlTokenKind Kind { get; }
    public string Text { get; }
}