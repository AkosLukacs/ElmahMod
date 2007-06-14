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
    using System.Configuration;
    using System.Xml;
    using Elmah.Assertions;

    #endregion

    /// <summary>
    /// Handler for the &lt;errorFilter&gt; section of the
    /// configuration file.
    /// </summary>

    internal sealed class ErrorFilterSectionHandler : IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, XmlNode section)
        {
            if (section == null)
                throw new ArgumentNullException("section");
            
            //
            // Either inherit the incoming parent configuration (for example
            // from the machine configuration file) or start with a fresh new
            // one.
            //

            ErrorFilterConfiguration config;

            if (parent != null)
            {
                ErrorFilterConfiguration parentConfig = (ErrorFilterConfiguration) parent;
                config = (ErrorFilterConfiguration) ((ICloneable) parentConfig).Clone();
            }    
            else
            {
                config = new ErrorFilterConfiguration();
            }

            //
            // Take the first child of <test> and turn it into the
            // assertion.
            //

            XmlElement assertionNode = (XmlElement) section.SelectSingleNode("test/*");

            if (assertionNode != null)
                config.SetAssertion(AssertionFactory.Create(assertionNode));

            return config;
        }
    }
}
