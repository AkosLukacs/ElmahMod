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
    using System.Web;

    #endregion

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

#if NET_1_0 || NET_1_1

            string userHostAddress = Mask.NullString(request.UserHostAddress);
            
            return userHostAddress.Equals("127.0.0.1") /* IP v4 */ || 
                userHostAddress.Equals("::1") /* IP v6 */ ||
                userHostAddress.Equals(request.ServerVariables["LOCAL_ADDR"]);
#else
            return request.IsLocal;
#endif
        }

        private HttpRequestSecurity()
        {
            throw new NotSupportedException();
        }
    }
}
