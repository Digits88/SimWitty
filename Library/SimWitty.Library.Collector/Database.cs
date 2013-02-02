// <copyright file="Database.cs" company="SimWitty (http://www.simwitty.org)">
//     Copyright © 2013 and distributed under the BSD license.
// </copyright>

namespace SimWitty.Library.Collector
{
    using System;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "The names are self-explanatory.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:EnumerationItemsMustBeDocumented", Justification = "Reviewed.")]
    public enum DatabaseClient
    {
        None = 0,
        Default = 1,
        SqlClient = 2,
        OleClient = 3
    }

    /// <summary>
    /// Database abstraction layer for SimWitty Collector Services and related tools.
    /// TODO: NOTE! OLE has not yet been tested -- 2009-02/25
    /// </summary>
    public class Database : IDisposable
    {
        #region Private members

        /// <summary>
        /// Internal value that is true if Dispose has been called.
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// Internal value that is true if Initialize has been called.
        /// </summary>
        private bool initialized = false;

        /// <summary>
        /// Internal OLE connection.
        /// </summary>
        private System.Data.OleDb.OleDbConnection oleConn;

        /// <summary>
        /// Internal SQL connection.
        /// </summary>
        private System.Data.SqlClient.SqlConnection sqlConn;

        /// <summary>
        /// Internal open connection to the database.
        /// </summary>
        private System.Data.IDbConnection connection;

        /// <summary>
        /// Internal value indicating whether the timeout should automatically increase on long running queries.
        /// </summary>
        private bool incrementTimeout = false;

        /// <summary>
        /// Internal value setting the default timeout for SQL commands (in seconds).
        /// </summary>
        private int commandTimeout = 60;

        /// <summary>
        /// Internal value setting the default timeout for database connections (in seconds).
        /// </summary>
        private int connectionTimeout = 60;

        /// <summary>
        /// The database name.
        /// </summary>
        private string databaseName = string.Empty;

        /// <summary>
        /// The data client (SQL, OLE, etc).
        /// </summary>
        private DatabaseClient dataClient = DatabaseClient.SqlClient;

        /// <summary>
        /// The data source string.
        /// </summary>
        private string dataSource = string.Empty;

        /// <summary>
        /// True if the <see cref="Database"/> instance has generated an exception during processing.
        /// </summary>
        private bool hasException = false;

        /// <summary>
        /// True if using Windows integrated authentication. False if using SQL authentication.
        /// </summary>
        private bool integratedAuthentication = false;

        /// <summary>
        /// True if the <see cref="Database"/> instance has an active database connection.
        /// </summary>
        private bool isConnected = false;

        /// <summary>
        /// The internal exception that was last thrown during processing.
        /// </summary>
        private System.Exception lastException;

        /// <summary>
        /// If using SQL authentication, the current password.
        /// </summary>
        private string password = string.Empty;

        /// <summary>
        /// If true, throw exceptions. If false, store exceptions.
        /// </summary>
        private bool throwExceptions = false;

        /// <summary>
        /// If true, encrypt the SQL connection.
        /// </summary>
        private bool useEncryption = false;

        /// <summary>
        /// If using SQL authentication, the current username.
        /// </summary>
        private string username = string.Empty;

        /// <summary>
        /// If true, use a new, separate, connection for each new, separate, command.
        /// </summary>
        private bool useSeparateConnections = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Database"/> class.
        /// </summary>
        public Database()
        {
            this.DataClient = DatabaseClient.Default;
            this.initialized = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Database"/> class.
        /// </summary>
        /// <param name="connectionString">The database connection parameters.</param>
        public Database(ConnectionString connectionString)
        {
            ConnectionString = connectionString;
            this.DataClient = DatabaseClient.SqlClient;
            this.initialized = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Database"/> class.
        /// </summary>
        /// <param name="connectionString">A string containing the database connection parameters.</param>
        public Database(string connectionString)
        {
            this.ConnectionProperties = connectionString;
            this.DataClient = DatabaseClient.SqlClient;
            this.initialized = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Database"/> class.
        /// </summary>
        /// <param name="connection">An active connection to the database.</param>
        public Database(System.Data.IDbConnection connection)
        {
            this.Connection = connection;
        }

        #endregion
        
        #region Destructors

        /// <summary>
        /// Finalizes an instance of the <see cref="Database"/> class.
        /// </summary>
        ~Database()
        {
            this.Dispose(false);
        }
        
        #endregion
        
        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether to automatically increases the CommandTimeout when queries take longer than expected.
        /// </summary>
        public bool AutoIncrementTimeout
        {
            get
            {
                return this.incrementTimeout;
            }

            set
            {
                this.incrementTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the time to wait while trying to establish a connection before terminating the attempt and generating an error.
        /// </summary>
        public int CommandTimeout
        {
            get
            {
                return this.commandTimeout;
            }

            set
            {
                this.commandTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the active database connection.
        /// </summary>
        public System.Data.IDbConnection Connection
        {
            get
            {
                return this.connection;
            }

            set
            {
                this.connection = value;
            }
        }

        /// <summary>
        /// Gets or sets the string used to open a database connection.
        /// </summary>
        public string ConnectionProperties
        {
            get
            {
                string s = string.Concat(
                    "Data Source=",
                    this.DataSource,
                    ";Database=",
                    this.DatabaseName,
                    ";Connection Timeout=",
                    this.ConnectionTimeout.ToString(),
                    ";Encrypt=",
                    this.UseEncryption.ToString(),
                    ";");

                if (this.IntegratedAuthentication == true)
                {
                    return string.Concat(s, "Integrated Security=True;");
                }
                else
                {
                    return string.Concat(
                        s,
                        "User ID=",
                        this.UserName,
                        ";Password=",
                        this.Password,
                        ";");
                }
            }

            set
            {
                this.ConnectionString = new ConnectionString(value);
            }
        }

        /// <summary>
        /// Gets or sets the string used to open a database connection.
        /// </summary>
        public ConnectionString ConnectionString
        {
            get
            {
                return new ConnectionString(this.ConnectionProperties);
            }

            set
            {
                int i;

                foreach (NameValuePair nvp in value)
                {
                    switch (nvp.Name.ToLower())
                    {
                        // "With one exception, (unlike the C++ switch statement), C# does not support an implicit fall through from one case label to another. The one exception is if a case statement has no code."
                        case "addr":
                        case "address":
                        case "data source":
                        case "network address":
                        case "server":
                            this.DataSource = nvp.Value;
                            break;

                        case "database":
                        case "initial catalog":
                            this.DatabaseName = nvp.Value;
                            break;

                        case "connect timeout":
                        case "connection timeout":

                            /*
                             * Specify IFormatProvider 
                             * "A method or constructor calls one or more members that have overloads that accept 
                             * a System.IFormatProvider parameter, and the method or constructor does not call 
                             * the overload that takes the IFormatProvider parameter." 
                             * http://msdn2.microsoft.com/en-us/library/ms182190(VS.80).aspx
                             */

                            if (int.TryParse(
                                nvp.Value,
                                System.Globalization.NumberStyles.Integer,
                                System.Globalization.NumberFormatInfo.CurrentInfo,
                                out i))
                                this.ConnectionTimeout = i;
                            break;

                        case "command timeout":

                            /*
                             * Specify IFormatProvider 
                             * "A method or constructor calls one or more members that have overloads that accept 
                             * a System.IFormatProvider parameter, and the method or constructor does not call 
                             * the overload that takes the IFormatProvider parameter." 
                             * http://msdn2.microsoft.com/en-us/library/ms182190(VS.80).aspx
                             */

                            if (int.TryParse(
                                nvp.Value,
                                System.Globalization.NumberStyles.Integer,
                                System.Globalization.NumberFormatInfo.CurrentInfo,
                                out i))
                                this.CommandTimeout = i;
                            break;

                        case "encrypt":
                            if (nvp.Value.ToLower() == "yes" | nvp.Value.ToLower() == "true")
                                this.UseEncryption = true;
                            else
                                this.UseEncryption = false;
                            break;

                        case "integrated security":
                        case "trusted connection":
                        case "trusted_connection":
                            if (nvp.Value.ToLower() == "yes" | nvp.Value.ToLower() == "true" | nvp.Value.ToLower() == "sspi")
                            {
                                this.IntegratedAuthentication = true;
                                this.UserName = "Integrated Authentication";
                                this.Password = string.Empty;
                            }
                            else
                            {
                                this.IntegratedAuthentication = false;
                            }

                            break;

                        case "password":
                        case "pwd":
                            this.Password = nvp.Value;
                            break;

                        case "user id":
                        case "username":
                            this.UserName = nvp.Value;
                            break;

                        default:
                            // Skip this name-value pair
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the time to wait while trying to establish a connection before terminating the attempt and generating an error.
        /// </summary>
        public int ConnectionTimeout
        {
            get
            {
                return this.connectionTimeout;
            }

            set
            {
                if (value >= 0)
                {
                    this.connectionTimeout = value;
                }
                else
                {
                    this.connectionTimeout = 0;
                }
            }
        }

        /// <summary>
        /// Gets or sets the name of the target database or the initial catalog.
        /// </summary>
        public string DatabaseName
        {
            get
            {
                return this.databaseName;
            }

            set
            {
                this.databaseName = value;
            }
        }

        /// <summary>
        /// Gets or sets the type of database client to use.
        /// </summary>
        public DatabaseClient DataClient
        {
            get
            {
                return this.dataClient;
            }

            set
            {
                if (this.IsConnected == false)
                {
                    this.dataClient = value;
                }
                else
                {
                    this.hasException = true;
                    this.lastException = new System.Exception("You cannot change the database client type when connected to the database. Disconnect first.");
                    if (this.ThrowExceptions == true) throw this.lastException;
                }
            }
        }

        /// <summary>
        /// Gets or sets the name of the SQL Server instance or ODBC database server.
        /// </summary>
        public string DataSource
        {
            get
            {
                return this.dataSource;
            }

            set
            {
                this.dataSource = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether an exception occurred on the last operation.
        /// </summary>
        public bool HasException
        {
            get
            {
                return this.hasException;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Database connections should authenticate using the current Windows user credentials
        /// </summary>
        public bool IntegratedAuthentication
        {
            get
            {
                return this.integratedAuthentication;
            }

            set
            {
                this.integratedAuthentication = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the Database connection is actively connected.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return this.isConnected;
            }
        }

        /// <summary>
        /// Gets the exception that occurred on the last operation.
        /// </summary>
        public System.Exception LastException
        {
            get
            {
                if (this.HasException == true)
                {
                    return this.lastException;
                }
                else
                {
                    return new SystemException();
                }
            }
        }

        /// <summary>
        /// Gets or sets the password to use on new connections. Ignored when IntegratedAuthentication=true.
        /// </summary>        
        public string Password
        {
            get
            {
                return this.password;
            }

            set
            {
                this.password = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether exceptions are thrown. Otherwise, exceptions are caught and stored (HasException / LastException).
        /// </summary>
        public bool ThrowExceptions
        {
            get
            {
                return this.throwExceptions;
            }

            set
            {
                this.throwExceptions = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether SQL Server uses SSL encryption for all data sent between the client and server if the server has a certificate installed.
        /// </summary>
        public bool UseEncryption
        {
            get
            {
                return this.useEncryption;
            }

            set
            {
                this.useEncryption = value;
            }
        }

        /// <summary>
        /// Gets or sets the username to use for new connections. Ignored when IntegratedAuthentication=true.
        /// </summary>
        public string UserName
        {
            get
            {
                return this.username;
            }

            set
            {
                this.username = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether a separate connection will be opened for each ExecuteQuery or GetDataTable call.
        /// </summary>
        public bool UseSeparateConnections
        {
            get
            {
                return this.useSeparateConnections;
            }

            set
            {
                this.useSeparateConnections = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// WORKING! Method for quickly building a SQL Insert transaction. 
        /// </summary>
        /// <param name="table">Resultant DataTable from a GetDataTable(...).</param>
        /// <param name="rowIndex">Index of the row to create an SQL Insert from.</param>
        /// <param name="sqlTableName">SQL table to insert into.</param>
        /// <returns>Returns an executable SQL statement.</returns>
        public string BuildSqlInsert(
            System.Data.DataTable table,
            int rowIndex,
            string sqlTableName)
        {
            // If the object is disposed, exit returning nothing
            if (this.disposed == true) return string.Empty;

            return string.Empty;

            // TODO: Rewrite this code from the original VB.NET version of EventLogger.

            /*
             * Below is the old school VB.NET way of building the insert (2007)
            
                '- Dimension Variables

                Dim ColumnName As String
                Dim GetTypeStr As String
                Dim oCol As System.Data.DataColumn
                Dim oRow As System.Data.DataRow = Table.Rows(RowIndex)
                Dim sqlSet As String = ""
                Dim sqlValue As String = ""

                '- Loop thru the columns, and build the SQL Set and SQL Value statements

                ' For Each Column in the DataTable
                '   Get the Column's name
                '   Get the data type of the Row's Column. Note that this is different than getting it from the column itself, as we want to know if the Row's column is Null
                '   Select Case on the data type
                '       Case for numbers
                '           Add the Column name to the SQL Set statement
                '           Add the number to the SQL Value statement, no quotes needed
                '       Case for strings
                '           Add the Column name to the SQL Set statement
                '           Add to the SQL Value statement a single quote, then the string, followed by a single quote 
                '       Case for Null values
                '           Do not add the column name or the value
                '       Case Default
                '           Throw an exception so that we can look at the data type, and add it where appropriate
                '   End Select
                ' Next

                For Each oCol In Table.Columns
                    ColumnName = oCol.ColumnName
                    GetTypeStr = oRow.Item(ColumnName).GetType.ToString

                    Select Case GetTypeStr
                        Case "System.Int16", "System.Int32", "System.Int64", "System.Byte"

                            If sqlSet.Length > 0 Then sqlSet = sqlSet & ", "
                            sqlSet = sqlSet & "[" & ColumnName & "]"

                            If sqlValue.Length > 0 Then sqlValue = sqlValue & ", "
                            sqlValue = sqlValue & CStr(oRow.Item(ColumnName))

                        Case "System.String", "System.DateTime"

                            If sqlSet.Length > 0 Then sqlSet = sqlSet & ", "
                            sqlSet = sqlSet & "[" & ColumnName & "]"

                            If sqlValue.Length > 0 Then sqlValue = sqlValue & ", "
                            sqlValue = sqlValue & "'" & oRow.Item(ColumnName).ToString.Trim & "'"

                        Case "System.DBNull"
                            ' Do not add this
                        Case Else
                            Throw New System.Exception("JWG.Db.Toolkit.BuildSqlInsert did not recognize the data type '" & GetTypeStr & "', and therefore cannot build the INSERT query.")
                    End Select
                Next


                '- Clean up the SQL Table Name; note that we accept empty or null SQL Table Names

                ' Remove any preceeding [
                ' Remove any ending ]
                ' Remove whitespace; note that we cannot use .Trim method as it throws an exception if the string is null (Object reference not set to an instance of an object.)

                SqlTableName = Replace(SqlTableName, "[", "")
                SqlTableName = Replace(SqlTableName, "]", "")
                SqlTableName = Trim(SqlTableName)

                '- Return the values

                ' If there is a SQL Table Name, then return INSERT Into [sqlTablename] (sqlSet) Values (sqlValue);
                ' Else return just (sqlSet) Values (sqlValue);

                If SqlTableName.Length > 0 Then
                    Return "INSERT Into [" & SqlTableName & "] (" & sqlSet & ") Values (" & sqlValue & ");"
                Else
                    Return "(" & sqlSet & ") Values (" & sqlValue & ");"
                End If

                '- Cleanup and clearout

                Table.Dispose()
                Table = Nothing
                oCol = Nothing
                oRow = Nothing
                Exit Function

             */
        }

        /// <summary>
        /// Open a connection and authenticate to the data source. 
        /// </summary>
        public void Connect()
        {
            // Do not proceed if the object has not been initialized
            if (this.initialized == false)
            {
                this.hasException = true;
                this.lastException = new System.Exception("You cannot connect to the database until the ConnectionString properties are completed (e.g., Data Source, Database, and user credentials).");
                if (this.ThrowExceptions == true) throw this.lastException;
                return;
            }

            // Clear past exceptions from your mind
            this.hasException = false;

            switch (this.DataClient)
            {
                case DatabaseClient.OleClient:

                    try
                    {
                        this.oleConn = new System.Data.OleDb.OleDbConnection(this.ConnectionProperties);
                        this.oleConn.Open();
                        this.isConnected = true;
                    }
                    catch (Exception exception)
                    {
                        this.isConnected = false;
                        this.hasException = true;
                        this.lastException = exception;
                        if (this.ThrowExceptions == true) throw this.lastException;
                    }

                    break;

                case DatabaseClient.SqlClient:

                    try
                    {
                        this.sqlConn = new System.Data.SqlClient.SqlConnection(this.ConnectionProperties);
                        this.sqlConn.Open();
                        this.isConnected = true;
                    }
                    catch (Exception exception)
                    {
                        this.isConnected = false;
                        this.hasException = true;
                        this.lastException = exception;
                        if (this.ThrowExceptions == true) throw this.lastException;
                    }

                    break;

                default:
                    this.isConnected = false;
                    this.hasException = true;
                    this.lastException = new SystemException("The database client type entered is invalid (DataClient=" + this.DataClient.ToString() + ").");
                    if (this.ThrowExceptions == true) throw this.lastException;
                    break;
            }
        }

        /// <summary>
        ///  Log off the data source and close the connection.
        /// </summary>
        public void Disconnect()
        {
            // If the object is disposed, exit returning nothing
            if (this.disposed == true) return;

            // Clear any past exceptions
            this.hasException = false;

            // Do not proceed if not connected to the database
            if (this.IsConnected == false) return;

            // Disconnect using the appropriate Database client
            switch (this.DataClient)
            {
                case DatabaseClient.OleClient:

                    try
                    {
                        this.oleConn.Close();
                        this.isConnected = false;
                    }
                    catch (Exception exception)
                    {
                        this.hasException = true;
                        this.lastException = exception;
                        if (this.ThrowExceptions == true) throw this.lastException;
                    }

                    break;

                case DatabaseClient.SqlClient:

                    try
                    {
                        this.sqlConn.Close();
                        this.isConnected = false;
                    }
                    catch (Exception exception)
                    {
                        this.hasException = true;
                        this.lastException = exception;
                        if (this.ThrowExceptions == true) throw this.lastException;
                    }

                    break;

                default:
                    this.isConnected = false;
                    this.hasException = true;
                    this.lastException = new SystemException("The database client type entered is invalid (DataClient=" + this.DataClient.ToString() + ").");
                    if (this.ThrowExceptions == true) throw this.lastException;
                    break;
            }
        }

        /// <summary>
        /// Dispose of the database automation object.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Run a transact SQL query against the database.
        /// </summary>
        /// <param name="sqlQuery">String with the SQL query (Insert, Update, Delete, etc).</param>
        /// <returns>The number of rows affected or -1 if an exception occurs.</returns>
        public int ExecuteQuery(string sqlQuery)
        {
            // Do not proceed if the database is not connected.
            if (this.IsConnected == false)
            {
                this.hasException = true;
                this.lastException = new System.Exception("You cannot query a database until you connect to it (ExecuteQuery(string). Connect first.");
                if (this.ThrowExceptions == true) throw this.lastException;
                return -1;
            }

            // Clear past exceptions
            this.hasException = false;

            /* 
             * Switch to the appropriate database client and execute the query
             * 
             * Set the default output to -1 (which means that there was an error)
             * Create a Command object
             * Set the Command to use the current database's object
             * Set the Command's timeout value (if exceeded, an exception will occur)
             * Execute the SQL query, populating the output with the number of rows affected
             * Dispose of the Command object
             */

            int output = -1;

            switch (this.DataClient)
            {
                case DatabaseClient.OleClient:

                    try
                    {
                        System.Data.OleDb.OleDbCommand cmd = new System.Data.OleDb.OleDbCommand();
                        cmd.Connection = this.oleConn;
                        cmd.CommandText = sqlQuery;
                        cmd.CommandTimeout = this.CommandTimeout;
                        output = cmd.ExecuteNonQuery();
                        cmd.Dispose();
                    }
                    catch (Exception exception)
                    {
                        hasException = true;
                        lastException = exception;

                        if (ThrowExceptions == true)
                            throw lastException;
                    }

                    break;

                case DatabaseClient.SqlClient:

                    try
                    {
                        System.Data.SqlClient.SqlCommand cmd = new System.Data.SqlClient.SqlCommand();
                        cmd.Connection = this.sqlConn;
                        cmd.CommandText = sqlQuery;
                        cmd.CommandTimeout = this.CommandTimeout;
                        output = cmd.ExecuteNonQuery();
                        cmd.Dispose();
                    }
                    catch (Exception exception)
                    {
                        hasException = true;
                        lastException = exception;

                        if (ThrowExceptions == true) throw lastException;
                    }

                    break;

                default:
                    this.isConnected = false;
                    this.hasException = true;
                    this.lastException = new SystemException("The database client type entered is invalid (DataClient=" + this.DataClient.ToString() + ").");

                    if (this.ThrowExceptions == true)
                        throw this.lastException;

                    break;
            }

            return output;
        }

        /// <summary>
        /// WORKING! Looks up a specific column specified by Database, Table name, and Column name.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="columnName">Name of the column.</param>
        /// <param name="dataType">(return) Data type for the column.</param>
        /// <param name="characterMaximumLength">(return) Maximum character length.</param>
        /// <param name="numericPrecision">(return) Numeric precision.</param>
        /// <param name="colDataType">(return) Custom column data type.</param>
        public void GetColumnInformation(
            string tableName,
            string columnName,
            ref string dataType,
            ref long characterMaximumLength,
            ref long numericPrecision,
            ref string colDataType)
        {
            // If the object is disposed, exit returning nothing
            if (this.disposed == true) return;

            // GetColumnInformation
            // Looks up a specific column specified by Database, Table name, and Column name.
            // Fills the reference values for the Data Type, Character Maximum Length, and 
            // Numeric Precision. Also fills VbDataType, which can be System.String or 
            // System.Integer.
            // 
            // Returns True if the Column is found, False if not.
            // Character Maximum Length and Numeric Precision return -1 if they are NULL in the
            // database.
            //
            // If both Character Maximum Length and Numeric Precision are -1, which I have not
            // seen, VbDataType returns Unknown.
        }

        /// <summary>
        /// Run a transact SQL query against the database.
        /// </summary>
        /// <param name="sqlQuery">String with the SQL query (Select, etc).</param>
        /// <returns>The DataTable with the result set. Returns a New, empty DataTable if there are any exceptions.</returns>
        public System.Data.DataTable GetDataTable(string sqlQuery)
        {
            // Do not proceed if the database is not connected.
            if (this.IsConnected == false)
            {
                this.hasException = true;
                this.lastException = new System.Exception("You cannot query a database until you connect to it (ExecuteQuery(string). Connect first.");

                if (this.ThrowExceptions == true) throw this.lastException;

                return new System.Data.DataTable();
            }

            // Clear past exceptions
            this.hasException = false;

            /*
             * Switch to the appropriate database client and execute the query
             * 
             * Create an adapter to run the SQL query
             * It was found some WFS Queries exceeded the default time out, so we bump up the command timeout
             * Fill the DataTable with the results of the SQL query
             * Dispose of the Adapter
             * Send the Adapter to Nothing
             * Return the DataTable
             */

            System.Data.DataTable output = new System.Data.DataTable();

            switch (this.DataClient)
            {
                case DatabaseClient.OleClient:

                    try
                    {
                        System.Data.OleDb.OleDbDataAdapter oleAdapter = new System.Data.OleDb.OleDbDataAdapter(sqlQuery, this.oleConn);
                        oleAdapter.SelectCommand.CommandTimeout = this.CommandTimeout;
                        oleAdapter.Fill(output);
                        oleAdapter.Dispose();
                        return output;
                    }
                    catch (Exception exception)
                    {
                        this.hasException = true;
                        this.lastException = exception;
                        if (this.ThrowExceptions == true) throw this.lastException;
                    }

                    break;

                case DatabaseClient.SqlClient:

                    try
                    {
                        System.Data.SqlClient.SqlDataAdapter sqlAdapter = new System.Data.SqlClient.SqlDataAdapter(sqlQuery, this.sqlConn);
                        sqlAdapter.SelectCommand.CommandTimeout = this.CommandTimeout;
                        sqlAdapter.Fill(output);
                        sqlAdapter.Dispose();
                        return output;
                    }
                    catch (Exception exception)
                    {
                        this.hasException = true;
                        this.lastException = exception;
                        if (this.ThrowExceptions == true) throw this.lastException;
                    }

                    break;

                default:
                    this.hasException = true;
                    this.lastException = new SystemException("The database client type entered is invalid (DataClient=" + this.DataClient.ToString() + ").");
                    if (this.ThrowExceptions == true) throw this.lastException;
                    break;
            }

            return new System.Data.DataTable();
        }

        /// <summary>
        /// Close the current Database connection, pause, and then reconnect.  
        /// </summary>
        public void Reconnect()
        {
            this.Reconnect(5 * 1000); // Reconnect and pause for five seconds
        }

        /// <summary>
        /// Close the current Database connection, pause, and then reconnect.  
        /// </summary>
        /// <param name="millisecondsTimeout">Milliseconds to pause after disconnecting and before reconnecting.</param>
        public void Reconnect(int millisecondsTimeout)
        {
            // Clear the current exception
            this.hasException = false;

            // If the object is disposed, exit returning nothing
            if (this.disposed == true) return;

            // Disconnect. Pause x number of seconds. Reconnect.
            this.Disconnect();
            if (this.hasException == true) return;
            System.Threading.Thread.Sleep(millisecondsTimeout);
            this.Connect();
            return;
        }

        /// <summary>
        /// Dispose of the database automation object.
        /// </summary>
        /// <param name="disposing">The current state.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                /*
                 * Disconnect if necessary
                 * Dispose dependent objects first
                 * Clear any exceptions
                 * Set the object as unititialized
                 * Mark the object as disposed
                 */

                if (this.IsConnected == true) this.Disconnect();

                this.oleConn.Dispose();
                this.sqlConn.Dispose();

                this.hasException = false;
                this.initialized = false;
                this.disposed = true;
            }

            // Free unmanaged resources
        }

        #endregion
    }
}