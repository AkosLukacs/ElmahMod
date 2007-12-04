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
    using System.Globalization;
    using System.Xml;

    #endregion

    /// <summary>
    /// An assertion implementation whose test is based on whether
    /// the result of an input expression evaluated against a context
    /// matches a regular expression pattern or not.
    /// </summary>

    public class ComparisonAssertion : DataBoundAssertion
    {
        private readonly object _expectedValue;
        private readonly ComparisonResultPredicate _predicate;

        public ComparisonAssertion(XmlElement config, ComparisonResultPredicate predicate) :
            base(config)
        {
            if (predicate == null)
                throw new ArgumentNullException("predicate");
            
            _predicate = predicate;

            //
            // Get the expected value and its type.
            //
            
            XmlAttributeCollection attributes = config.Attributes;
            
            string valueString = ConfigurationSectionHelper.GetValueAsString(attributes["value"]);
            
            TypeCode type = TypeCode.String;
            string typeName = ConfigurationSectionHelper.GetValueAsString(attributes["type"]);
            
            if (typeName.Length > 0)
            {
                type = (TypeCode) Enum.Parse(typeof(TypeCode), typeName);
                
                if (type == TypeCode.DBNull ||
                    type == TypeCode.Empty ||
                    type == TypeCode.Object)
                {
                    throw new ConfigurationException(string.Format(
                        "The {0} value type is invalid for a comparison assertion.", 
                        type.ToString()));
                }
            }
            
            //
            // Convert the expected value to the comparison type and 
            // save it as a field.
            //

            _expectedValue = Convert.ChangeType(valueString, type);
        }

        public virtual object ExpectedValue
        {
            get { return _expectedValue; }
        }

        public override bool Test(object context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            if (_expectedValue == null)
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

            return DataBinder.Eval(context, "Exception");
        }

        protected override bool TestResult(object result)
        {
            if (result == null)
                return false;

            IComparable right = ExpectedValue as IComparable;
            
            if (right == null)
                return false;

            IComparable left = Convert.ChangeType(result, right.GetType(), CultureInfo.InvariantCulture) as IComparable;
            
            if (left == null)
                return false;
 
            return TestComparison(left, right);
        }

        protected bool TestComparison(IComparable left, IComparable right)
        {
            if (left == null)
                throw new ArgumentNullException("left");
            
            return _predicate(left.CompareTo(right));
        }
    }
}