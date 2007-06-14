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

namespace Elmah.Assertions
{
    #region Imports

    using System;
    using System.Globalization;
    using System.Text.RegularExpressions;
    using System.Xml;

    #endregion

    /// <summary>
    /// An assertion implementation whose test is based on whether
    /// the result of an input expression evaluated against a context
    /// matches a regular expression pattern or not.
    /// </summary>

    public class RegexMatchAssertion : DataBoundAssertion
    {
        private Regex _regex;
        
        public RegexMatchAssertion(XmlElement config) : 
            base(config)
        {
            XmlAttributeCollection attributes = config.Attributes;

            string pattern = ConfigurationSectionHelper.GetValueAsString(attributes["pattern"]);

            if (pattern.Length > 0)
            {
                //
                // NOTE: There is an assumption here that most uses of this
                // assertion will be for culture-insensitive matches. Since
                // it is difficult to imagine all the implications of involving
                // a culture at this point, it seems safer to just err with the
                // invariant culture settings.
                //

                RegexOptions options = RegexOptions.CultureInvariant;

                if (!ConfigurationSectionHelper.GetValueAsBoolean(attributes["caseSensitive"]))
                    options |= RegexOptions.IgnoreCase;

                if (!ConfigurationSectionHelper.GetValueAsBoolean(attributes["dontCompile"]))
                    options |= RegexOptions.Compiled;

                _regex = new Regex(pattern, options);
            }
        }

        public virtual Regex RegexObject
        {
            get { return _regex; }
        }

        public override bool Test(object context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            if (RegexObject == null)
                return false;
            
            return base.Test(context);
        }

        protected override bool TestResult(object result)
        {
            return TestResultMatch(Convert.ToString(result, CultureInfo.InvariantCulture));
        }

        protected virtual bool TestResultMatch(string result)
        {
            Regex expression = RegexObject;
            return expression != null ? expression.Match(result).Success : false;
        }
    }
}