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

namespace Elmah
{
    #region Imports

    using System;
    using System.Data;
    using System.Data.SqlClient;

    using IDictionary = System.Collections.IDictionary;
    using ConfigurationSettings = System.Configuration.ConfigurationSettings;
    using StringReader = System.IO.StringReader;
    using StringWriter = System.IO.StringWriter;
    using XmlTextReader = System.Xml.XmlTextReader;
    using XmlTextWriter = System.Xml.XmlTextWriter;
    using Formatting = System.Xml.Formatting;
    using XmlReader = System.Xml.XmlReader;
    using IList = System.Collections.IList;

    #endregion

    /// <summary>
    /// An <see cref="ErrorLog"/> implementation that uses Microsoft SQL 
    /// Server 2000 as its backing store.
    /// </summary>
    
    public class SqlErrorLog : ErrorLog
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlErrorLog"/> class
        /// using a dictionary of configured settings.
        /// </summary>

        public SqlErrorLog(IDictionary config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            //
            // Get the connection string. If it is empty, then check for
            // another setting called connectionStringAppKey. The latter
            // specifies the key in appSettings that contains the actual
            // connection string to be used.
            //

            _connectionString = StringEtc.MaskNull((string) config["connectionString"]);

            if (_connectionString.Length == 0)
            {
                string connectionStringAppKey = StringEtc.MaskNull((string) config["connectionStringAppKey"]);

                if (connectionStringAppKey.Length != 0)
                {
                    _connectionString = ConfigurationSettings.AppSettings[connectionStringAppKey];
                }
            }

            //
            // If there is no connection string to use then throw an 
            // exception to abort construction.
            //

            if (_connectionString.Length == 0)
            {
                throw new ApplicationException("Connection string is missing for the SQL error log.");
            }
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

            StringWriter errorStringWriter = new StringWriter();
            XmlTextWriter errorXmlWriter = new XmlTextWriter(errorStringWriter);
            errorXmlWriter.Formatting = Formatting.Indented;

            errorXmlWriter.WriteStartElement("error");
            error.ToXml(errorXmlWriter);
            errorXmlWriter.WriteEndElement();
            errorXmlWriter.Flush();
            
            string errorXml = errorStringWriter.ToString();

            using (SqlConnection connection = new SqlConnection(this.ConnectionString))
            using (SqlCommand command = new SqlCommand("ELMAH_LogError", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                SqlParameter parameter;

                Guid id = Guid.NewGuid();

                parameter = command.Parameters.Add("@ErrorId", SqlDbType.UniqueIdentifier);
                parameter.Value = id;

                parameter = command.Parameters.Add("@Application", SqlDbType.NVarChar, 60);
                parameter.Value = this.ApplicationName;
                
                parameter = command.Parameters.Add("@Host", SqlDbType.NVarChar, 30);
                parameter.Value = error.HostName;

                parameter = command.Parameters.Add("@Type", SqlDbType.NVarChar, 100);
                parameter.Value = error.Type;
                
                parameter = command.Parameters.Add("@Source", SqlDbType.NVarChar, 60);
                parameter.Value = error.Source;

                parameter = command.Parameters.Add("@Message", SqlDbType.NVarChar, 500);
                parameter.Value = error.Message;
                
                parameter = command.Parameters.Add("@User", SqlDbType.NVarChar, 50);
                parameter.Value = error.User;

                parameter = command.Parameters.Add("@AllXml", SqlDbType.NText);
                parameter.Value = errorXml;

                parameter = command.Parameters.Add("@StatusCode", SqlDbType.Int);
                parameter.Value = error.StatusCode;
                
                parameter = command.Parameters.Add("@TimeUtc", SqlDbType.DateTime);
                parameter.Value = error.Time.ToUniversalTime();

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
                throw new ArgumentOutOfRangeException("pageSite");

            using (SqlConnection connection = new SqlConnection(this.ConnectionString))
            using (SqlCommand command = new SqlCommand("ELMAH_GetErrorsXml", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                SqlParameter parameter;

                parameter = command.Parameters.Add("@Application", SqlDbType.NVarChar, 60);
                parameter.Value = this.ApplicationName;

                parameter = command.Parameters.Add("@PageIndex", SqlDbType.Int);
                parameter.Value = pageIndex;
                
                parameter = command.Parameters.Add("@PageSize", SqlDbType.Int);
                parameter.Value = pageSize;

                SqlParameter totalParameter = command.Parameters.Add("@TotalCount", SqlDbType.Int);
                totalParameter.Direction = ParameterDirection.Output;

                connection.Open();

                XmlReader reader = command.ExecuteXmlReader();

                try
                {
                    while (reader.IsStartElement("error"))
                    {
                        string id = reader.GetAttribute("errorId");
                        
                        Error error = NewError();
                        error.FromXml(reader);

                        if (errorEntryList != null)
                        {
                            errorEntryList.Add(new ErrorLogEntry(this, id, error));
                        }
                    }
                }
                finally
                {
                    reader.Close();
                }

                return (int) totalParameter.Value;
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

            using (SqlConnection connection = new SqlConnection(this.ConnectionString))
            using (SqlCommand command = new SqlCommand("ELMAH_GetErrorXml", connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                SqlParameter parameter;

                parameter = command.Parameters.Add("@Application", SqlDbType.NVarChar, 60);
                parameter.Value = this.ApplicationName;

                parameter = command.Parameters.Add("@ErrorId", SqlDbType.UniqueIdentifier);
                parameter.Value = errorGuid;
                
                connection.Open();

                string errorXml = (string) command.ExecuteScalar();

                StringReader errorStringReader = new StringReader(errorXml);
                XmlTextReader errorXmlReader = new XmlTextReader(errorStringReader);

                if (!errorXmlReader.IsStartElement("error"))
                {
                    throw new ApplicationException("The error XML is not in the expected format.");
                }

                Error error = NewError();
                error.FromXml(errorXmlReader);

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
