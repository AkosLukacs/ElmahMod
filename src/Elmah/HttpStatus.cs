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

    using HttpWorkerRequest = System.Web.HttpWorkerRequest;
    using CultureInfo = System.Globalization.CultureInfo;

    #endregion

    /// <summary>
    /// Represents an HTTP status (code plus reason) as per 
    /// <a href="http://www.w3.org/Protocols/rfc2616/rfc2616-sec6.html#sec6.1">Section 6.1 of RFC 2616</a>.
    /// </summary>

    [ Serializable ]
    internal struct HttpStatus
    {
        public static readonly HttpStatus NotFound = new HttpStatus(404);
        public static readonly HttpStatus InternalServerError = new HttpStatus(500);

        private readonly int _code;
        private readonly string _reason;

        public HttpStatus(int code) :
            this(code, HttpWorkerRequest.GetStatusDescription(code)) {}

        public HttpStatus(int code, string reason)
        {
            Debug.AssertStringNotEmpty(reason);
            
            _code = code;
            _reason = reason;
        }

        public int Code
        {
            get { return _code; }
        }

        public string Reason
        {
            get { return Mask.NullString(_reason); }
        }

        public string StatusLine
        {
            get { return Code.ToString(CultureInfo.InvariantCulture) + " " + Reason; }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (!(obj is HttpStatus))
                return false;

            return Equals((HttpStatus) obj);
        }

        public bool Equals(HttpStatus other)
        {
            return Code.Equals(other.Code);
        }
        
        public override int GetHashCode()
        {
            return Code.GetHashCode();
        }

        public override string ToString()
        {
            return StatusLine;
        }
    }
}