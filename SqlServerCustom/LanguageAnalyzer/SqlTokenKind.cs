using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomSqlServer.LanguageAnalyzer
{
    public enum SqlTokenKind
    {
        CreateTable,
        TableName,
        ColumnName,
        DataType,
        OpenParenthesis,
        CloseParenthesis,
        Comma,
        Unknown,
        EndOfFile,
        InsertIntoTable,
        From,
        Select,
        All,
        FilterColumns,
        Where,
        Alias,
        PrimaryKey
    }
}