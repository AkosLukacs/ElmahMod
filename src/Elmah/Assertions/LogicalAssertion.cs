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

    public sealed class LogicalAssertion : CompositeAssertion
    {
        private readonly bool _not;
        private readonly bool _all;

        public static LogicalAssertion LogicalAnd(XmlElement config)
        {
            return new LogicalAssertion(config, false, true);
        }

        public static LogicalAssertion LogicalOr(XmlElement config)
        {
            return new LogicalAssertion(config, false, false);
        }

        public static LogicalAssertion LogicalNot(XmlElement config)
        {
            return new LogicalAssertion(config, true, true);
        }

        private LogicalAssertion(XmlElement config, bool not, bool all) : 
            base(config)
        {
            _not = not;
            _all = all;
        }

        public override bool Test(object context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            if (Count == 0)
                return false;

            //
            // Walk through all child assertions and determine the
            // outcome, OR-ing or AND-ing each as needed.
            //

            bool result = false;

            foreach (IAssertion assertion in this)
            {
                if (assertion == null)
                    continue;

                bool testResult = assertion.Test(context);
                
                if (_not) 
                    testResult = !testResult;
                
                if (testResult)
                {
                    if (!_all) return true;
                    result = true;
                }
                else
                {
                    if (_all) return false;
                }
            }

            return result;
        }
    }
}
