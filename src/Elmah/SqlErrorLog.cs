#region License, Terms and Author(s)
//
// ELMAH - Error Logging Modules and Handlers for ASP.NET
// Copyright (c) 2007 Atif Aziz. All rights reserved.
//
//  Author(s):
//
//      Atif Aziz, http://www.raboof.com
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

#if !NET_1_0 && !NET_1_1
#define ASYNC_ADONET
#endif

[assembly: Elmah.Scc("$Id$")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Configuration;
    using System.Data;
    using System.Data.SqlClient;
    using System.Threading;
    using System.Xml;

    using IDictionary = System.Collections.IDictionary;
    using StringReader = System.IO.StringReader;
    using StringWriter = System.IO.StringWriter;
    using IList = System.Collections.IList;

    #endregion

    /// <summary>
    /// An <see cref="ErrorLog"/> implementation that uses Microsoft SQL 
    /// Server 2000 as its backing store.
    /// </summary>
    
    public class SqlErrorLog : ErrorLog
    {
        private readonly string _connectionString;

#if ASYNC_ADONET
        private delegate RV Function<RV, A>(A a);
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlErrorLog"/> class
        /// using a dictionary of configured settings.
        /// </summary>

        public SqlErrorLog(IDictionary config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            _connectionString = GetConnectionString(config);

            //
            // If there is no connection string to use then throw an 
            // exception to abort construction.
            //

            if (_connectionString.Length == 0)
                throw new ApplicationException("Connection string is missing for the SQL error log.");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlErrorLog"/> class
        /// to use a specific connection string for connecting to the database.
        /// </summary>

        public SqlErrorLog(string connectionString)
        {
            if (connectionString == null)
                throw new ArgumentNullException("connectionString");

            if (connectionString.Length == 0)
                throw new ArgumentOutOfRangeException("connectionString");
            
            _connectionString = connectionString;
        }

        /// <summary>
        /// Gets the name of this error log implementation.
        /// </summary>
        
        public override string Name
        {
            get { return "Microsoft SQL Server Error Log"; }
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

#if NET_2_0 // FIXME NET_2_0 -> !NET_1_0 && !NET_1_1
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.NewLineOnAttributes = true;
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

            string errorXml = sw.ToString();
            Guid id = Guid.NewGuid();

            using (SqlConnection connection = new SqlConnection(this.ConnectionString))
            using (SqlCommand command = Commands.LogError(
                id, this.ApplicationName, 
                error.HostName, error.Type, error.Source, error.Message, error.User,
                error.StatusCode, error.Time.ToUniversalTime(), errorXml))
            {
                command.Connection = connection;
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

            using (SqlConnection connection = new SqlConnection(this.ConnectionString))
            using (SqlCommand command = Commands.GetErrorsXml(this.ApplicationName, pageIndex, pageSize))
            {
                command.Connection = connection;
                connection.Open();

                XmlReader reader = command.ExecuteXmlReader();
                
                try
                {
                    ErrorsXmlToList(reader, errorEntryList);
                }
                finally
                {
                    reader.Close();
                }
                
                int total;
                Commands.GetErrorsXmlOutputs(command, out total);
                return total;
            }
        }

#if ASYNC_ADONET

        /// <summary>
        /// Begins an asynchronous version of <see cref="GetErrors"/>.
        /// </summary>

        public override IAsyncResult BeginGetErrors(int pageIndex, int pageSize, IList errorEntryList,
            AsyncCallback asyncCallback, object asyncState)
        {
            if (pageIndex < 0)
                throw new ArgumentOutOfRangeException("pageIndex");

            if (pageSize < 0)
                throw new ArgumentOutOfRangeException("pageSize");

            //
            // Modify the connection string on the fly to support async 
            // processing otherwise the asynchronous methods on the
            // SqlCommand will throw an exception. This ensures the
            // right behavior regardless of whether configured
            // connection string sets the Async option to true or not.
            //

            SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(this.ConnectionString);
            csb.AsynchronousProcessing = true;
            SqlConnection connection = new SqlConnection(csb.ConnectionString);

            //
            // Create the command object with input parameters initialized
            // and setup to call the stored procedure.
            //

            SqlCommand command = Commands.GetErrorsXml(this.ApplicationName, pageIndex, pageSize);
            command.Connection = connection;

            //
            // Create a closure to handle the ending of the async operation
            // and retrieve results.
            //

            AsyncResultWrapper asyncResult = null;

            Function<int, IAsyncResult> endHandler = delegate
            {
                Debug.Assert(asyncResult != null);

                using (connection)
                using (command)
                {
                    using (XmlReader reader = command.EndExecuteXmlReader(asyncResult.InnerResult))
                        ErrorsXmlToList(reader, errorEntryList);

                    int total;
                    Commands.GetErrorsXmlOutputs(command, out total);
                    return total;
                }
            };

            //
            // Open the connenction and execute the command asynchronously,
            // returning an IAsyncResult that wrap the downstream one. This
            // is needed to be able to send our own AsyncState object to
            // the downstream IAsyncResult object. In order to preserve the
            // one sent by caller, we need to maintain and return it from
            // our wrapper.
            //

            try
            {
                connection.Open();

                asyncResult = new AsyncResultWrapper(
                    command.BeginExecuteXmlReader(
                        asyncCallback != null ? /* thunk */ delegate { asyncCallback(asyncResult); } : (AsyncCallback) null, 
                        endHandler), asyncState);

                return asyncResult;
            }
            catch (Exception)
            {
                connection.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Ends an asynchronous version of <see cref="ErrorLog.GetErrors"/>.
        /// </summary>

        public override int EndGetErrors(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException("asyncResult");

            AsyncResultWrapper wrapper = asyncResult as AsyncResultWrapper;

            if (wrapper == null)
                throw new ArgumentException("Unexepcted IAsyncResult type.", "asyncResult");

            Function<int, IAsyncResult> endHandler = (Function<int, IAsyncResult>) wrapper.InnerResult.AsyncState;
            return endHandler(wrapper.InnerResult);
        }

#endif

        private void ErrorsXmlToList(XmlReader reader, IList errorEntryList)
        {
            Debug.Assert(reader != null);
            Debug.Assert(errorEntryList != null);

            while (reader.IsStartElement("error"))
            {
                string id = reader.GetAttribute("errorId");

                Error error = NewError();
                error.FromXml(reader);

                if (errorEntryList != null)
                    errorEntryList.Add(new ErrorLogEntry(this, id, error));
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

            string errorXml;

            using (SqlConnection connection = new SqlConnection(this.ConnectionString))
            using (SqlCommand command = Commands.GetErrorXml(this.ApplicationName, errorGuid))
            {
                command.Connection = connection;
                connection.Open();
                errorXml = (string) command.ExecuteScalar();
            }

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

            string connectionStringName = (string) config["connectionStringName"] ?? string.Empty;

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

        private sealed class Commands
        {
            private Commands() {}

            public static SqlCommand LogError(
                Guid id,
                string appName,
                string hostName,
                string typeName,
                string source,
                string message,
                string user,
                int statusCode,
                DateTime time,
                string xml)
            {
                SqlCommand command = new SqlCommand("ELMAH_LogError");
                command.CommandType = CommandType.StoredProcedure;

                SqlParameterCollection parameters = command.Parameters;

                parameters.Add("@ErrorId", SqlDbType.UniqueIdentifier).Value = id;
                parameters.Add("@Application", SqlDbType.NVarChar, 60).Value = appName;
                parameters.Add("@Host", SqlDbType.NVarChar, 30).Value = hostName;
                parameters.Add("@Type", SqlDbType.NVarChar, 100).Value = typeName;
                parameters.Add("@Source", SqlDbType.NVarChar, 60).Value = source;
                parameters.Add("@Message", SqlDbType.NVarChar, 500).Value = message;
                parameters.Add("@User", SqlDbType.NVarChar, 50).Value = user;
                parameters.Add("@AllXml", SqlDbType.NText).Value = xml;
                parameters.Add("@StatusCode", SqlDbType.Int).Value = statusCode;
                parameters.Add("@TimeUtc", SqlDbType.DateTime).Value = time.ToUniversalTime();

                return command;
            }

            public static SqlCommand GetErrorXml(string appName, Guid id)
            {
                SqlCommand command = new SqlCommand("ELMAH_GetErrorXml");
                command.CommandType = CommandType.StoredProcedure;

                SqlParameterCollection parameters = command.Parameters;
                parameters.Add("@Application", SqlDbType.NVarChar, 60).Value = appName;
                parameters.Add("@ErrorId", SqlDbType.UniqueIdentifier).Value = id;

                return command;
            }

            public static SqlCommand GetErrorsXml(string appName, int pageIndex, int pageSize)
            {
                SqlCommand command = new SqlCommand("ELMAH_GetErrorsXml");
                command.CommandType = CommandType.StoredProcedure;

                SqlParameterCollection parameters = command.Parameters;

                parameters.Add("@Application", SqlDbType.NVarChar, 60).Value = appName;
                parameters.Add("@PageIndex", SqlDbType.Int).Value = pageIndex;
                parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;
                parameters.Add("@TotalCount", SqlDbType.Int).Direction = ParameterDirection.Output;

                return command;
            }

            public static void GetErrorsXmlOutputs(SqlCommand command, out int totalCount)
            {
                Debug.Assert(command != null);

                totalCount = (int) command.Parameters["@TotalCount"].Value;
            }
        }

        private sealed class AsyncResultWrapper : IAsyncResult
        {
            private readonly IAsyncResult _inner;
            private readonly object _asyncState;

            public AsyncResultWrapper(IAsyncResult inner, object asyncState)
            {
                _inner = inner;
                _asyncState = asyncState;
            }

            public IAsyncResult InnerResult
            {
                get { return _inner; }
            }

            public bool IsCompleted
            {
                get { return _inner.IsCompleted; }
            }

            public WaitHandle AsyncWaitHandle
            {
                get { return _inner.AsyncWaitHandle; }
            }

            public object AsyncState
            {
                get { return _asyncState; }
            }

            public bool CompletedSynchronously
            {
                get { return _inner.CompletedSynchronously; }
            }
        }
    }
}
