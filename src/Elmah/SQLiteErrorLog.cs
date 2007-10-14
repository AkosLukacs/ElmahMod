#region License, Terms and Author(s)

//
// ELMAH - Error Logging Modules and Handlers for ASP.NET
// Copyright (c) 2007 Atif Aziz. All rights reserved.
//
//  Author(s):
//
//      Simone Busoli, mailto:simone.busoli@gmail.com
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
    using System.Collections;
    using System.Configuration;
    using System.Data;
    using System.Data.SQLite;
    using System.IO;
    using System.Xml;

    #endregion

    /// <summary>
    /// An <see cref="ErrorLog"/> implementation that uses SQLite as its backing store.
    /// </summary>

    public class SQLiteErrorLog : ErrorLog
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLiteErrorLog"/> class
        /// using a dictionary of configured settings.
        /// </summary>
        
        public SQLiteErrorLog(IDictionary config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            string connectionString = GetConnectionString(config);

            //
            // If there is no connection string to use then throw an 
            // exception to abort construction.
            //

            if (connectionString.Length == 0)
                throw new ApplicationException("Connection string is missing for the SQLite error log.");

            _connectionString = connectionString;

            InitializeDatabase();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLiteErrorLog"/> class
        /// to use a specific connection string for connecting to the database.
        /// </summary>
        
        public SQLiteErrorLog(string connectionString)
        {
            if (connectionString == null)
                throw new ArgumentNullException("connectionString");

            if (connectionString.Length == 0)
                throw new ArgumentOutOfRangeException("connectionString");

            _connectionString = connectionString;

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            Debug.AssertStringNotEmpty(ConnectionString);

            SQLiteConnectionStringBuilder builder = new SQLiteConnectionStringBuilder(ConnectionString);

            string dbFilePath = builder.DataSource;

            if (File.Exists(dbFilePath))
                return;

            SQLiteConnection.CreateFile(dbFilePath);

            const string sql = @"
                CREATE TABLE ELMAH_Error (
                    ErrorId UNIQUEIDENTIFIER NOT NULL,
                    Host TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    Source TEXT NOT NULL,
                    Message TEXT NOT NULL,
                    User TEXT NOT NULL,
                    StatusCode INTEGER NOT NULL,
                    TimeUtc TEXT NOT NULL,
                    Sequence INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                    AllXml TEXT NOT NULL
                );

                CREATE UNIQUE INDEX ELMAH_Index on ELMAH_Error (ErrorId ASC);";

            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            using (SQLiteCommand command = new SQLiteCommand(sql, connection))
            {
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Gets the name of this error log implementation.
        /// </summary>
        
        public override string Name
        {
            get { return "SQLite Error Log"; }
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
            using (XmlWriter writer = XmlWriter.Create(sw, settings))
            {
                writer.WriteStartElement("error");
                error.ToXml(writer);
                writer.WriteEndElement();
                writer.Flush();
            }

            string errorXml = sw.ToString();

            const string query = @"
                INSERT INTO ELMAH_Error (
                    ErrorId, Host, 
                    Type, Source, Message, User, StatusCode, 
                    TimeUtc, AllXml)
                VALUES (
                    ?, ?, ?, ?, ?, ?, ?, ?, ?)";

            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            using (SQLiteCommand command = new SQLiteCommand(query, connection))
            {
                Guid id = Guid.NewGuid();

                SQLiteParameterCollection parameters = command.Parameters;

                parameters.Add("@ErrorId", DbType.Guid).Value = id;
                parameters.Add("@Host", DbType.String, 30).Value = error.HostName;
                parameters.Add("@Type", DbType.String, 100).Value = error.Type;
                parameters.Add("@Source", DbType.String, 60).Value = error.Source;
                parameters.Add("@Message", DbType.String, 500).Value = error.Message;
                parameters.Add("@User", DbType.String, 50).Value = error.User;
                parameters.Add("@StatusCode", DbType.Int64).Value = error.StatusCode;
                parameters.Add("@TimeUtc", DbType.DateTime).Value = error.Time.ToUniversalTime();
                parameters.Add("@AllXml", DbType.String).Value = errorXml;

                connection.Open();
                command.ExecuteNonQuery();

                return id.ToString();
            }
        }

        /// <summary>
        /// Returns a page of errors from the databse in descending order 
        /// of logged time.
        /// </summary>
        public override int GetErrors(int pageIndex, int pageSize, IList errorEntryList)
        {
            if (pageIndex < 0)
                throw new ArgumentOutOfRangeException("pageIndex");

            if (pageSize < 0)
                throw new ArgumentOutOfRangeException("pageSize");

            const string sql = @"
                
                SELECT
                    ErrorId,
                    Host,
                    Type,
                    Source,
                    Message,
                    User,
                    StatusCode,
                    TimeUtc
                FROM
                    ELMAH_Error
                ORDER BY
                    TimeUtc DESC,
                    Sequence DESC
                LIMIT 
                    @PageIndex * @PageSize,
                    @PageSize;

                SELECT COUNT(*) FROM ELMAH_Error;";

            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            using (SQLiteCommand command = new SQLiteCommand(sql, connection))
            {
                SQLiteParameterCollection parameters = command.Parameters;

                parameters.Add("@PageIndex", DbType.Int16).Value = pageIndex;
                parameters.Add("@PageSize", DbType.Int16).Value = pageSize;

                connection.Open();

                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string id = reader["ErrorId"].ToString();

                        Error error = NewError();

                        error.HostName = reader["Host"].ToString();
                        error.Type = reader["Type"].ToString();
                        error.Source = reader["Source"].ToString();
                        error.Message = reader["Message"].ToString();
                        error.User = reader["User"].ToString();
                        error.StatusCode = Convert.ToInt32(reader["StatusCode"]);
                        error.Time = Convert.ToDateTime(reader["TimeUtc"]).ToLocalTime();

                        if (errorEntryList != null)
                            errorEntryList.Add(new ErrorLogEntry(this, id, error));
                    }

                    //
                    // Get the result of SELECT COUNT(*) FROM Page
                    //

                    reader.NextResult();
                    reader.Read();
                    return reader.GetInt32(0);
                }
            }
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
                throw new ArgumentOutOfRangeException("id");

            Guid errorGuid;

            try
            {
                errorGuid = new Guid(id);
            }
            catch (FormatException e)
            {
                throw new ArgumentOutOfRangeException("id", id, e.Message);
            }

            const string sql =
                @"
                SELECT 
                    AllXml
                FROM 
                    ELMAH_Error
                WHERE
                    ErrorId = @ErrorId";

            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            using (SQLiteCommand command = new SQLiteCommand(sql, connection))
            {
                SQLiteParameterCollection parameters = command.Parameters;
                parameters.Add("@ErrorId", DbType.Guid).Value = errorGuid;

                connection.Open();

                string errorXml = (string) command.ExecuteScalar();

                using (XmlReader reader = XmlReader.Create(new StringReader(errorXml)))
                {
                    if (!reader.IsStartElement("error"))
                        throw new ApplicationException("The error XML is not in the expected format.");

                    Error error = NewError();
                    error.FromXml(reader);
                    return new ErrorLogEntry(this, id, error);
                }
            }
        }

        /// <summary>
        /// Creates a new and empty instance of the <see cref="Error"/> class.
        /// </summary>
        
        protected virtual Error NewError()
        {
            return new Error();
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

            string connectionStringName = (string) config["connectionStringName"] ?? string.Empty;

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

            string connectionString = Mask.NullString((string) config["connectionString"]);

            if (connectionString.Length > 0)
                return connectionString;

            //
            // As a last resort, check for another setting called 
            // connectionStringAppKey. The specifies the key in 
            // <appSettings> that contains the actual connection string to 
            // be used.
            //

            string connectionStringAppKey = Mask.NullString((string) config["connectionStringAppKey"]);

            if (connectionStringAppKey.Length == 0)
                return string.Empty;

            return Configuration.AppSettings[connectionStringAppKey];
        }
    }
}

#endif //!NET_1_1 && !NET_1_0
