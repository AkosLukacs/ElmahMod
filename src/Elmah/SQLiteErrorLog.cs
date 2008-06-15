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
    using System.Data;
    using System.Data.SQLite;
    using System.Globalization;
    using System.IO;

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

            string connectionString = ConnectionStringHelper.GetConnectionString(config, true);

            //
            // If there is no connection string to use then throw an 
            // exception to abort construction.
            //

            if (connectionString.Length == 0)
                throw new ApplicationException("Connection string is missing for the SQLite error log.");

            _connectionString = connectionString;

            InitializeDatabase();

            ApplicationName = Mask.NullString((string) config["applicationName"]);
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
                throw new ArgumentException(null, "connectionString");

            _connectionString = ConnectionStringHelper.GetResolvedConnectionString(connectionString);

            InitializeDatabase();
        }

        private static readonly object _lock = new object();
        private void InitializeDatabase()
        {
            string connectionString = ConnectionString;
            Debug.AssertStringNotEmpty(connectionString);

            string dbFilePath = ConnectionStringHelper.GetDataSourceFilePath(connectionString);

            if (File.Exists(dbFilePath))
                return;

            //
            // Make sure that we don't have multiple threads all trying to create the database
            //

            lock (_lock)
            {
                //
                // Just double check that no other thread has created the database while
                // we were waiting for the lock
                //

                if (File.Exists(dbFilePath))
                    return;

                SQLiteConnection.CreateFile(dbFilePath);

                const string sql = @"
                CREATE TABLE Error (
                    ErrorId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
                    Application TEXT NOT NULL,
                    Host TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    Source TEXT NOT NULL,
                    Message TEXT NOT NULL,
                    User TEXT NOT NULL,
                    StatusCode INTEGER NOT NULL,
                    TimeUtc TEXT NOT NULL,
                    AllXml TEXT NOT NULL
                )";

                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                using (SQLiteCommand command = new SQLiteCommand(sql, connection))
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
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

            string errorXml = error.ToXmlString();

            const string query = @"
                INSERT INTO Error (
                    Application, Host, 
                    Type, Source, Message, User, StatusCode, 
                    TimeUtc, AllXml)
                VALUES (
                    @Application, @Host, 
                    @Type, @Source, @Message, @User, @StatusCode, 
                    @TimeUtc, @AllXml);

                SELECT last_insert_rowid();";

            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            using (SQLiteCommand command = new SQLiteCommand(query, connection))
            {
                SQLiteParameterCollection parameters = command.Parameters;

                parameters.Add("@Application", DbType.String, 60).Value = ApplicationName;
                parameters.Add("@Host", DbType.String, 30).Value = error.HostName;
                parameters.Add("@Type", DbType.String, 100).Value = error.Type;
                parameters.Add("@Source", DbType.String, 60).Value = error.Source;
                parameters.Add("@Message", DbType.String, 500).Value = error.Message;
                parameters.Add("@User", DbType.String, 50).Value = error.User;
                parameters.Add("@StatusCode", DbType.Int64).Value = error.StatusCode;
                parameters.Add("@TimeUtc", DbType.DateTime).Value = error.Time.ToUniversalTime();
                parameters.Add("@AllXml", DbType.String).Value = errorXml;

                connection.Open();

                return Convert.ToInt64(command.ExecuteScalar()).ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Returns a page of errors from the databse in descending order 
        /// of logged time.
        /// </summary>
        public override int GetErrors(int pageIndex, int pageSize, IList errorEntryList)
        {
            if (pageIndex < 0)
                throw new ArgumentOutOfRangeException("pageIndex", pageIndex, null);

            if (pageSize < 0)
                throw new ArgumentOutOfRangeException("pageSize", pageSize, null);

            const string sql = @"
                SELECT
                    ErrorId,
                    Application,
                    Host,
                    Type,
                    Source,
                    Message,
                    User,
                    StatusCode,
                    TimeUtc
                FROM
                    Error
                ORDER BY
                    ErrorId DESC
                LIMIT 
                    @PageIndex * @PageSize,
                    @PageSize;

                SELECT COUNT(*) FROM Error";

            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            using (SQLiteCommand command = new SQLiteCommand(sql, connection))
            {
                SQLiteParameterCollection parameters = command.Parameters;

                parameters.Add("@PageIndex", DbType.Int16).Value = pageIndex;
                parameters.Add("@PageSize", DbType.Int16).Value = pageSize;

                connection.Open();

                using (SQLiteDataReader reader = command.ExecuteReader())
                {
                    if (errorEntryList != null)
                    {
                        while (reader.Read())
                        {
                            string id = reader["ErrorId"].ToString();

                            Error error = NewError();

                            error.ApplicationName = reader["Application"].ToString();
                            error.HostName = reader["Host"].ToString();
                            error.Type = reader["Type"].ToString();
                            error.Source = reader["Source"].ToString();
                            error.Message = reader["Message"].ToString();
                            error.User = reader["User"].ToString();
                            error.StatusCode = Convert.ToInt32(reader["StatusCode"]);
                            error.Time = Convert.ToDateTime(reader["TimeUtc"]).ToLocalTime();

                            errorEntryList.Add(new ErrorLogEntry(this, id, error));
                        }
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
                throw new ArgumentException(null, "id");

            long key;
            
            try
            {
                key = long.Parse(id, CultureInfo.InvariantCulture);
            }
            catch (FormatException e)
            {
                throw new ArgumentException(e.Message, "id", e);
            }

            const string sql = @"
                SELECT 
                    AllXml
                FROM 
                    Error
                WHERE
                    ErrorId = @ErrorId";

            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            using (SQLiteCommand command = new SQLiteCommand(sql, connection))
            {
                SQLiteParameterCollection parameters = command.Parameters;
                parameters.Add("@ErrorId", DbType.Int64).Value = key;

                connection.Open();

                string errorXml = (string) command.ExecuteScalar();

                if (errorXml == null)
                    return null;

                Error error = NewError();
                error.FromString(errorXml);
                return new ErrorLogEntry(this, id, error);
            }
        }

        /// <summary>
        /// Creates a new and empty instance of the <see cref="Error"/> class.
        /// </summary>

        protected virtual Error NewError()
        {
            return new Error();
        }
    }
}

#endif //!NET_1_1 && !NET_1_0
