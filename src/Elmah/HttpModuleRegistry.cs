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
    using System.Collections;
    using System.Security;
    using System.Web;

    #endregion

    internal sealed class HttpModuleRegistry
    {
        private static readonly string _key = typeof(HttpModuleRegistry).FullName;

        public static bool RegisterInPartialTrust(HttpApplication application, IHttpModule module)
        {
            if (application == null)
                throw new ArgumentNullException("application");

            if (module == null)
                throw new ArgumentNullException("module");

            if (IsHighlyTrusted())
                return false;
            
            HttpApplicationState state = application.Application;
            state.Lock();
            
            try
            {
                IList moduleList = (IList) state[_key];
                
                if (moduleList == null)
                {
                    moduleList = new ArrayList(4);
                    state.Add(_key, moduleList);
                }

                if (moduleList.Contains(module))
                    throw new ApplicationException("Duplicate module registration.");
                
                moduleList.Add(module);
            }
            finally
            {
                state.UnLock();
            }

            return true;
        }

        public static ICollection GetModules(HttpApplication application)
        {
            if (application == null)
                throw new ArgumentNullException("application");
            
            try
            {
                return application.Modules;
            }
            catch (SecurityException)
            {
                //
                // Pass through because probably this is a partially trusted
                // environment that does have access to the modules 
                // collection over HttpApplication so we have to resort
                // to our own devices...
                //
            }
            
            HttpApplicationState state = application.Application;
            state.Lock();
            
            try
            {
                IList moduleList = (IList) state[_key];
                IHttpModule[] modules = new IHttpModule[moduleList.Count];
                moduleList.CopyTo(modules, 0);
                return modules;
            }
            finally
            {
                state.UnLock();
            }
        }
        
        private static bool IsHighlyTrusted() 
        {
#if NET_1_0
            //
            // ASP.NET 1.0 applications always required and ran under full 
            // trust so we just return true here.
            //

            return true;
#else
            try
            {
                AspNetHostingPermission permission = new AspNetHostingPermission(AspNetHostingPermissionLevel.High);
                permission.Demand();
                return true;
            }
            catch (SecurityException)
            {
                return false;
            }
#endif
        }

        private HttpModuleRegistry()
        {
            throw new NotSupportedException();
        }
    }
}
