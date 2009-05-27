#region License, Terms and Author(s)
//
// ELMAH - Error Logging Modules and Handlers for ASP.NET
// Copyright (c) 2004-9 Atif Aziz. All rights reserved.
//
//  Author(s):
//
//      Atif Aziz, http://www.raboof.com
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
    using System.Web;

    #endregion

    // TODO: Review if this is needed anymore. 
    // It was just a wrapper to isolate differences between .NET Framework 1.x and 2.0 onwards.

    /// <summary>
    /// Security-related helper methods for web requests.
    /// </summary>

    public sealed class HttpRequestSecurity
    {
        /// <summary>
        /// Determines whether the request is from the local computer or not.
        /// </summary>
        /// <remarks>
        /// This method is primarily for .NET Framework 1.x where the
        /// <see cref="HttpRequest.IsLocal"/> was not available.
        /// </remarks>

        public static bool IsLocal(HttpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            return request.IsLocal;
        }

        private HttpRequestSecurity()
        {
            throw new NotSupportedException();
        }
    }
}
