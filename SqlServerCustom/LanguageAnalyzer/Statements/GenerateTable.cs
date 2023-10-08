using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace CustomSqlServer.LanguageAnalyzer.Statements
{
    public static class GenerateTable
    {
        public static void GenerateTables(string query)
        {
            var lexer = new SqlLexer(query);

            var tokens = new List<SqlToken>();
            var primaryKeyColumn = string.Empty;
            while (true)
            {
                var token = lexer.GetNextToken();

                if (token.Kind == SqlTokenKind.EndOfFile)
                {
                    break;
                }

                if (token.Kind == SqlTokenKind.Alias)
                {
                    continue;
                }

                if(token.Kind == SqlTokenKind.PrimaryKey)
                {
                    primaryKeyColumn = token.Text;
                }

                tokens.Add(token);
            }

            var tableRecord = new Table
            {
                Records = new List<TableRecord>()
            };

            for (int i = 0; i < tokens.Count - 1; i++)
            {
                if (tokens[i].Kind == SqlTokenKind.TableName)
                {
                    tableRecord.TableName = tokens[i].Text;
                }

                if (tokens[i].Kind == SqlTokenKind.ColumnName)
                {
                    var record = new TableRecord();
                    record.ColumnName = tokens[i].Text;
                    record.DataType = tokens[i + 1].Text;
                    tableRecord.Records.Add(record);

                    if (record.ColumnName.Contains("{"))
                    {
                        primaryKeyColumn = record.ColumnName.Trim().Split('{')[0].Trim();
                        record.ColumnName = primaryKeyColumn;
                    }
                }
            }

            Console.WriteLine(primaryKeyColumn);

            var filePath = $@"C:\Tables\{tableRecord.TableName}";
            var filePathForHashedValues = $@"C:\Tables\{tableRecord.TableName}Values_PK";

            var json = JsonConvert.SerializeObject(tableRecord);

            try
            {
                if (TableExists(filePath))
                {
                    throw new Exception("The table already exists");
                }

                using (var writer = new StreamWriter(filePath, true))
                {
                    writer.WriteLine(json);
                }

                using(var writer = new StreamWriter(filePathForHashedValues, true))
                {
                    writer.WriteLine(primaryKeyColumn);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static bool TableExists(string tableName)
        {
            if (File.Exists(tableName))
            {
                return true;
            }

            return false;
        }
    }
}

