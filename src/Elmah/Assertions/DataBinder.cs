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

    #endregion

    /// <summary>
    /// Provides data expression evaluation facilites similar to 
    /// <see cref="System.Web.UI.DataBinder"/> in ASP.NET.
    /// </summary>

    public sealed class DataBinder
    {
        public static object Eval(object container, string expression)
        {
            if (container == null)
                throw new ArgumentNullException("container");

            //
            // The ASP.NET DataBinder.Eval method does not like an empty or null
            // expression. Rather than making it an unnecessary exception, we
            // turn a nil-expression to mean, "evaluate to container."
            //

            if (Mask.NullString(expression).Length == 0)
                return container;

            //
            // CAUTION! DataBinder.Eval performs late-bound evaluation, using
            // reflection, at runtime, therefore it can cause performance less
            // than optimal. If needed, this point can be used to either
            // compile the expression or optimize out certain cases (known to be
            // heavily used) by binding statically at compile-time or even
            // partially at runtime using delegates.
            //

            return System.Web.UI.DataBinder.Eval(container, expression);
        }

        private DataBinder()
        {
            throw new NotSupportedException();
        }
    }
}
