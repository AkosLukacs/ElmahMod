#region License, Terms and Author(s)
//
// ELMAH - Error Logging Modules and Handlers for ASP.NET
// Copyright (c) 2007 Atif Aziz. All rights reserved.
//
//  Author(s):
//
//      Scott Wilson <sw@scratchstudio.net>
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
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Collections;

    #endregion

    /// <summary>
    /// An <see cref="ErrorLog"/> implementation that uses XML files stored on 
    /// disk as its backing store.
    /// </summary>

    public class XmlFileErrorLog : ErrorLog
    {
        private string _logPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlFileErrorLog"/> class
        /// using a dictionary of configured settings.
        /// </summary>
        
        public XmlFileErrorLog(IDictionary config)
        {
            _logPath = Mask.NullString(config["LogPath"] as string);
            
            if (_logPath.Length == 0)
                throw new ApplicationException("Log path is missing for the XML file-based error log.");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlFileErrorLog"/> class
        /// to use a specific path to store/load XML files.
        /// </summary>
        
        public XmlFileErrorLog(string logPath)
        {
            _logPath = logPath;
        }
        
        /// <summary>
        /// Gets the path to where the log is stored.
        /// </summary>
        
        public virtual string LogPath
        {
            get { return Mask.NullString(_logPath); }
        }

        /// <summary>
        /// Gets the name of this error log implementation.
        /// </summary>
        
        public override string Name
        {
            get { return "XML File-Based Error Log"; }
        }

        /// <summary>
        /// Logs an error to the database.
        /// </summary>
        /// <remarks>
        /// Logs an error as a single XML file stored in a folder. XML files are named with a
        /// sortable date and a unique identifier. Currently the XML files are stored indefinately.
        /// As they are stored as files, they may be managed using standard scheduled jobs.
        /// </remarks>
        
        public override string Log(Error error)
        {
            string errorId = Guid.NewGuid().ToString();
            
            string timeStamp = DateTime.UtcNow.ToString("yyyy-MM-ddHHmmssZ", CultureInfo.InvariantCulture);
            string path = Path.Combine(LogPath, string.Format(@"error-{0}-{1}.xml", timeStamp, errorId));
            
            XmlTextWriter writer = new XmlTextWriter(path, Encoding.UTF8);

            try
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartElement("error");
                writer.WriteAttributeString("errorId", errorId);
                error.ToXml(writer);
                writer.WriteEndElement();
                writer.Flush();
            }
            finally
            {
                writer.Close();
            }                
            
            return errorId;
        }

        /// <summary>
        /// Returns a page of errors from the folder in descending order 
        /// of logged time as defined by the sortable filenames.
        /// </summary>
        
        public override int GetErrors(int pageIndex, int pageSize, IList errorEntryList)
        {
            if (pageIndex < 0)
                throw new ArgumentOutOfRangeException("pageIndex");

            if (pageSize < 0)
                throw new ArgumentOutOfRangeException("pageSize");

            /* Get the file list from the folder */
            string[] files = Directory.GetFiles(LogPath);

            if (files.Length < 1)
                return 0;

            Array.Sort(files, Comparer.DefaultInvariant);
            Array.Reverse(files);
            
            /* Find the proper page */
            int firstIndex = pageIndex * pageSize;
            int lastIndex = (firstIndex + pageSize < files.Length) ? firstIndex + pageSize : files.Length;

            /* Open them up and rehydrate the list */
            for (int i = firstIndex; i < lastIndex; i++)
            {
                XmlTextReader reader = new XmlTextReader(files[i]);

                try
                {
                    while (reader.IsStartElement("error"))
                    {
                        string id = reader.GetAttribute("errorId");
                        
                        Error error = new Error();
                        error.FromXml(reader);

                        if (errorEntryList != null)
                            errorEntryList.Add(new ErrorLogEntry(this, id, error));
                    } 
                }
                finally
                {
                    reader.Close();
                }

            }
    
            /* Return how many are total */
            return files.Length;
        }

        /// <summary>
        /// Returns the specified error from the filesystem, or throws an exception if it does not exist.
        /// </summary>
        
        public override ErrorLogEntry GetError(string id)
        {
            /* Make sure the identifier is a valid GUID */
            id = (new Guid(id)).ToString();

            /* Get the file folder list - should only return one ever */
            string[] files = Directory.GetFiles(LogPath, string.Format("error-{0}.xml", id));
            
            if (files.Length < 1)
                throw new System.ApplicationException(string.Format("Cannot locate error file for error with ID {0}.", id));

            XmlTextReader reader = new XmlTextReader(files[0]);
            
            try
            {
                Error error = new Error();
                error.FromXml(reader);
                return new ErrorLogEntry(this, id, error);
            }
            finally
            {
                reader.Close();
            }
        }
    }
}
