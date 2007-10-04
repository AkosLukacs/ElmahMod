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
    using System.Collections;
    using System.Globalization;

    #endregion

    [ Serializable ]
    internal sealed class SecurityConfiguration
    {
        public static readonly SecurityConfiguration Default;

        private readonly bool _allowRemoteAccess;

        private static readonly string[] _trues = new string[] { "true", "yes", "on", "1" };

        static SecurityConfiguration()
        {
            Default = new SecurityConfiguration((IDictionary) Configuration.GetSubsection("security"));
        }
        
        public SecurityConfiguration(IDictionary options)
        {
            _allowRemoteAccess = GetBoolean(options, "allowRemoteAccess");
        }
        
        public bool AllowRemoteAccess
        {
            get { return _allowRemoteAccess; }
        }

        private static bool GetBoolean(IDictionary options, string name)
        {
            string str = GetString(options, name).Trim().ToLower(CultureInfo.InvariantCulture);
            return Boolean.TrueString.Equals(StringTranslation.Translate(Boolean.TrueString, str, _trues));
        }

        private static string GetString(IDictionary options, string name)
        {
            Debug.Assert(name != null);

            if (options == null)
                return string.Empty;

            object value = options[name];

            if (value == null)
                return string.Empty;

            return Mask.NullString(value.ToString());
        }
    }
}