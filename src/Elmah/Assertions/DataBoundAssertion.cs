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

    public abstract class DataBoundAssertion : IAssertion
    {
        private readonly string _expression;

        public DataBoundAssertion(XmlElement config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            _expression = ConfigurationSectionHelper.GetValueAsString(config.Attributes["binding"]).Trim();
        }

        public virtual string DataBindingExpression
        {
            get { return Mask.NullString(_expression); }
        }

        public virtual bool Test(object context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            return TestResult(GetBoundData(context));
        }

        protected abstract bool TestResult(object result);

        protected virtual object GetBoundData(object context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            return Eval(context, DataBindingExpression);
        }

        protected virtual object Eval(object context, string expression) 
        {
            return DataBinder.Eval(context, expression);
        }
    }
}