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

// All code in this file requires .NET Framework 2.0 or later.

#if !NET_1_1 && !NET_1_0

[assembly: Elmah.Scc("$Id$")]

namespace Elmah
{
    #region Imports

    using System.Web.UI.WebControls;
    using System.Web;
    using System.Collections.Generic;

    #endregion

    /// <summary>
    /// Methods of this type are designed to serve an
    /// <see cref="System.Web.UI.WebControls.ObjectDataSource" /> control
    /// and are adapted according to expected call signatures and
    /// behavior.
    /// </summary>

    public sealed class ErrorLogDataSourceAdapter
    {
        private readonly ErrorLog _log;

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="ErrorLogDataSourceAdapter"/> class with the default
        /// error log implementation.
        /// </summary>

        public ErrorLogDataSourceAdapter()
        {
            _log = ErrorLog.GetDefault(HttpContext.Current);
        }

        /// <summary>
        /// Use as the value for <see cref="ObjectDataSource.SelectCountMethod"/>.
        /// </summary>

        public int GetErrorCount()
        {
            return _log.GetErrors(0, 0, null);
        }

        /// <summary>
        /// Use as the value for <see cref="ObjectDataSource.SelectMethod"/>.
        /// </summary>
        /// <remarks>
        /// The parameters of this method are named after the default values
        /// for <see cref="ObjectDataSource.StartRowIndexParameterName"/> 
        /// and <see cref="ObjectDataSource.MaximumRowsParameterName"/> so
        /// that the minimum markup is needed for the object data source
        /// control.
        /// </remarks>

        public ErrorLogEntry[] GetErrors(int startRowIndex, int maximumRows)
        {
            return GetErrorsPage(startRowIndex / maximumRows, maximumRows);
        }

        private ErrorLogEntry[] GetErrorsPage(int index, int size)
        {
            List<ErrorLogEntry> list = new List<ErrorLogEntry>(size);
            _log.GetErrors(index, size, list);
            return list.ToArray();
        }
    }
}

#endif
