#region License, Terms and Author(s)

//
// ELMAH - Error Logging Modules and Handlers for ASP.NET
// Copyright (c) 2007 Atif Aziz. All rights reserved.
//
//  Author(s):
//
//      James Driscoll, mailto:jamesdriscoll@btinternet.com
//
// This library is free software; you can redistribute it and/or modify it 
// under the terms of the New BSD License, a copy of which should have 
// been delivered along with this distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT 
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A 
// PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT 
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT 
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, 
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY 
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT 
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//

#endregion

// All code in this file requires .NET Framework 2.0 or later.

#if !NET_1_1 && !NET_1_0

[assembly: Elmah.Scc("$Id$")]

namespace Elmah
{

    #region Imports

    using System;
    using System.Configuration;
    using System.Data;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Xml;
    using VistaDB;
    using VistaDB.DDA;
    using VistaDB.Provider;

    using IDictionary = System.Collections.IDictionary;
    using IList = System.Collections.IList;
    using StringReader = System.IO.StringReader;
    using StringWriter = System.IO.StringWriter;

    #endregion

    /// <summary>
    /// An <see cref="ErrorLog"/> implementation that uses VistaDB as its backing store.
    /// </summary>

    public class VistaDBErrorLog : ErrorLog
    {
        private readonly string _connectionString;

        // TODO - don't think we have to limit strings in VistaDB, so decide if we really need this
        // Is it better to keep it for consistency, or better to exploit the full potential of the database??
        private const int _maxAppNameLength = 60;

        /// <summary>
        /// Initializes a new instance of the <see cref="VistaDBErrorLog"/> class
        /// using a dictionary of configured settings.
        /// </summary>

        public VistaDBErrorLog(IDictionary config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            _connectionString = GetConnectionString(config);

            //
            // If there is no connection string to use then throw an 
            // exception to abort construction.
            //

            if (_connectionString.Length == 0)
                throw new ApplicationException("Connection string is missing for the VistaDB error log.");

            InitializeDatabase();

            string appName = Mask.NullString((string)config["applicationName"]);

            if (appName.Length > _maxAppNameLength)
            {
                throw new ApplicationException(string.Format(
                    "Application name is too long. Maximum length allowed is {0} characters.",
                    _maxAppNameLength.ToString("N0")));
            }

            ApplicationName = appName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VistaDBErrorLog"/> class
        /// to use a specific connection string for connecting to the database.
        /// </summary>

        public VistaDBErrorLog(string connectionString)
        {
            if (connectionString == null)
                throw new ArgumentNullException("connectionString");

            if (connectionString.Length == 0)
                throw new ArgumentException(null, "connectionString");

            _connectionString = connectionString;

            InitializeDatabase();
        }

        /// <summary>
        /// Gets the name of this error log implementation.
        /// </summary>

        public override string Name
        {
            get { return "VistaDB Error Log"; }
        }

        /// <summary>
        /// Gets the connection string used by the log to connect to the database.
        /// </summary>

        public virtual string ConnectionString
        {
            get { return _connectionString; }
        }

        /// <summary>
        /// Logs an error to the database.
        /// </summary>
        /// <remarks>
        /// Use the stored procedure called by this implementation to set a
        /// policy on how long errors are kept in the log. The default
        /// implementation stores all errors for an indefinite time.
        /// </remarks>

        public override string Log(Error error)
        {
            if (error == null)
                throw new ArgumentNullException("error");

            StringWriter sw = new StringWriter();

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.NewLineOnAttributes = true;
            settings.CheckCharacters = false;
            XmlWriter writer = XmlWriter.Create(sw, settings);

            try
            {
                writer.WriteStartElement("error");
                error.ToXml(writer);
                writer.WriteEndElement();
                writer.Flush();
            }
            finally
            {
                writer.Close();
            }

            string errorXml = sw.ToString();
            Guid id = Guid.NewGuid();

            using (VistaDBConnection connection = new VistaDBConnection(this.ConnectionString))
            using (VistaDBCommand command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = "ELMAH_LogError";
                command.CommandType = CommandType.StoredProcedure;

                VistaDBParameterCollection parameters = command.Parameters;
                parameters.Clear();
                parameters.Add("@ErrorId", VistaDBType.UniqueIdentifier).Value = id;
                parameters.Add("@Application", VistaDBType.NVarChar, _maxAppNameLength).Value = ApplicationName;
                parameters.Add("@Host", VistaDBType.NVarChar, 30).Value = error.HostName;
                parameters.Add("@Type", VistaDBType.NVarChar, 100).Value = error.Type;
                parameters.Add("@Source", VistaDBType.NVarChar, 60).Value = error.Source;
                parameters.Add("@Message", VistaDBType.NVarChar, 500).Value = error.Message;
                parameters.Add("@User", VistaDBType.NVarChar, 50).Value = error.User;
                parameters.Add("@AllXml", VistaDBType.NText).Value = errorXml;
                parameters.Add("@StatusCode", VistaDBType.Int).Value = error.StatusCode;
                parameters.Add("@TimeUtc", VistaDBType.DateTime).Value = error.Time.ToUniversalTime();

                command.ExecuteNonQuery();
                return id.ToString();
            }
        }

        /// <summary>
        /// Returns a page of errors from the databse in descending order 
        /// of logged time.
        /// </summary>

        private static string EscapeApostrophes(string text)
        {
            return text.Replace("'", "''");
        }

        public override int GetErrors(int pageIndex, int pageSize, IList errorEntryList)
        {
            if (pageIndex < 0)
                throw new ArgumentOutOfRangeException("pageIndex", pageIndex, null);

            if (pageSize < 0)
                throw new ArgumentOutOfRangeException("pageSize", pageSize, null);

            VistaDBConnectionStringBuilder builder = new VistaDBConnectionStringBuilder(_connectionString);


            // Use the VistaDB Direct Data Access objects
            IVistaDBDDA ddaObjects = VistaDBEngine.Connections.OpenDDA();
            // Create a connection object to a VistaDB database
            IVistaDBDatabase vistaDB = ddaObjects.OpenDatabase(ResolveDataSourceFilePath(builder.DataSource), builder.OpenMode, builder.Password);
            // Open the table
            IVistaDBTable elmahTable = vistaDB.OpenTable("ELMAH_Error", false, true);

            elmahTable.ActiveIndex = "IX_ELMAH_Error_App_Time_Seq";

            if (errorEntryList != null)
            {
                if (!elmahTable.EndOfTable)
                {
                    // move to the correct record
                    elmahTable.First();
                    elmahTable.MoveBy(pageIndex * pageSize);

                    int rowsProcessed = 0;

                    // Traverse the table to get the records we want
                    while (!elmahTable.EndOfTable && rowsProcessed < pageSize)
                    {
                        rowsProcessed++;

                        Guid guid = (Guid)elmahTable.Get("ErrorId").Value;
                        Error error = NewError();

                        error.ApplicationName = (string)elmahTable.Get("Application").Value;
                        error.HostName = (string)elmahTable.Get("Host").Value;
                        error.Type = (string)elmahTable.Get("Type").Value;
                        error.Source = (string)elmahTable.Get("Source").Value;
                        error.Message = (string)elmahTable.Get("Message").Value;
                        error.User = (string)elmahTable.Get("User").Value;
                        error.StatusCode = (int)elmahTable.Get("StatusCode").Value;
                        error.Time = ((DateTime)elmahTable.Get("TimeUtc").Value).ToLocalTime();

                        errorEntryList.Add(new ErrorLogEntry(this, guid.ToString(), error));

                        // move to the next record
                        elmahTable.Next();
                    }
                }
            }
            
            return Convert.ToInt32(elmahTable.RowCount);
        }

        /// <summary>
        /// Returns the specified error from the database, or null 
        /// if it does not exist.
        /// </summary>

        public override ErrorLogEntry GetError(string id)
        {
            if (id == null)
                throw new ArgumentNullException("id");

            if (id.Length == 0)
                throw new ArgumentException(null, "id");

            Guid errorGuid;

            try
            {
                errorGuid = new Guid(id);
            }
            catch (FormatException e)
            {
                throw new ArgumentException(e.Message, "id", e);
            }

            string errorXml;

            using (VistaDBConnection connection = new VistaDBConnection(this.ConnectionString))
            using (VistaDBCommand command = connection.CreateCommand())
            {
                command.CommandText = "ELMAH_GetErrorXml";
                command.CommandType = CommandType.StoredProcedure;

                VistaDBParameterCollection parameters = command.Parameters;
                parameters.Add("@ErrorId", VistaDBType.UniqueIdentifier).Value = errorGuid;

                connection.Open();
                
                // NB this has been deliberately done like this as command.ExecuteScalar 
                // is not exhibiting the expected behaviour in VistaDB at the moment
                using (VistaDBDataReader dr = command.ExecuteReader())
                {
                    if (dr.Read())
                        errorXml = dr[0] as string;
                    else
                        errorXml = null;
                }
            }

            if (errorXml == null)
                return null;

            StringReader sr = new StringReader(errorXml);
            XmlTextReader reader = new XmlTextReader(sr);

            if (!reader.IsStartElement("error"))
                throw new ApplicationException("The error XML is not in the expected format.");

            Error error = NewError();
            error.FromXml(reader);

            return new ErrorLogEntry(this, id, error);
        }

        /// <summary>
        /// Creates a new and empty instance of the <see cref="Error"/> class.
        /// </summary>

        protected virtual Error NewError()
        {
            return new Error();
        }

        /// <remarks>
        /// This method is excluded from inlining so that if 
        /// HostingEnvironment does not need JIT-ing if it is not implicated
        /// by the caller.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string MapPath(string path)
        {
            return System.Web.Hosting.HostingEnvironment.MapPath(path);
        }

        private static string ResolveDataSourceFilePath(string path)
        {
            const string DataDirectoryMacroString = "|datadirectory|";

            if (string.IsNullOrEmpty(path))
                return string.Empty;

            // check to see if it starts with a ~/ and if so map it and return it
            if (path.StartsWith("~/"))
                return MapPath(path);

            // else see if it uses the |DataDirectory| macro and if so perform the appropriate substitution
            if (path.StartsWith(DataDirectoryMacroString, StringComparison.OrdinalIgnoreCase))
            {
                // first try to get the data directory from the CurrentDomain
                string baseDirectory = AppDomain.CurrentDomain.GetData("DataDirectory") as string;
                // if not, try the BaseDirectory
                if (string.IsNullOrEmpty(baseDirectory))
                    baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                // make sure we haven't got a null string
                if (baseDirectory == null)
                    baseDirectory = string.Empty;
                // work out if we've got backslashes at the end of our variables
                int length = DataDirectoryMacroString.Length;
                bool baseDirectoryHasBackSlash = (0 < baseDirectory.Length) && (baseDirectory[baseDirectory.Length - 1] == '\\');
                bool dbFilePathHasBackSlash = (length < path.Length) && (path[length] == '\\');
                // piece the filepath back together correctly!
                if (!baseDirectoryHasBackSlash && !dbFilePathHasBackSlash)
                    return baseDirectory + '\\' + path.Substring(length);
                if (baseDirectoryHasBackSlash && dbFilePathHasBackSlash)
                    return baseDirectory + path.Substring(length + 1);
                return baseDirectory + path.Substring(length);
            }

            // simply return what was passed in
            return path;
        }

        private void InitializeDatabase()
        {
            string connectionString = ConnectionString;
            Debug.AssertStringNotEmpty(connectionString);

            VistaDBConnectionStringBuilder builder = new VistaDBConnectionStringBuilder(connectionString);

            // get the supplied data source
            string dbFilePath = ResolveDataSourceFilePath(builder.DataSource);

            if (File.Exists(dbFilePath))
                return;

            using (VistaDBConnection connection = new VistaDBConnection())
            using (VistaDBCommand command = connection.CreateCommand())
            {
                string passwordClause = string.Empty;
                if (!string.IsNullOrEmpty(builder.Password))
                    passwordClause = " PASSWORD '" + EscapeApostrophes(builder.Password) + "',";

                // create the database using the webserver's default locale
                command.CommandText = "CREATE DATABASE '" + EscapeApostrophes(dbFilePath) + "'" + passwordClause + ", PAGE SIZE 1, CASE SENSITIVE FALSE;";
                command.ExecuteNonQuery();

                command.CommandText = @"
                    CREATE TABLE [ELMAH_Error]
                    (
                        [ErrorId] UNIQUEIDENTIFIER NOT NULL DEFAULT '(newid())',
                        [Application] NVARCHAR (60) NOT NULL,
                        [Host] NVARCHAR (50) NOT NULL,
                        [Type] NVARCHAR (100) NOT NULL,
                        [Source] NVARCHAR (60) NOT NULL,
                        [Message] NVARCHAR (500) NOT NULL,
                        [User] NVARCHAR (50) NOT NULL,
                        [StatusCode] INT NOT NULL,
                        [TimeUtc] DATETIME NOT NULL,
                        [Sequence] INT NOT NULL,
                        [AllXml] NTEXT NOT NULL,
                        CONSTRAINT [PK_ELMAH_Error] PRIMARY KEY ([ErrorId])
                    )";
                command.ExecuteNonQuery();

                command.CommandText = @"
                    ALTER TABLE [ELMAH_Error]
                    ALTER COLUMN [Sequence] INT NOT NULL IDENTITY (1, 1)";
                command.ExecuteNonQuery();

                command.CommandText = "CREATE INDEX [IX_ELMAH_Error_App_Time_Seq] ON [ELMAH_Error] ([TimeUtc] DESC, [Sequence] DESC)";
                command.ExecuteNonQuery();

                command.CommandText = @"
                    CREATE PROCEDURE [ELMAH_GetErrorXml]
                        @ErrorId        UniqueIdentifier
                    AS
                    BEGIN
                        SELECT  AllXml
                        FROM    ELMAH_Error
                        WHERE   ErrorId = @ErrorId;
                    END";
                command.ExecuteNonQuery();

                command.CommandText = @"
                    CREATE PROCEDURE [ELMAH_LogError]
                        @ErrorId        UniqueIdentifier,
                        @Application    NVarChar,
                        @Host           NVarChar,
                        @Type           NVarChar,
                        @Source         NVarChar,
                        @Message        NVarChar,
                        @User           NVarChar,
                        @AllXml         NText,
                        @StatusCode     Int,
                        @TimeUtc        DateTime
                    AS
                    BEGIN
                    INSERT
                    INTO
                        ELMAH_Error
                        (
                            ErrorId,
                            Application,
                            Host,
                            Type,
                            Source,
                            Message,
                            [User],
                            AllXml,
                            StatusCode,
                            TimeUtc
                        )
                    VALUES
                        (
                            @ErrorId,
                            @Application,
                            @Host,
                            @Type,
                            @Source,
                            @Message,
                            @User,
                            @AllXml,
                            @StatusCode,
                            @TimeUtc
                        );
                    END";
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Gets the connection string from the given configuration.
        /// </summary>

        private static string GetConnectionString(IDictionary config)
        {
            Debug.Assert(config != null);

            //
            // First look for a connection string name that can be 
            // subsequently indexed into the <connectionStrings> section of 
            // the configuration to get the actual connection string.
            //

            string connectionStringName = (string)config["connectionStringName"] ?? string.Empty;

            if (connectionStringName.Length > 0)
            {
                ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[connectionStringName];

                if (settings == null)
                    return string.Empty;

                return settings.ConnectionString ?? string.Empty;
            }

            //
            // Connection string name not found so see if a connection 
            // string was given directly.
            //

            string connectionString = Mask.NullString((string)config["connectionString"]);

            if (connectionString.Length > 0)
                return connectionString;

            //
            // As a last resort, check for another setting called 
            // connectionStringAppKey. The specifies the key in 
            // <appSettings> that contains the actual connection string to 
            // be used.
            //

            string connectionStringAppKey = Mask.NullString((string)config["connectionStringAppKey"]);

            if (connectionStringAppKey.Length == 0)
                return string.Empty;

            return Configuration.AppSettings[connectionStringAppKey];
        }
    }
}

#endif //!NET_1_1 && !NET_1_0
