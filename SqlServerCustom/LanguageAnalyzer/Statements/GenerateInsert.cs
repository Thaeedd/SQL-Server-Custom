using CustomSqlServer.LanguageAnalyzer;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System;
using SqlServerCustom.CommonMethods;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

public static class GenerateInsert
{
    public static void Insert(string query)
    {
        var lexer = new SqlLexer(query);

        var tokens = new List<SqlToken>();

        while (true)
        {
            var token = lexer.GetNextToken();

            if (token.Kind == SqlTokenKind.EndOfFile)
            {
                break;
            }

            tokens.Add(token);
        }

        var tableRecord = new Table
        {
            Records = new List<TableRecord>()
        };

        var genericCols = new List<string>();
        var genericTypes = new List<string>();
        var pkValue = string.Empty;
        var primaryKeyColumn = string.Empty;
        var isUnique = true;

        for (int i = 0; i < tokens.Count - 1; i++)
        {
            if (tokens[i].Kind == SqlTokenKind.InsertIntoTable)
            {
                tableRecord.TableName = tokens[i].Text;
                (genericCols, genericTypes) = TableStructure.GetTableStructureColumns(tableRecord.TableName);
                for (int j = 0; j < genericCols.Count; j++)
                {
                    var record = new TableRecord();
                    record.ColumnName = genericCols[j];
                    record.DataType = "NULL";

                    tableRecord.Records.Add(record);
                }

                if (File.Exists($@"C:\Tables\{tableRecord.TableName}Values_PK"))
                {
                    using (var reader = new StreamReader($@"C:\Tables\{tableRecord.TableName}Values_PK", true))
                    {
                        primaryKeyColumn = reader.ReadLine();
                    }
                }
            }

            if (tokens[i].Kind == SqlTokenKind.ColumnName)
            {
                for (int j = 0; j < tableRecord.Records.Count; j++)
                {
                    if (tableRecord.Records[j].ColumnName == tokens[i].Text)
                    {
                        if (genericTypes[j].ToLower() == "int")
                        {
                            try
                            {
                                Convert.ToInt32(tokens[i + 1].Text);
                            }
                            catch (Exception ex)
                            {
                                throw ex;
                            }
                        }
                        if (primaryKeyColumn == tokens[i].Text)
                        {
                            using(var reader = new StreamReader($@"C:\Tables\{tableRecord.TableName}Values_PK", true))
                            {
                                while (!reader.EndOfStream)
                                {
                                    var value = reader.ReadLine();
                                    if(value == tokens[i + 1].Text)
                                    {
                                        isUnique = false;
                                    }
                                }
                            }

                            using (var writer = new StreamWriter($@"C:\Tables\{tableRecord.TableName}Values_PK", true))
                            {
                                if (isUnique)
                                    writer.WriteLine(tokens[i + 1].Text);
                                else
                                    throw new Exception("The primary key value cannot be duplicated");
                            }
                        }

                        tableRecord.Records[j].DataType = tokens[i + 1].Text;
                    }
                }
            }
        }

        //if(primaryKeyColumn != string.Empty) 
        //{
        //    try
        //    {
        //        using (var writer = new StreamWriter($@"C:\Tables\{tableRecord.TableName}Values_PK", true))
        //        {
        //            writer.WriteLine(tableRecord);
        //        }
        //    }
        //    catch(Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        var json = JsonConvert.SerializeObject(tableRecord);

        using (var writer = new StreamWriter($@"C:\Tables\{tableRecord.TableName}", true))
        {
            if(isUnique)
                writer.WriteLine(json);
            else
                throw new Exception("The primary key value cannot be duplicated");
        }
    }
}