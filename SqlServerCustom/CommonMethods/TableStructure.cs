using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomSqlServer.LanguageAnalyzer;
using Newtonsoft.Json;

namespace SqlServerCustom.CommonMethods
{
    public static class TableStructure
    {
        public static (List<string>, List<string>) GetTableStructureColumns(string tableName)
        {
            var tableStructure = string.Empty;
            using (var reader = new StreamReader($@"C:\Tables\{tableName}", true))
            {
                tableStructure = reader.ReadLine();
            }

            var genericTable = new Table();
            genericTable = JsonConvert.DeserializeObject<Table>(tableStructure);

            var cols = new List<string>();
            var types = new List<string>();
            foreach (var column in genericTable.Records)
            {
                cols.Add(column.ColumnName);
                types.Add(column.DataType);
            }

            return (cols, types);
        }
    }
}
