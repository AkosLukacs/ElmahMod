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
    internal sealed class Build
    {
#if DEBUG
        public const bool IsDebug = true;
        public const bool IsRelease = !IsDebug;
        public const string Type = "Debug";
        public const string TypeUppercase = "DEBUG";
        public const string TypeLowercase = "debug";
#else
        public const bool IsDebug = false;
        public const bool IsRelease = !IsDebug;
        public const string Type = "Release";
        public const string TypeUppercase = "RELEASE";
        public const string TypeLowercase = "release";
#endif

#if NET_1_0
        public const string Runtime = "net-1.0";
#elif NET_1_1
        public const string Runtime = "net-1.1";
#elif NET_2_0
        public const string Runtime = "net-2.0";
#elif NET_3_5
        public const string Runtime = "net-3.5";
#else
        public const string Runtime = "unknown";
#endif

        public const string Configuration = TypeLowercase + "; " + Status + "; " + Runtime;

        /// <summary>
        /// Gets a string representing the version of the CLR saved in 
        /// the file containing the manifest. Under 1.0, this returns
        /// the hard-wired string "v1.0.3705".
        /// </summary>

        public static string ImageRuntimeVersion
        {
            get
            {
#if NET_1_0
                //
                // As Assembly.ImageRuntimeVersion property was not available
                // under .NET Framework 1.0, we just return the version 
                // hard-coded based on conditional compilation symbol.
                //

                return "v1.0.3705";
#else
                return typeof(ErrorLog).Assembly.ImageRuntimeVersion;
#endif
            }
        }

        /// <summary>
        /// This is the status or milestone of the build. Examples are
        /// M1, M2, ..., Mn, BETA1, BETA2, RC1, RC2, RTM.
        /// </summary>

        public const string Status = "BETA2";
    }
}
