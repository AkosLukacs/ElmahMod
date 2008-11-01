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
    using System.Runtime.CompilerServices;
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
        private readonly string _logPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlFileErrorLog"/> class
        /// using a dictionary of configured settings.
        /// </summary>
        
        public XmlFileErrorLog(IDictionary config)
        {
            string logPath = Mask.NullString(config["logPath"] as string);

            if (logPath.Length == 0)
            {
                //
                // For compatibility reasons with older version of this
                // implementation, we also try "LogPath".
                //

                logPath = Mask.NullString(config["LogPath"] as string);

                if (logPath.Length == 0)
                    throw new ApplicationException("Log path is missing for the XML file-based error log.");
            }

#if !NET_1_1 && !NET_1_0
            if (logPath.StartsWith("~/"))
                logPath = MapPath(logPath);
#endif

            _logPath = logPath;
        }

#if !NET_1_1 && !NET_1_0


        /// <remarks>
        /// This method is excluded from inlining so that if 
        /// HostingEnvironment does not need JIT-ing if it is not implicated
        /// by the caller.
        /// </remarks>

        [ MethodImpl(MethodImplOptions.NoInlining) ]
        private static string MapPath(string path) 
        {
            return System.Web.Hosting.HostingEnvironment.MapPath(path);
        }

#endif

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
                ErrorXml.Encode(error, writer);
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
                throw new ArgumentOutOfRangeException("pageIndex", pageIndex, null);

            if (pageSize < 0)
                throw new ArgumentOutOfRangeException("pageSize", pageSize, null);

            /* Get all files in directory */
            string logPath = LogPath;
            DirectoryInfo dir = new DirectoryInfo(logPath);
            FileSystemInfo[] infos = dir.GetFiles("error-*.xml");

            if (infos.Length < 1)
                return 0;

            string[] files = new string[infos.Length];
            int count = 0;

            /* Get files that are not marked with system and hidden attributes */
            foreach (FileSystemInfo info in infos)
            {
                if (IsUserFile(info.Attributes))
                    files[count++] = Path.Combine(logPath, info.Name);
            }

            InvariantStringArray.Sort(files, 0, count);
            Array.Reverse(files, 0, count);

            if (errorEntryList != null)
            {
                /* Find the proper page */
                int firstIndex = pageIndex * pageSize;
                int lastIndex = (firstIndex + pageSize < count) ? firstIndex + pageSize : count;

                /* Open them up and rehydrate the list */
                for (int i = firstIndex; i < lastIndex; i++)
                {
                    XmlTextReader reader = new XmlTextReader(files[i]);

                    try
                    {
                        while (reader.IsStartElement("error"))
                        {
                            string id = reader.GetAttribute("errorId");
                            Error error = ErrorXml.Decode(reader);
                            errorEntryList.Add(new ErrorLogEntry(this, id, error));
                        }
                    }
                    finally
                    {
                        reader.Close();
                    }
                }
            }
    
            /* Return how many are total */
            return count;
        }

        /// <summary>
        /// Returns the specified error from the filesystem, or throws an exception if it does not exist.
        /// </summary>
        
        public override ErrorLogEntry GetError(string id)
        {
            try
            {
                /* Make sure the identifier is a valid GUID */
                id = (new Guid(id)).ToString();
            }
            catch (FormatException e)
            {
                throw new ArgumentException(e.Message, id, e);
            }

            /* Get the file folder list - should only return one ever */
            string[] files = Directory.GetFiles(LogPath, string.Format("error-*-{0}.xml", id));
            
            if (files.Length < 1)
                throw new FileNotFoundException(string.Format("Cannot locate error file for error with ID {0}.", id));

            FileInfo file = new FileInfo(files[0]);
            if (!IsUserFile(file.Attributes))
                return null;

            XmlTextReader reader = new XmlTextReader(file.FullName);
            
            try
            {
                Error error = ErrorXml.Decode(reader);
                return new ErrorLogEntry(this, id, error);
            }
            finally
            {
                reader.Close();
            }
        }

        private static bool IsUserFile(FileAttributes attributes)
        {
            return 0 == (attributes & (FileAttributes.Directory | 
                                       FileAttributes.Hidden | 
                                       FileAttributes.System));
        }
    }
}
