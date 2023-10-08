//The things that server generally returns to the client are in an object, ServerResponse
//Here we can also store the data from the select statement, as our project grows, this class will
//Be very flexible to work with.
using CustomSqlServer.LanguageAnalyzer;
using System.Collections.Generic;

public class ServerResponse
{
    public string Message { get; set; } // For INSERT and CREATE TABLE queries
    public List<Table> Data { get; set; }     // For SELECT queries
}