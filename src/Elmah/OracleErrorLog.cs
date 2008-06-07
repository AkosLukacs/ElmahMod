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

#if !NET_1_0

[assembly: Elmah.Scc("$Id$")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Data;
    using System.Data.OracleClient;
    using System.IO;
    using System.Text;

    using IDictionary = System.Collections.IDictionary;
    using IList = System.Collections.IList;

    #endregion

    /// <summary>
    /// An <see cref="ErrorLog"/> implementation that uses Oracle as its backing store.
    /// </summary>

    public class OracleErrorLog : ErrorLog
    {
        private readonly string _connectionString;

        private const int _maxAppNameLength = 60;

        /// <summary>
        /// Initializes a new instance of the <see cref="OracleErrorLog"/> class
        /// using a dictionary of configured settings.
        /// </summary>

        public OracleErrorLog(IDictionary config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            string connectionString = ConnectionStringHelper.GetConnectionString(config);

            //
            // If there is no connection string to use then throw an 
            // exception to abort construction.
            //

            if (connectionString.Length == 0)
                throw new ApplicationException("Connection string is missing for the Oracle error log.");

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
        /// Initializes a new instance of the <see cref="OracleErrorLog"/> class
        /// to use a specific connection string for connecting to the database.
        /// </summary>

        public OracleErrorLog(string connectionString)
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
            get { return "Oracle Error Log"; }
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
            Guid id = Guid.NewGuid();

            using (OracleConnection connection = new OracleConnection(this.ConnectionString))
            using (OracleCommand command = connection.CreateCommand())
            {
                connection.Open();
                using (OracleTransaction transaction = connection.BeginTransaction())
                {
                    // because we are storing the XML data in a NClob, we need to jump through a few hoops!!
                    // so first we've got to operate within a transaction
                    command.Transaction = transaction;

                    // then we need to create a temporary lob on the database server
                    command.CommandText = "declare xx nclob; begin dbms_lob.createtemporary(xx, false, 0); :tempblob := xx; end;";
                    command.CommandType = CommandType.Text;

                    OracleParameterCollection parameters = command.Parameters;
                    parameters.Add("tempblob", OracleType.NClob).Direction = ParameterDirection.Output;
                    command.ExecuteNonQuery();

                    // now we can get a handle to the NClob
                    OracleLob xmlLob = (OracleLob)parameters[0].Value;
                    // create a temporary buffer in which to store the XML
                    byte[] tempbuff = Encoding.Unicode.GetBytes(errorXml);
                    // and finally we can write to it!
                    xmlLob.BeginBatch(OracleLobOpenMode.ReadWrite);
                    xmlLob.Write(tempbuff,0,tempbuff.Length);
                    xmlLob.EndBatch();

                    command.CommandText = "pkg_elmah$error.LogError";
                    command.CommandType = CommandType.StoredProcedure;

                    parameters.Clear();
                    parameters.Add("v_ErrorId", OracleType.NVarChar, 32).Value = id.ToString("N");
                    parameters.Add("v_Application", OracleType.NVarChar, _maxAppNameLength).Value = ApplicationName;
                    parameters.Add("v_Host", OracleType.NVarChar, 30).Value = error.HostName;
                    parameters.Add("v_Type", OracleType.NVarChar, 100).Value = error.Type;
                    parameters.Add("v_Source", OracleType.NVarChar, 60).Value = error.Source;
                    parameters.Add("v_Message", OracleType.NVarChar, 500).Value = error.Message;
                    parameters.Add("v_User", OracleType.NVarChar, 50).Value = error.User;
                    parameters.Add("v_AllXml", OracleType.NClob).Value = xmlLob;
                    parameters.Add("v_StatusCode", OracleType.Int32).Value = error.StatusCode;
                    parameters.Add("v_TimeUtc", OracleType.DateTime).Value = error.Time.ToUniversalTime();

                    command.ExecuteNonQuery();
                    transaction.Commit();
                }
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

            using (OracleConnection connection = new OracleConnection(this.ConnectionString))
            using (OracleCommand command = connection.CreateCommand())
            {
                command.CommandText = "pkg_elmah$error.GetErrorsXml";
                command.CommandType = CommandType.StoredProcedure;

                OracleParameterCollection parameters = command.Parameters;

                parameters.Add("v_Application", OracleType.NVarChar, _maxAppNameLength).Value = ApplicationName;
                parameters.Add("v_PageIndex", OracleType.Int32).Value = pageIndex;
                parameters.Add("v_PageSize", OracleType.Int32).Value = pageSize;
                parameters.Add("v_TotalCount", OracleType.Int32).Direction = ParameterDirection.Output;
                parameters.Add("v_Results", OracleType.Cursor).Direction = ParameterDirection.Output;

                connection.Open();

                using (OracleDataReader reader = command.ExecuteReader())
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

                return (int)command.Parameters["v_TotalCount"].Value;
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

            string errorXml;

            using (OracleConnection connection = new OracleConnection(this.ConnectionString))
            using (OracleCommand command = connection.CreateCommand())
            {
                command.CommandText = "pkg_elmah$error.GetErrorXml";
                command.CommandType = CommandType.StoredProcedure;

                OracleParameterCollection parameters = command.Parameters;
                parameters.Add("v_Application", OracleType.NVarChar, _maxAppNameLength).Value = ApplicationName;
                parameters.Add("v_ErrorId", OracleType.NVarChar, 32).Value = errorGuid.ToString("N");
                parameters.Add("v_AllXml", OracleType.NClob).Direction = ParameterDirection.Output;

                connection.Open();
                command.ExecuteNonQuery();
                OracleLob xmlLob = (OracleLob)command.Parameters["v_AllXml"].Value;

                StreamReader streamreader = new StreamReader(xmlLob, Encoding.Unicode);
                char[] cbuffer = new char[1000];
                int actual;
                StringBuilder sb = new StringBuilder();
                while((actual = streamreader.Read(cbuffer, 0, cbuffer.Length)) >0)
                    sb.Append(cbuffer, 0, actual);
                errorXml = sb.ToString();
            }

            if (errorXml == null)
                return null;

            Error error = NewError();
            error.FromString(errorXml);

            return new ErrorLogEntry(this, id, error);
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

#endif //!NET_1_0
