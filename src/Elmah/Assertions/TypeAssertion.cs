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
    using System.Xml;

    #endregion

    /// <summary>
    /// An assertion implementation whose test is based on whether
    /// the result of an input expression evaluated against a context
    /// matches a regular expression pattern or not.
    /// </summary>

    public sealed class TypeAssertion : DataBoundAssertion
    {
        private readonly Type _expectedType;
        private readonly bool _byCompatibility;

        public TypeAssertion(XmlElement config, bool byCompatibility) : 
            base(config)
        {
            _byCompatibility = byCompatibility;

            //
            // Get the expected type.
            //

            string typeName = ConfigurationSectionHelper.GetValueAsString(config.Attributes["type"]);

            if (typeName.Length > 0)
            {
                Type type = Type.GetType(typeName, /* throwOnError */ true);
            
                if (type.IsInterface || (type.IsClass && type.IsAbstract))
                {
                    //
                    // Interfaces and abstract classes will always have an 
                    // ancestral relationship.
                    //
                    
                    _byCompatibility = true;
                }
                
                _expectedType = type;
            }
        }

        public Type ExpectedType
        {
            get { return _expectedType; }
        }

        public bool ByCompatibility
        {
            get { return _byCompatibility; }
        }

        public override bool Test(object context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            if (ExpectedType == null)
                return false;
            
            return base.Test(context);
        }

        protected override object GetBoundData(object context)
        {
            if (DataBindingExpression.Length > 0)
                return base.GetBoundData(context);
            
            //
            // If no data expression is supplied then then assume the 
            // reasonable default that the user wants the exception from the 
            // context.
            //

            ExceptionFilterEventArgs args = context as ExceptionFilterEventArgs;
            
            if (args != null)
                return args.Exception;
            
            //
            // Hmm...the context is not the expected type so resort to
            // late-binding.
            //

            return Eval(context, "Exception");
        }

        protected override bool TestResult(object result)
        {
            if (result == null)
                return false;

            Type resultType = result.GetType();
            Type expectedType = ExpectedType;
            
            Debug.Assert(expectedType != null);
            
            return ByCompatibility ? 
                expectedType.IsAssignableFrom(resultType) : 
                expectedType.Equals(resultType);
        }
    }
}