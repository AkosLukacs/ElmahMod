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

    #endregion

    /// <summary>
    /// Binds an <see cref="Error"/> instance with the <see cref="ErrorLog"/>
    /// instance from where it was served.
    /// </summary>
    
    [ Serializable ]
    public class ErrorLogEntry
    {
        private readonly string _id;
        private readonly ErrorLog _log;
        private readonly Error _error;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorLogEntry"/> class
        /// for a given unique error entry in an error log.
        /// </summary>
     
        public ErrorLogEntry(ErrorLog log, string id, Error error)
        {
            if (log == null)
                throw new ArgumentNullException("log");

            if (id == null)
                throw new ArgumentNullException("id");

            if (id.Length == 0)
                throw new ArgumentOutOfRangeException("id");

            if (error == null)
                throw new ArgumentNullException("error");
            
            _log = log;
            _id = id;
            _error = error;
        }

        /// <summary>
        /// Gets the <see cref="ErrorLog"/> instance where this entry 
        /// originated from.
        /// </summary>
   
        public ErrorLog Log
        {
            get { return _log; }
        }
        
        /// <summary>
        /// Gets the unique identifier that identifies the error entry 
        /// in the log.
        /// </summary>
        
        public string Id
        {
            get { return _id; }
        }

        /// <summary>
        /// Gets the <see cref="Error"/> object held in the entry.
        /// </summary>

        public Error Error
        {
            get { return _error; }
        }
    }
}
