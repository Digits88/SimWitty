// <copyright file="CsvWriter.cs" company="SimWitty (http://www.simwitty.org)">
//     Copyright © 2013 and distributed under the BSD license.
// </copyright>

namespace SimWitty.Library.Collector
{
    using System;
    using System.Data;
    using System.IO;

    /// <summary>
    /// Comma Separated Value (CSV) parser and writer.
    /// Adapted from: 
    /// CSV file parser and writer in C# (Part 1) by Andreas Knab
    /// http://knab.ws/blog/index.php?/archives/3-CSV-file-parser-and-writer-in-C-Part-1.html
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Rule tripped by author's name and URL.")]
    public class CsvWriter
    {
        /// <summary>
        /// Write the DataTable to a string.
        /// </summary>
        /// <param name="table">The source DataTable with the values.</param>
        /// <param name="header">True if including a header row with the column names.</param>
        /// <param name="quoteall">True if quoting all values.</param>
        /// <returns>Returns the DataTable represented by a in the Comma Separated Value format.</returns>
        public static string WriteToString(DataTable table, bool header, bool quoteall)
        {
            StringWriter writer = new StringWriter();
            WriteToStream(writer, table, header, quoteall);
            return writer.ToString();
        }
        
        /// <summary>
        /// Write the DataTable to a TextWriter stream.
        /// </summary>
        /// <param name="stream">A TextWriter stream to write the values to.</param>
        /// <param name="table">The source DataTable with the values.</param>
        /// <param name="header">True if including a header row with the column names.</param>
        /// <param name="quoteall">True if quoting all values.</param>
        public static void WriteToStream(TextWriter stream, DataTable table, bool header, bool quoteall)
        {
            if (header)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    WriteItem(stream, table.Columns[i].Caption, quoteall);
                    if (i < table.Columns.Count - 1)
                        stream.Write(',');
                    else
                        stream.Write(Environment.NewLine);
                }
            }

            foreach (DataRow row in table.Rows)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    WriteItem(stream, row[i], quoteall);
                    if (i < table.Columns.Count - 1)
                        stream.Write(',');
                    else
                        stream.Write(Environment.NewLine);
                }
            }
        }

        /// <summary>
        /// Write one item to one stream.
        /// </summary>
        /// <param name="stream">A TextWriter stream to write the values to.</param>
        /// <param name="item">The column name or column data item to write.</param>
        /// <param name="quoteall">True if quoting all values.</param>
        private static void WriteItem(TextWriter stream, object item, bool quoteall)
        {
            if (item == null)
                return;
            string s = item.ToString();
            if (quoteall || s.IndexOfAny("\",\x0A\x0D".ToCharArray()) > -1)
                stream.Write("\"" + s.Replace("\"", "\"\"") + "\"");
            else
                stream.Write(s);
        }
    }
}
