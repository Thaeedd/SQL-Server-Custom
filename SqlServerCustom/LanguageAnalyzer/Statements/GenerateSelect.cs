using CustomSqlServer.LanguageAnalyzer;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using SqlServerCustom.CommonMethods;
public static class GenerateSelect
{
    //The main method for generating Select statement with the given query.
    public static List<Table> Select(string query)
    {
        //Firstly, we are making language analysis to read the tokens from the raw string query.
        var lexer = new SqlLexer(query);

        var tokens = new List<SqlToken>();

        while (true)
        {
            //The tokens are added character by character with the specific rules written in GetNextToken()
            var token = lexer.GetNextToken();
            //We are putting every SqlTokenKind untill we have EndOfFile token.
            if (token.Kind == SqlTokenKind.EndOfFile)
            {
                break;
            }
            //Adding the tokens together.
            tokens.Add(token);
        }
        #region GetValuesAndInfoFromTokens
        //We are making this string, because we have an option to select only some columns from the table
        //So it is used to store filtered results as a json.
        var filteredResults = string.Empty;
        //We need to read tableName to make operatins on that.
        var tableName = string.Empty;
        //We are getting where expression from here.
        var whereExpression = string.Empty;
        //We are checking if the executor has '*' in it's query.
        bool isAll = false;
        //We are checking if the given query is aliased.
        bool isAliased = false;
        //Get the aliased value.
        string aliasValue = string.Empty;
        #endregion GetValuesAndInfoFromTokens

        #region ReadingTokens
        foreach (var token in tokens)
        {
            //We are getting the table name, when we have a from token, since our language is designed
            //Such way that From statement must be followed by TableName (FileName).
            if (token.Kind == SqlTokenKind.From)
            {
                tableName = token.Text;
            }
            //We are checking if we have where token we are parsing that token and have it's expression for further movements.
            else if (token.Kind == SqlTokenKind.Where)
            {
                whereExpression = token.Text;
                break;
            }
            //We are setting isAll boolean to true, if as I said there is '*' select.
            else if (token.Kind == SqlTokenKind.All)
            {
                isAll = true;
            }
            //We are getting aliased value and setting the isAliased boolean to true.
            else if (token.Kind == SqlTokenKind.Alias)
            {
                isAliased = true;
                aliasValue = token.Text;
            }
            //We also need to know what we are going to select, that's why as I said, we are collecting FilteColumns Tokens too.
            else if (token.Kind == SqlTokenKind.FilterColumns)
            {
                filteredResults = token.Text;
            }
        }
        #endregion ReadingTokens

        #region ParsingAliasedValues
        //Always the first one of the parsed alias expression will be the alias table name
        var aliasTableName = aliasValue;
        #endregion ParsingAliasedValues

        #region GroupTablesAndAliases
        //If the alias exists in the expression we are going to group it with the corresponding table name.
        //And store them in dictionary, this way, we avoid the matching alias names in the current execution.
        var tableAliases = new Dictionary<string, string>();
        if (isAliased)
        {
            //We are adding the alias name to the given table.
            tableAliases.Add(aliasTableName, tableName);
        }
        #endregion GroupTablesAndAliases

        #region ReadingWhereExpression
        //The expressions can be simplified
        //Based on the whereExpression we are reading the expression symbol, in our case it can only be '<' '>' '='
        //Then we are parsing and adding it to the exp collection which is type of ExpressionNode -> this itself is a struct
        //That defines the where clause structure, ColumnName-Expression-Value.
        var exp = new List<ExpressionNode>();
        if (whereExpression.Contains(">"))
        {
            var expressionNode = new ExpressionNode();

            if (isAliased && aliasTableName == whereExpression.TrimStart().Substring(0, aliasTableName.Length))
                expressionNode.ColumnName = whereExpression.Substring(aliasTableName.Length + 2, whereExpression.IndexOf(">") - aliasTableName.Length - 3).Trim();
            else
                expressionNode.ColumnName = whereExpression.Substring(0, whereExpression.IndexOf(">")).Trim();

            expressionNode.Expression = whereExpression.Substring(whereExpression.IndexOf(">"), 1).Trim();
            expressionNode.Value = whereExpression.Substring(whereExpression.IndexOf(">") + 1, whereExpression.Length - whereExpression.IndexOf(">") - 1).Trim();

            exp.Add(expressionNode);
        }
        else if (whereExpression.Contains("<"))
        {
            var expressionNode = new ExpressionNode();
            if (isAliased && aliasTableName == whereExpression.TrimStart().Substring(0, aliasTableName.Length))
                expressionNode.ColumnName = whereExpression.Substring(aliasTableName.Length + 2, whereExpression.IndexOf("<") - aliasTableName.Length - 3).Trim();
            else
                expressionNode.ColumnName = whereExpression.Substring(0, whereExpression.IndexOf("<")).Trim();

            expressionNode.Expression = whereExpression.Substring(whereExpression.IndexOf("<"), 1).Trim();
            expressionNode.Value = whereExpression.Substring(whereExpression.IndexOf("<") + 1, whereExpression.Length - whereExpression.IndexOf("<") - 1).Trim();

            exp.Add(expressionNode);
        }
        else if (whereExpression.Contains("="))
        {
            var expressionNode = new ExpressionNode();
            if (isAliased && aliasTableName == whereExpression.TrimStart().Substring(0, aliasTableName.Length))
                expressionNode.ColumnName = whereExpression.Substring(aliasTableName.Length + 2, whereExpression.IndexOf("=") - aliasTableName.Length - 3).Trim();
            else
                expressionNode.ColumnName = whereExpression.Substring(0, whereExpression.IndexOf("=")).Trim();

            expressionNode.Expression = whereExpression.Substring(whereExpression.IndexOf("="), 1).Trim();
            expressionNode.Value = whereExpression.Substring(whereExpression.IndexOf("=") + 1, whereExpression.Length - whereExpression.IndexOf("=") - 1).Trim();

            exp.Add(expressionNode);
        }
        #endregion ReadingWhereExpression

        #region CheckingWhereColumnNameDataTypes

        var columnExists = false;
        var dataType = string.Empty;

        //Filling the generic collections with actual values from the given table.
        (var genericCols, var genericTypes) = TableStructure.GetTableStructureColumns(tableName);
        //We are checking that there is at least one expression, it means that there exists where clause.
        //If where clause exists(exp.Count > 0, meaning that there is at least 1 expression after where) we are checking either
        //The expression is '>' or '<'(Notice, that we are not checking if expression is '=', because = can be applied to strings too
        //and the given '>' or '<' only applicable to numeric values).
        if (exp.Count > 0 && (exp[0].Expression == ">" || exp[0].Expression == "<"))
        {
            for (int i = 0; i < genericCols.Count; i++)
            {
                //Here we are checking that the column name in the expression is actually valid and exists in general table
                //structure.
                if (exp[0].ColumnName == genericCols[i])
                {
                    //Now we are checking if the given ColumnName is type of INT, because if the '>' or '<' is applied to
                    //Non-INT type column, it's incorrect and we want to return unsupported query.
                    if (genericTypes[i] != "INT")
                    {
                        throw new Exception();
                    }
                    //Either way, everything goes right and we have a datatype, which we know, in our case will be INT 
                    //because we are only working with 2 types(VARCHAR and INT)
                    columnExists = true;
                    dataType = genericTypes[i];
                }
            }
            //If the column does not exist we also want to return bad query
            if (!columnExists)
            {
                throw new Exception();
            }
            if (columnExists)
            {
                //If column is correct and type of INT
                if (dataType == "INT")
                {
                    try
                    {
                        //We are still checking if the given value can be converted to INT value.
                        Convert.ToInt32(exp[0].Value);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }
        }
        #endregion CheckingWhereColumnNameDataTypes

        //We are storing raw jsons that are selected from the table, to deserialize.
        var records = new List<string>();

        using (var reader = new StreamReader($@"C:\Tables\{tableName}", true))
        {
            //We are making one ReadLine() because, the first line in our File(Table) is the 
            //Table 'Header' which defines the structure of the table, and it must ensure the 
            //Validations of the statements.
            reader.ReadLine();
            while (!reader.EndOfStream)
            {
                //Reading after the first line is skipped.
                records.Add(reader.ReadLine());
            }
        }
        //Now, we are making the Tables list from that and trying to deserialize the jsons that are 
        //Going to be converted to objects.
        var tables = new List<Table>();
        foreach (var record in records)
        {
            //We are coping from the result table, so we are getting the whole select state
            Table resultTable = new Table();
            Table originalTable = JsonConvert.DeserializeObject<Table>(record);
            resultTable.Records = new List<TableRecord>();
            //We are getting the collection of strings of the column names user wants to query.
            var filteredColumns = filteredResults.Trim().Split(',').ToList();

            //If there is '*' symbol, we are selecting all the columns that are in generic table header.
            if (isAll)
            {
                filteredColumns = genericCols;
            }

            //If there is aliased
            if (isAliased)
            {
                for (int i = 0; i < filteredColumns.Count; i++)
                {
                    //We are checking that if the alias value truly exists on the column, in this case remove that.
                    //Notice that the table can be aliased, but the alias not used in select.
                    if (filteredColumns[i].Contains("."))
                    {
                        filteredColumns[i] = filteredColumns[i].Substring(aliasValue.Length + 1, filteredColumns[i].Length - aliasValue.Length - 1);
                    }
                }
            }

            bool add = false;
            foreach (var node in originalTable.Records)
            {
                if (exp.Count > 0 && node.ColumnName == exp[0].ColumnName)
                {
                    if (exp.Count != 0)
                    {
                        if (exp[0].Expression == ">")
                        {
                            if (node.DataType != "NULL" && Convert.ToInt32(node.DataType) > Convert.ToInt32(exp[0].Value))
                            {
                                add = true;
                            }
                        }
                        else if (exp[0].Expression == "<")
                        {
                            if (node.DataType != "NULL" && Convert.ToInt32(node.DataType) < Convert.ToInt32(exp[0].Value))
                            {
                                add = true;
                            }
                        }
                        else if (exp[0].Expression == "=")
                        {
                            if (node.DataType != "NULL" && node.DataType == exp[0].Value)
                            {
                                add = true;
                            }
                        }
                    }
                }
                else if (exp.Count == 0)
                {
                    add = true;
                }


            }
            foreach (var node in originalTable.Records)
            {
                if (add && filteredColumns.Contains(node.ColumnName))
                {
                    resultTable.Records.Add(node);
                }
            }
            if (add)
            {
                tables.Add(resultTable);
            }
        }

        return tables;
    }
}