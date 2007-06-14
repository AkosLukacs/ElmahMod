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
    using System.Reflection;
    using System.Runtime.Remoting;
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

                if (!SoapServices.DecodeXmlNamespaceForClrTypeNamespace(xmlns, out ns, out assemblyName))
                    throw new Exception(string.Format("Error decoding CLR type namespace and assembly from the XML namespace '{0}'.", xmlns)); // TODO: Throw a more specific exception

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
        
        private AssertionFactory()
        {
            throw new NotSupportedException();
        }
    }
}
