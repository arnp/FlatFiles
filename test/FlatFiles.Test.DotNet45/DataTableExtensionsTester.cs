﻿using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Threading;
using Xunit;

namespace FlatFiles.Test
{
    /// <summary>
    /// Tests the DataTableExtensions class.
    /// </summary>
    public class DataTableExtensionsTester
    {
        /// <summary>
        /// Setup for tests.
        /// </summary>
        public DataTableExtensionsTester()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
        }

        /// <summary>
        /// An exception should be thrown if the table is null.
        /// </summary>
        [Fact]
        public void TestReadFlatFile_DataTableNull_Throws()
        {
            DataTable table = null;
            StringReader stringReader = new StringReader(String.Empty);
            IReader parser = new SeparatedValueReader(stringReader);
            Assert.Throws<ArgumentNullException>(() => DataTableExtensions.ReadFlatFile(table, parser));
        }

        /// <summary>
        /// An exception should be thrown if the table is null.
        /// </summary>
        [Fact]
        public void TestReadFlatFile_ParserNull_Throws()
        {
            DataTable table = new DataTable();
            IReader parser = null;
            Assert.Throws<ArgumentNullException>(() => table.ReadFlatFile(parser));
        }

        /// <summary>
        /// The schema should be extracted from the file and then the data is used
        /// to populate the table.
        /// </summary>
        [Fact]
        public void TestReadFlatFile_ExtractsSchema_PopulatesTable()
        {
            SeparatedValueSchema schema = new SeparatedValueSchema();
            schema.AddColumn(new Int32Column("id"))
                .AddColumn(new StringColumn("name"))
                .AddColumn(new DateTimeColumn("created") { InputFormat = "MM/dd/yyyy", OutputFormat = "MM/dd/yyyy" })
                .AddColumn(new DecimalColumn("avg"));
            SeparatedValueOptions options = new SeparatedValueOptions() { IsFirstRecordSchema = true };

            StringWriter stringWriter = new StringWriter();
            SeparatedValueWriter builder = new SeparatedValueWriter(stringWriter, schema, options);
            builder.Write(new object[] { 123, "Bob", new DateTime(2012, 12, 31), 3.14159m });

            StringReader stringReader = new StringReader(stringWriter.ToString());
            DataTable table = new DataTable();
            IReader parser = new SeparatedValueReader(stringReader, options);
            table.ReadFlatFile(parser);
            Assert.Equal(4, table.Columns.Count);
            Assert.True(table.Columns.Contains("id"), "The ID column was not extracted.");
            Assert.True(table.Columns.Contains("name"), "The name column was not extracted.");
            Assert.True(table.Columns.Contains("created"), "The created column was not extracted.");
            Assert.True(table.Columns.Contains("avg"), "The AVG column was not extracted.");
            Assert.Equal(1, table.Rows.Count);
            DataRow row = table.Rows[0];
            object[] expected = new object[] { "123", "Bob", "12/31/2012", "3.14159" };
            object[] values = row.ItemArray;
            Assert.Equal(expected, values);
        }

        /// <summary>
        /// If there is any existing schema information or rows, it should be cleared out.
        /// </summary>
        [Fact]
        public void TestReadFlatFile_ClearsSchemaAndData()
        {
            // fill the table with some existing columns, constraints and data
            DataTable table = new DataTable();
            DataColumn column = table.Columns.Add("blah", typeof(int));
            table.Columns.Add("bleh", typeof(string));
            table.Constraints.Add("PK_blah", column, true);
            DataRow row = table.Rows.Add(new object[] { 123, "dopey" });
            row.AcceptChanges();

            const string text = @"id,name,created,avg
123,Bob,12/31/2012,3.14159";
            SeparatedValueOptions options = new SeparatedValueOptions() { IsFirstRecordSchema = true };
            StringReader stringReader = new StringReader(text);
            IReader parser = new SeparatedValueReader(stringReader, options);
            table.ReadFlatFile(parser);
            Assert.Equal(4, table.Columns.Count);
            Assert.True(table.Columns.Contains("id"), "The ID column was not extracted.");
            Assert.True(table.Columns.Contains("name"), "The name column was not extracted.");
            Assert.True(table.Columns.Contains("created"), "The created column was not extracted.");
            Assert.True(table.Columns.Contains("avg"), "The AVG column was not extracted.");
            Assert.Equal(1, table.Rows.Count);
            row = table.Rows[0];
            object[] expected = new object[] { "123", "Bob", "12/31/2012", "3.14159" };
            object[] values = row.ItemArray;
            Assert.Equal(expected, values);
        }

        /// <summary>
        /// If a schema is provided, it should be used to set the column types.
        /// </summary>
        [Fact]
        public void TestReadFlatFile_SchemaProvided_TypesUsed()
        {
            const string text = @"123,Bob,12/31/2012,3.14159";
            DataTable table = new DataTable();
            SeparatedValueSchema schema = new SeparatedValueSchema();
            schema.AddColumn(new Int32Column("id"))
                  .AddColumn(new StringColumn("name"))
                  .AddColumn(new DateTimeColumn("created"))
                  .AddColumn(new DoubleColumn("avg"));

            StringReader stringReader = new StringReader(text);
            IReader parser = new SeparatedValueReader(stringReader, schema);
            table.ReadFlatFile(parser);
            Assert.Equal(4, table.Columns.Count);
            Assert.True(table.Columns.Contains("id"), "The ID column was not extracted.");
            Assert.True(table.Columns.Contains("name"), "The name column was not extracted.");
            Assert.True(table.Columns.Contains("created"), "The created column was not extracted.");
            Assert.True(table.Columns.Contains("avg"), "The AVG column was not extracted.");
            Assert.Equal(1, table.Rows.Count);
            DataRow row = table.Rows[0];
            object[] expected = new object[] { 123, "Bob", new DateTime(2012, 12, 31), 3.14159 };
            object[] values = row.ItemArray;
            Assert.Equal(expected, values);
        }
    }
}
