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

    #endregion

    /// <summary>
    /// Helper class for handling values in configuration sections.
    /// </summary>

    internal sealed class ConfigurationSectionHelper
    {
        public static string GetValueAsString(XmlAttribute attribute)
        {
            return GetValueAsString(attribute, string.Empty);
        }

        public static string GetValueAsString(XmlAttribute attribute, string defaultValue)
        {
            if (attribute == null)
                return defaultValue;
            
            return Mask.EmptyString(attribute.Value, defaultValue);
        }

        public static bool GetValueAsBoolean(XmlAttribute attribute)
        {
            //
            // If the attribute is absent, then always assume the default value
            // of false. Not allowing the default value to be parameterized
            // maintains a consisent policy and makes it easier for the user to
            // remember that all boolean options default to false if not
            // specified.
            //

            if (attribute == null)
                return false;

            try
            {
                return XmlConvert.ToBoolean(attribute.Value);
            }
            catch (FormatException e)
            {
                throw new ConfigurationException(string.Format("Error in parsing the '{0}' attribute of the '{1}' element as a boolean value. Use either 1, 0, true or false (latter two being case-sensitive).", attribute.Name, attribute.OwnerElement.Name), e, attribute);
            }
        }
        
        private ConfigurationSectionHelper()
        {
            throw new NotSupportedException();
        }
    }
}