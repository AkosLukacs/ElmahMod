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

[assembly: Elmah.Scc("$Id$")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Configuration;
    using System.Data;
    using System.Data.OleDb;
    using System.Diagnostics;
    using System.Text;
    using System.Xml;

    using IDictionary = System.Collections.IDictionary;
    using StringReader = System.IO.StringReader;
    using StringWriter = System.IO.StringWriter;
    using IList = System.Collections.IList;

    #endregion

    /// <summary>
    /// An <see cref="ErrorLog"/> implementation that uses Microsoft Access as its backing store.
    /// Use the supplied Elmah.mdb as the database.
    /// </summary>

    public class AccessErrorLog : ErrorLog
    {
        private readonly string _connectionString;

        private const int _maxAppNameLength = 60;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccessErrorLog"/> class
        /// using a dictionary of configured settings.
        /// </summary>

        public AccessErrorLog(IDictionary config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            string connectionString = GetConnectionString(config);

            //
            // If there is no connection string to use then throw an 
            // exception to abort construction.
            //

            if (connectionString.Length == 0)
                throw new ApplicationException("Connection string is missing for the Access error log.");

            _connectionString = connectionString;

            //
            // Set the application name as this implementation provides
            // per-application isolation over a single store.
            //

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
        /// Initializes a new instance of the <see cref="AccessErrorLog"/> class
        /// to use a specific connection string for connecting to the database.
        /// </summary>

        public AccessErrorLog(string connectionString)
        {
            if (connectionString == null)
                throw new ArgumentNullException("connectionString");

            if (connectionString.Length == 0)
                throw new ArgumentException(null, "connectionString");

            _connectionString = connectionString;
        }

        /// <summary>
        /// Gets the name of this error log implementation.
        /// </summary>

        public override string Name
        {
            get { return "Microsoft Access Error Log"; }
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

#if !NET_1_0 && !NET_1_1
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.NewLineOnAttributes = true;
            settings.CheckCharacters = false;
            XmlWriter writer = XmlWriter.Create(sw, settings);
#else
            XmlTextWriter writer = new XmlTextWriter(sw);
            writer.Formatting = Formatting.Indented;
#endif

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

            Guid id = Guid.NewGuid();
            string errorXml = sw.ToString();

            using (OleDbConnection connection = new OleDbConnection(this.ConnectionString))
            using (OleDbCommand command = connection.CreateCommand())
            {
                connection.Open();

                command.CommandText = "INSERT INTO Elmah_Error " + 
                                        "(ErrorId, Application, Host, Type, Source, Message, UserName, StatusCode, TimeUtc, AllXml) " + 
                                      "VALUES " + 
                                        "(@ErrorId, @Application, @Host, @Type, @Source, @Message, @UserName, @StatusCode, @TimeUtc, @AllXml)";
                command.CommandType = CommandType.Text;

                OleDbParameterCollection parameters = command.Parameters;

                parameters.Add("@ErrorId", OleDbType.VarChar, 32).Value = id.ToString("N");
                parameters.Add("@Application", OleDbType.VarChar, _maxAppNameLength).Value = ApplicationName;
                parameters.Add("@Host", OleDbType.VarChar, 30).Value = error.HostName;
                parameters.Add("@Type", OleDbType.VarChar, 100).Value = error.Type;
                parameters.Add("@Source", OleDbType.VarChar, 60).Value = error.Source;
                parameters.Add("@Message", OleDbType.LongVarChar, error.Message.Length).Value = error.Message;
                parameters.Add("@User", OleDbType.VarChar, 50).Value = error.User;
                parameters.Add("@StatusCode", OleDbType.Integer).Value = error.StatusCode;
                parameters.Add("@TimeUtc", OleDbType.Date).Value = error.Time.ToUniversalTime();
                parameters.Add("@AllXml", OleDbType.LongVarChar, errorXml.Length).Value = errorXml;
                
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
                throw new ArgumentOutOfRangeException("pageIndex", pageIndex, null);

            if (pageSize < 0)
                throw new ArgumentOutOfRangeException("pageSize", pageSize, null);

            using (OleDbConnection connection = new OleDbConnection(this.ConnectionString))
            using (OleDbCommand command = connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = "SELECT COUNT(*) FROM Elmah_Error WHERE Application = @Application";

                OleDbParameterCollection parameters = command.Parameters;
                parameters.Add("@Application", OleDbType.VarChar, _maxAppNameLength).Value = ApplicationName;

                connection.Open();
                int totalCount = (int)command.ExecuteScalar();

                int maxRecords = pageSize * (pageIndex + 1);
                if (maxRecords > totalCount)
                {
                    maxRecords = totalCount;
                    pageSize = totalCount - pageSize * (totalCount / pageSize);
                }

                StringBuilder sql = new StringBuilder(1000);
                sql.Append("SELECT e.* FROM (");
                sql.Append("SELECT TOP ");
                sql.Append(pageSize.ToString());
                sql.Append(" TimeUtc, SequenceNumber FROM (");
                sql.Append("SELECT TOP ");
                sql.Append(maxRecords.ToString());
                sql.Append(" TimeUtc, SequenceNumber FROM Elmah_Error ");
                sql.Append("WHERE Application = @Application");
                sql.Append("ORDER BY TimeUtc DESC, SequenceNumber DESC) ");
                sql.Append("ORDER BY TimeUtc ASC, SequenceNumber ASC) AS i ");
                sql.Append("INNER JOIN Elmah_Error AS e ON i.SequenceNumber = e.SequenceNumber ");
                sql.Append("ORDER BY e.TimeUtc DESC, e.SequenceNumber DESC");

                command.CommandText = sql.ToString();

                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    Debug.Assert(reader != null);

                    while (reader.Read())
                    {
                        string id = reader["ErrorId"].ToString();
                        Guid guid = new Guid(id);

                        Error error = NewError();

                        error.ApplicationName = reader["Application"].ToString();
                        error.HostName = reader["Host"].ToString();
                        error.Type = reader["Type"].ToString();
                        error.Source = reader["Source"].ToString();
                        error.Message = reader["Message"].ToString();
                        error.User = reader["UserName"].ToString();
                        error.StatusCode = Convert.ToInt32(reader["StatusCode"]);
                        error.Time = Convert.ToDateTime(reader["TimeUtc"]).ToLocalTime();

                        if (errorEntryList != null)
                            errorEntryList.Add(new ErrorLogEntry(this, guid.ToString(), error));
                    }

                    reader.Close();
                }

                return totalCount;
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

            Guid errorGuid;

            try
            {
                errorGuid = new Guid(id);
            }
            catch (FormatException e)
            {
                throw new ArgumentException(e.Message, "id", e);
            }

            string errorXml = null;

            using (OleDbConnection connection = new OleDbConnection(this.ConnectionString))
            using (OleDbCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT   AllXml " +
                                      "FROM     Elmah_Error " +
                                      "WHERE    Application = @Application " + 
                                      "AND      ErrorId = @ErrorId";
                command.CommandType = CommandType.Text;

                OleDbParameterCollection parameters = command.Parameters;
                parameters.Add("@Application", OleDbType.VarChar, _maxAppNameLength).Value = ApplicationName;
                parameters.Add("@ErrorId", OleDbType.VarChar, 32).Value = errorGuid.ToString("N");

                connection.Open();
                errorXml = (string)command.ExecuteScalar();
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

        /// <summary>
        /// Gets the connection string from the given configuration.
        /// </summary>

        private static string GetConnectionString(IDictionary config)
        {
            Debug.Assert(config != null);

#if !NET_1_1 && !NET_1_0
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
#endif

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
