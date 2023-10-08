using CustomSqlServer.LanguageAnalyzer;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;

public class SqlLexer
{
    private readonly string _input;
    private int _position;
    private static readonly Regex IdentifierRegex = new Regex(@"[A-Za-z0-9_{][A-Za-z0-9_{]*", RegexOptions.Compiled);
    private List<SqlTokenKind> _tokens = new List<SqlTokenKind>();

    public SqlLexer(string input)
    {
        _input = input;
        _position = 0;
    }

    public SqlToken GetNextToken()
    {
        if (_position >= _input.Length)
        {
            return new SqlToken(SqlTokenKind.EndOfFile, string.Empty);
        }

        if (char.IsWhiteSpace(_input[_position]))
        {
            while (_position < _input.Length && char.IsWhiteSpace(_input[_position]))
            {
                _position++;
            }
        }
        //Select
        if (_input.Substring(_position).StartsWith("TAKE", StringComparison.OrdinalIgnoreCase))
        {
            var start = _position;
            _position += "TAKE".Length - 1;

            while (_position < _input.Length && char.IsWhiteSpace(_input[_position]))
            {
                _position++;
            }

            var tableNameStart = _position;
            while (_position < _input.Length && !char.IsWhiteSpace(_input[_position]) && _input[_position] != '(')
            {
                _position++;
            }

            var tableName = _input.Substring(tableNameStart, _position - tableNameStart);
            return new SqlToken(SqlTokenKind.Select, "TAKE");
        }

        if (_input.Substring(_position).StartsWith("ამოიღე", StringComparison.OrdinalIgnoreCase))
        {
            var start = _position;
            _position += "ამოიღე".Length - 1;

            while (_position < _input.Length && char.IsWhiteSpace(_input[_position]))
            {
                _position++;
            }

            var tableNameStart = _position;
            while (_position < _input.Length && !char.IsWhiteSpace(_input[_position]) && _input[_position] != '(')
            {
                _position++;
            }

            var tableName = _input.Substring(tableNameStart, _position - tableNameStart);
            return new SqlToken(SqlTokenKind.Select, "ამოიღე");
        }
        if (_input[_position] == '[')
        {
            var endIndex = _input.IndexOf(']');

            string result = _input.Substring(_position + 1, endIndex - _position - 1).Replace(" ", "");

            _position = endIndex + 1;

            return new SqlToken(SqlTokenKind.FilterColumns, result);
        }
        if (_input[_position] == '*')
        {
            _position++;

            return new SqlToken(SqlTokenKind.All, "*");
        }
        if (_input.Substring(_position).StartsWith("FROM", StringComparison.OrdinalIgnoreCase))
        {
            var start = _position;
            _position += "FROM".Length;

            while (_position < _input.Length && char.IsWhiteSpace(_input[_position]))
            {
                _position++;
            }

            var tableNameStart = _position;
            while (_position < _input.Length && !char.IsWhiteSpace(_input[_position]) && _input[_position] != '(')
            {
                _position++;
            }

            var tableName = _input.Substring(tableNameStart, _position - tableNameStart);
            _tokens.Add(SqlTokenKind.From);

            return new SqlToken(SqlTokenKind.From, tableName);
        }

        if (_input.Substring(_position).StartsWith("აქედან", StringComparison.OrdinalIgnoreCase))
        {
            var start = _position;
            _position += "აქედან".Length;

            while (_position < _input.Length && char.IsWhiteSpace(_input[_position]))
            {
                _position++;
            }

            var tableNameStart = _position;
            while (_position < _input.Length && !char.IsWhiteSpace(_input[_position]) && _input[_position] != '(')
            {
                _position++;
            }

            var tableName = _input.Substring(tableNameStart, _position - tableNameStart);
            _tokens.Add(SqlTokenKind.From);

            return new SqlToken(SqlTokenKind.From, tableName);
        }
        //Insert
        if (_input.Substring(_position).StartsWith("ADD", StringComparison.OrdinalIgnoreCase))
        {
            var start = _position;
            _position += "ADD".Length;

            while (_position < _input.Length && char.IsWhiteSpace(_input[_position]))
            {
                _position++;
            }

            var tableNameStart = _position;
            while (_position < _input.Length && !char.IsWhiteSpace(_input[_position]) && _input[_position] != '(')
            {
                _position++;
            }

            var tableName = _input.Substring(tableNameStart, _position - tableNameStart);
            return new SqlToken(SqlTokenKind.InsertIntoTable, tableName);
        }

        if (_input.Substring(_position).StartsWith("დაამატე", StringComparison.OrdinalIgnoreCase))
        {
            var start = _position;
            _position += "დაამატე".Length;

            while (_position < _input.Length && char.IsWhiteSpace(_input[_position]))
            {
                _position++;
            }

            var tableNameStart = _position;
            while (_position < _input.Length && !char.IsWhiteSpace(_input[_position]) && _input[_position] != '(')
            {
                _position++;
            }

            var tableName = _input.Substring(tableNameStart, _position - tableNameStart);
            return new SqlToken(SqlTokenKind.InsertIntoTable, tableName);
        }
        //Create Table
        if (_input.Substring(_position).StartsWith("CREATE TABLE", StringComparison.OrdinalIgnoreCase))
        {
            var start = _position;
            _position += "CREATE TABLE".Length;

            while (_position < _input.Length && char.IsWhiteSpace(_input[_position]))
            {
                _position++;
            }

            var tableNameStart = _position;
            while (_position < _input.Length && !char.IsWhiteSpace(_input[_position]) && _input[_position] != '(')
            {
                _position++;
            }

            var tableName = _input.Substring(tableNameStart, _position - tableNameStart);
            return new SqlToken(SqlTokenKind.TableName, tableName);
        }

        if (_input.Substring(_position).StartsWith("შექმენი ცხრილი", StringComparison.OrdinalIgnoreCase))
        {
            var start = _position;
            _position += "შექმენი ცხრილი".Length;

            while (_position < _input.Length && char.IsWhiteSpace(_input[_position]))
            {
                _position++;
            }

            var tableNameStart = _position;
            while (_position < _input.Length && !char.IsWhiteSpace(_input[_position]) && _input[_position] != '(')
            {
                _position++;
            }

            var tableName = _input.Substring(tableNameStart, _position - tableNameStart);
            return new SqlToken(SqlTokenKind.TableName, tableName);
        }

        //Alias
        if (_input.IndexOf("::") >= _position && _input.ToLower().StartsWith("take") && _input.IndexOf("::") != -1 && _tokens.Count == 1)
        {
            var start = _input.IndexOf("::") + "::".Length;

            while (_position < _input.Length)
            {
                _position++;
            }

            var expression = _input.Substring(start, _position - start).TrimStart();

            var rawAliasExpression = expression.Split(' ');
            //Always the first one of the parsed alias expression will be the alias table name
            var aliasTableName = rawAliasExpression[0];

            _position = _position - expression.Length + aliasTableName.Length;

            return new SqlToken(SqlTokenKind.Alias, aliasTableName);
        }

        if (_input.IndexOf("::") >= _position && _input.ToLower().StartsWith("ამოიღე") && _input.IndexOf("::") != -1 && _tokens.Count == 1)
        {
            var start = _input.IndexOf("::") + "::".Length;

            while (_position < _input.Length)
            {
                _position++;
            }

            var expression = _input.Substring(start, _position - start).TrimStart();

            var rawAliasExpression = expression.Split(' ');
            //Always the first one of the parsed alias expression will be the alias table name
            var aliasTableName = rawAliasExpression[0];

            _position = _position - expression.Length + aliasTableName.Length;

            return new SqlToken(SqlTokenKind.Alias, aliasTableName);
        }
        //Primary key
        if (_input[_position] == '{')
        {
            var endIndex = _input.IndexOf('}');

            string result = _input.Substring(_position + 1, endIndex - _position - 1).Replace(" ", "");

            _position = endIndex + 1;

            return new SqlToken(SqlTokenKind.PrimaryKey, result);
        }

        if (_input.IndexOf("where") != -1 && _position >= _input.IndexOf("where"))
        {
            var start = _input.IndexOf("where") + "where".Length;

            while (_position < _input.Length && char.IsWhiteSpace(_input[_position]))
            {
                _position++;
            }

            while (_position < _input.Length && !char.IsWhiteSpace(_input[_position]) && _input[_position] != '(')
            {
                _position++;
            }

            var expression = _input.Substring(start, _input.Length - start);
            _position = _input.Length + 4;

            return new SqlToken(SqlTokenKind.Where, expression);
        }

        if (_input.IndexOf("სადაც") != -1 && _position >= _input.IndexOf("სადაც"))
        {
            var start = _input.IndexOf("სადაც") + "სადაც".Length;

            while (_position < _input.Length && char.IsWhiteSpace(_input[_position]))
            {
                _position++;
            }

            while (_position < _input.Length && !char.IsWhiteSpace(_input[_position]) && _input[_position] != '(')
            {
                _position++;
            }

            var expression = _input.Substring(start, _input.Length - start);
            _position = _input.Length + 4;

            return new SqlToken(SqlTokenKind.Where, expression);
        }

        if (_input[_position] == '(')
        {
            _position++;
            return new SqlToken(SqlTokenKind.OpenParenthesis, "(");
        }

        if (_input[_position] == ')')
        {
            _position++;
            return new SqlToken(SqlTokenKind.CloseParenthesis, ")");
        }

        if (_input[_position] == ',')
        {
            _position++;
            return new SqlToken(SqlTokenKind.Comma, ",");
        }

        var match = IdentifierRegex.Match(_input.Substring(_position));
        if (match.Success)
        {
            var identifier = match.Value;
            _position += identifier.Length;

            //Needs to be added almost everywhere.
            //while (char.IsWhiteSpace(_input[_position]))
            //{
            //    _position++;
            //}
            while(_position <= _input.IndexOf("}") && _position >= _input.IndexOf("{"))
            {
                identifier += _input[_position];
                _position ++;
            }


            if (_position < _input.Length && _input[_position] == ':')
            {
                _position++;
                if (identifier.Equals("CREATE TABLE", StringComparison.OrdinalIgnoreCase))
                {
                    return new SqlToken(SqlTokenKind.CreateTable, identifier);
                }
                else if (identifier.Equals("INSERT INTO", StringComparison.OrdinalIgnoreCase))
                {
                    return new SqlToken(SqlTokenKind.InsertIntoTable, identifier);
                }

                return new SqlToken(SqlTokenKind.ColumnName, identifier);
            }

            return new SqlToken(SqlTokenKind.DataType, identifier);
        }

        return new SqlToken(SqlTokenKind.Unknown, _input[_position++].ToString());
    }
}