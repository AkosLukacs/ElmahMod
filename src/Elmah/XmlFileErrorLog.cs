#region License, Terms and Author(s)
//
// ELMAH - Error Logging Modules and Handlers for ASP.NET
// Copyright (c) 2004-9 Atif Aziz. All rights reserved.
//
//  Author(s):
//
//      Scott Wilson <sw@scratchstudio.net>
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

[assembly: Elmah.Scc("$Id$")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Xml;
    using System.Collections.Generic;

    using IDictionary = System.Collections.IDictionary;

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
        
        public XmlFileErrorLog(IDictionary config) // TODO arg checking
        {
            var logPath = config["logPath"] as string ?? string.Empty;

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

            if (logPath.StartsWith("~/"))
                logPath = MapPath(logPath);

            _logPath = logPath;
        }

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
            var errorId = Guid.NewGuid().ToString();
            
            var timeStamp = DateTime.UtcNow.ToString("yyyy-MM-ddHHmmssZ", CultureInfo.InvariantCulture);
            var path = Path.Combine(LogPath, string.Format(@"error-{0}-{1}.xml", timeStamp, errorId));

            using (var writer = new XmlTextWriter(path, Encoding.UTF8))
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartElement("error");
                writer.WriteAttributeString("errorId", errorId);
                ErrorXml.Encode(error, writer);
                writer.WriteEndElement();
                writer.Flush();
            }

            return errorId;
        }

        /// <summary>
        /// Returns a page of errors from the folder in descending order 
        /// of logged time as defined by the sortable filenames.
        /// </summary>

        public override int GetErrors(int pageIndex, int pageSize, IList<ErrorLogEntry> errorEntryList)
        {
            if (pageIndex < 0) throw new ArgumentOutOfRangeException("pageIndex", pageIndex, null);
            if (pageSize < 0) throw new ArgumentOutOfRangeException("pageSize", pageSize, null);

            var logPath = LogPath;
            var dir = new DirectoryInfo(logPath);
            var infos = dir.GetFiles("error-*.xml");

            if (!infos.Any())
                return 0;

            var files = dir.GetFiles("error-*.xml")
                           .Where(info => IsUserFile(info.Attributes))
                           .OrderBy(info => info.Name, StringComparer.OrdinalIgnoreCase)
                           .Select(info => Path.Combine(logPath, info.Name))
                           .Reverse()
                           .ToArray();

            if (errorEntryList != null)
            {
                var entries = files.Skip(pageIndex * pageSize)
                                   .Take(pageSize)
                                   .Select(file =>
                                   {
                                       using (var reader = XmlReader.Create(file))
                                       {
                                           if (!reader.IsStartElement("error"))
                                               return null;
                                           
                                           var id = reader.GetAttribute("errorId");
                                           var error = ErrorXml.Decode(reader);
                                           return new ErrorLogEntry(this, id, error);
                                       }
                                   });

                foreach (var entry in entries)
                    errorEntryList.Add(entry);
            }

            return files.Length; // Return total
        }

        /// <summary>
        /// Returns the specified error from the filesystem, or throws an exception if it does not exist.
        /// </summary>
        
        public override ErrorLogEntry GetError(string id)
        {
            try
            {
                id = (new Guid(id)).ToString(); // validate GUID
            }
            catch (FormatException e)
            {
                throw new ArgumentException(e.Message, id, e);
            }

            var file = new DirectoryInfo(LogPath).GetFiles(string.Format("error-*-{0}.xml", id))
                                                 .FirstOrDefault();
            
            if (file == null)
                throw new FileNotFoundException(string.Format("Cannot locate error file for error with ID {0}.", id));

            if (!IsUserFile(file.Attributes))
                return null;

            using (var reader = XmlReader.Create(file.FullName))
                return new ErrorLogEntry(this, id, ErrorXml.Decode(reader));
        }

        private static bool IsUserFile(FileAttributes attributes)
        {
            return 0 == (attributes & (FileAttributes.Directory | 
                                       FileAttributes.Hidden | 
                                       FileAttributes.System));
        }
    }
}
