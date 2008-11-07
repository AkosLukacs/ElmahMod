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

        public ComparisonAssertion(ComparisonResultPredicate predicate, IContextExpression source, TypeCode type, string value) :
            base(source)
        {
            if (predicate == null) 
                throw new ArgumentNullException("predicate");

            _predicate = predicate;

            if (type == TypeCode.DBNull 
                || type == TypeCode.Empty 
                || type == TypeCode.Object)
            {
                string message = string.Format(
                    "The {0} value type is invalid for a comparison.", type.ToString());
                throw new ArgumentException(message, "type");
            }

            //
            // Convert the expected value to the comparison type and 
            // save it as a field.
            //

            _expectedValue = Convert.ChangeType(value, type/*, FIXME CultureInfo.InvariantCulture */);
        }

        public IContextExpression Source
        {
            get { return Expression; }
        }

        public object ExpectedValue
        {
            get { return _expectedValue; }
        }

        public override bool Test(object context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            if (ExpectedValue == null)
                return false;

            return base.Test(context);
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