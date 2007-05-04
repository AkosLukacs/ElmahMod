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

    using SerializationInfo = System.Runtime.Serialization.SerializationInfo;
    using StreamingContext = System.Runtime.Serialization.StreamingContext;

    #endregion

    /// <summary>
    /// The exception that is thrown when to test the error logging 
    /// subsystem. This exception is used for testing purposes only and 
    /// should not be used for any other purpose.
    /// </summary>
    
    [ Serializable ]
    internal sealed class TestException : System.ApplicationException
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="TestException"/> class.
        /// </summary>

        public TestException() : 
            this("This is a test exception that can be safely ignored.") {}
        
        /// <summary>
        /// Initializes a new instance of the <see cref="TestException"/> class 
        /// with a specified error message.
        /// </summary>

        public TestException(string message) : 
            base(message) {}
        
        /// <summary>
        /// ializes a new instance of the <see cref="TestException"/> 
        /// class with a specified error message and a reference to the 
        /// inner exception that is the cause of this exception.
        /// </summary>

        public TestException(string message, Exception innerException) : 
            base(message, innerException) {}
        
        /// <summary>
        /// Initializes a new instance of the <see cref="TestException"/> class 
        /// with serialized data.
        /// </summary>

        private TestException(SerializationInfo info, StreamingContext context) : 
            base(info, context) {}
    }
}

