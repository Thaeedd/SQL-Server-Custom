using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CustomSqlServer.LanguageAnalyzer.Statements;
using Newtonsoft.Json;
using System.IO;

namespace SqlServerCustom.Server
{
    public static class Server
    {
        public static void StartServer()
        {
            TcpListener server = null;
            Int32 port = 23456;
            IPAddress localAddr = IPAddress.Parse("127.0.0.1");

            try
            {
                // Start listening for client requests
                server = new TcpListener(localAddr, port);
                server.Start();
                Console.WriteLine("Server is waiting for connections...");

                while (true)
                {
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Client connected");

                    Thread clientThread = new Thread(() =>
                    {
                        HandleClient(client);
                    });

                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server error: {ex.Message}");
            }
            finally
            {
                server.Stop();
            }
        }

        static void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1000000];
            int bytesRead;

            //While there are no bytes left, we are reading the NetworkStream.
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                //Getting the query and for classification, we are displaying it on the console window.
                string query = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Received query: {query}");

                ServerResponse response = new ServerResponse();

                //We are checking the select statement from here.
                if (query.StartsWith("TAKE", StringComparison.OrdinalIgnoreCase) || query.StartsWith("ამოიღე", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        response.Data = GenerateSelect.Select(query);

                        //Check if the given select statement does not have any data in it,
                        //It means that either the column does not exist, or the data for the given table returns null.
                        bool isEmptySelect = true;
                        foreach (var item in response.Data)
                        {
                            if (item.Records.Count != 0)
                            {
                                isEmptySelect = false;
                            }
                        }
                        if (!isEmptySelect)
                            response.Message = "Select command completed succesfully.";
                        else
                            response.Message = "Either column does not exist or no data for this conditions.";
                    }
                    catch (DirectoryNotFoundException ex)
                    {
                        response.Message = "Missing from clause or bad syntax.";
                        Console.WriteLine(ex.Message);
                    }
                    catch (FileNotFoundException ex)
                    {
                        response.Message = "Given table not found.";
                        Console.WriteLine(ex.Message);
                    }
                    catch (Exception ex)
                    {
                        response.Message = "Unsupported Select statement, fix the query";
                        Console.WriteLine(ex.Message);
                    }
                }
                //We are checking Insert Into statement here.
                else if (query.StartsWith("ADD", StringComparison.OrdinalIgnoreCase) || query.StartsWith("დაამატე", StringComparison.OrdinalIgnoreCase))
                {
                    //In our case, the files are the tables, so we are checking if the file with given
                    //FileName (TableName) is reached, If not, it does not exist, or the syntax is incorrect.
                    try
                    {
                        GenerateInsert.Insert(query);
                        response.Message = "Insert command completed succesfully.";
                    }
                    catch (DirectoryNotFoundException ex)
                    {
                        response.Message = "Fail. Either table does not exist or bad syntax.";
                        Console.WriteLine(ex.Message);
                    }
                    catch (Exception)
                    {
                        response.Message = "The insert statement failed, fixed the query.";
                    }
                }
                //We are working the CREATE TABLE here.
                else if (query.StartsWith("CREATE TABLE", StringComparison.OrdinalIgnoreCase) || query.StartsWith("შექმენი ცხრილი", StringComparison.OrdinalIgnoreCase))
                {
                    //Here we are checking that there are no special characters in the TableName (FileName)
                    //Because as we know, on windows, the files can't be named special characters.
                    try
                    {
                        GenerateTable.GenerateTables(query);
                        response.Message = "Table Create command completed succesfully.";
                    }
                    catch (Exception)
                    {
                        response.Message = "Creating table failed. Unsupported table name or bad syntax.";
                    }
                }
                //Anything else that are not made by us, mark as Unsupported query.
                else
                {
                    response.Message = "Unsupported query";
                }

                //As we have a TCP connection with the client, we have NetworkStream, so, we are writing into
                //That NetworkStream for client to get responses from the server.
                string jsonResponse = JsonConvert.SerializeObject(response);
                byte[] responseBytes = Encoding.UTF8.GetBytes(jsonResponse);
                stream.Write(responseBytes, 0, responseBytes.Length);
            }

            client.Close();
        }
    }
}