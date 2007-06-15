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
    using System.Configuration;
    using System.Reflection;
    using System.Web;
    using System.Xml;
    
    #endregion

    public delegate IAssertion AssertionFactoryHandler(XmlElement config);

    public sealed class AssertionFactory
    {
        public static IAssertion assert_equal(XmlElement config)
        {
            return new ComparisonAssertion(config, ComparisonResults.Equal);
        }

        public static IAssertion assert_not_equal(XmlElement config)
        {
            return new UnaryNotAssertion(new ComparisonAssertion(config, ComparisonResults.Equal));
        }

        public static IAssertion assert_lesser(XmlElement config)
        {
            return new ComparisonAssertion(config, ComparisonResults.Lesser);
        }

        public static IAssertion assert_lesser_or_equal(XmlElement config)
        {
            return new ComparisonAssertion(config, ComparisonResults.LesserOrEqual);
        }

        public static IAssertion assert_greater(XmlElement config)
        {
            return new ComparisonAssertion(config, ComparisonResults.Greater);
        }

        public static IAssertion assert_greater_or_equal(XmlElement config)
        {
            return new ComparisonAssertion(config, ComparisonResults.GreaterOrEqual);
        }
        
        public static IAssertion assert_and(XmlElement config)
        {
            return LogicalAssertion.LogicalAnd(config);
        }

        public static IAssertion assert_or(XmlElement config)
        {
            return LogicalAssertion.LogicalOr(config);
        }
        
        public static IAssertion assert_not(XmlElement config)
        {
            return LogicalAssertion.LogicalNot(config);
        }

        public static IAssertion assert_is_type(XmlElement config)
        {
            return new TypeAssertion(config, false);
        }

        public static IAssertion assert_is_type_compatible(XmlElement config)
        {
            return new TypeAssertion(config, true);
        }

        public static IAssertion Create(XmlElement config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            string name = "assert_" + config.LocalName;
            
            if (name.IndexOf('-') > 0)
                name = name.Replace("-", "_");
            
            Type factoryType;

            string xmlns = Mask.NullString(config.NamespaceURI);

            if (xmlns.Length > 0)
            {
                string assemblyName, ns;

                if (!DecodeClrTypeNamespaceFromXmlNamespace(xmlns, out ns, out assemblyName))
                    throw new ConfigurationException(string.Format("Error decoding CLR type namespace and assembly from the XML namespace '{0}'.", xmlns));
                
                // TODO: Throw exception here if assembly name is empty.
                // TODO: Review for case of empty namespace.
                
                Assembly assembly = Assembly.Load(assemblyName);
                factoryType = assembly.GetType(ns + ".AssertionFactory", /* throwOnError */ true);
            }
            else
            {
                factoryType = typeof(AssertionFactory);
            }
            
            AssertionFactoryHandler handler = (AssertionFactoryHandler) Delegate.CreateDelegate(typeof(AssertionFactoryHandler), factoryType, name);
            return handler(config);
        }
        

        /// <remarks>
        /// Ideally, we would be able to use SoapServices.DecodeXmlNamespaceForClrTypeNamespace
        /// but that requires a link demand permission that will fail in partially trusted
        /// environments such as ASP.NET medium trust.
        /// </remarks>
        
        private static bool DecodeClrTypeNamespaceFromXmlNamespace(string xmlns, out string typeNamespace, out string assemblyName)
        {
            Debug.Assert(xmlns != null);

            assemblyName = string.Empty;
            typeNamespace = string.Empty;

            const string assemblyNS = "http://schemas.microsoft.com/clr/assem/";
            const string namespaceNS = "http://schemas.microsoft.com/clr/ns/";
            const string fullNS = "http://schemas.microsoft.com/clr/nsassem/";
            
            if (OrdinalStringStartsWith(xmlns, assemblyNS))
            {
                assemblyName = HttpUtility.UrlDecode(xmlns.Substring(assemblyNS.Length));
                return assemblyName.Length > 0;
            }
            else if (OrdinalStringStartsWith(xmlns, namespaceNS))
            {
                typeNamespace = xmlns.Substring(namespaceNS.Length);
                return typeNamespace.Length > 0;
            }
            else if (OrdinalStringStartsWith(xmlns, fullNS))
            {
                int index = xmlns.IndexOf("/", fullNS.Length);
                typeNamespace = xmlns.Substring(fullNS.Length, index - fullNS.Length);
                assemblyName = HttpUtility.UrlDecode(xmlns.Substring(index + 1));

                return assemblyName.Length > 0 && typeNamespace.Length > 0;
            }
            else
            {
                return false;
            }
        }
        
        private static bool OrdinalStringStartsWith(string s, string prefix)
        {
            Debug.Assert(s != null);
            Debug.Assert(prefix != null);
            
            return s.Length >= prefix.Length && 
                string.CompareOrdinal(s.Substring(0, prefix.Length), prefix) == 0;
        }
 
        private AssertionFactory()
        {
            throw new NotSupportedException();
        }
    }
}
