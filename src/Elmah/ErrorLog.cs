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

[assembly: Elmah.Scc("$Id$")]

namespace Elmah
{
    #region Imports
    
    using System;

    using HttpContext = System.Web.HttpContext;
    using IList = System.Collections.IList;
    using HttpRuntime = System.Web.HttpRuntime;

    #endregion

    /// <summary>
    /// Represents an error log capable of storing and retrieving errors
    /// generated in an ASP.NET Web application.
    /// </summary>

    public abstract class ErrorLog
    {
        [ ThreadStatic ] private static ErrorLog _defaultLog;
        
#if !NET_1_1 && !NET_1_0
        private string _appName;
#endif

        /// <summary>
        /// Logs an error in log for the application.
        /// </summary>
        
        public abstract string Log(Error error);

        /// <summary>
        /// Retrieves a single application error from log given its 
        /// identifier, or null if it does not exist.
        /// </summary>

        public abstract ErrorLogEntry GetError(string id);
        
        /// <summary>
        /// Retrieves a page of application errors from the log in 
        /// descending order of logged time.
        /// </summary>
        
        public abstract int GetErrors(int pageIndex, int pageSize, IList errorEntryList);

        /// <summary>
        /// Get the name of this log.
        /// </summary>

        public virtual string Name
        {
            get { return this.GetType().Name; }   
        }

        /// <summary>
        /// Gets the name of the application to which the log is scoped.
        /// </summary>
        
        public virtual string ApplicationName
        {
            get
            {
#if NET_1_1 || NET_1_0
                return HttpRuntime.AppDomainAppId;
#else
                if (_appName == null)
                {
                    string path = HttpRuntime.AppDomainAppVirtualPath;
                    string[] parts = path.Split('/');
                    _appName = Mask.EmptyString(parts[parts.Length - 1], "/"); 
                }

                return _appName;
#endif
            }
        }

        /// <summary>
        /// Gets the default error log implementation specified in the 
        /// configuration file, or the in-memory log implemention if
        /// none is configured.
        /// </summary>
        
        public static ErrorLog Default
        {
            get 
            { 
                if (_defaultLog == null)
                {
                    //
                    // Determine the default store type from the configuration and 
                    // create an instance of it.
                    //

                    ErrorLog log = (ErrorLog) SimpleServiceProviderFactory.CreateFromConfigSection("elmah/errorLog");

                    //
                    // If no object got created (probably because the right 
                    // configuration settings are missing) then default to 
                    // the in-memory log implementation.
                    //

                    _defaultLog = log != null ? log : new MemoryErrorLog();
                }

                return _defaultLog;
            }
        }
    }
}
