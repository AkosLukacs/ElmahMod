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

namespace Elmah
{
	#region Imports

	using System;
    using System.Web;

    using Stream = System.IO.Stream;
    using Encoding = System.Text.Encoding;

	#endregion

    /// <summary>
    /// Reads a resource from the assembly manifest and returns its contents
    /// as the response entity.
    /// </summary>

    internal sealed class ManifestResourceHandler : IHttpHandler
	{
        private string _resourceName;
        private string _contentType;
        private Encoding _responseEncoding;

        public ManifestResourceHandler(string resourceName, string contentType) :
            this(resourceName, contentType, null) {}

        public ManifestResourceHandler(string resourceName, string contentType, Encoding responseEncoding)
        {
            Debug.AssertStringNotEmpty(resourceName);
            Debug.AssertStringNotEmpty(contentType);

            _resourceName = resourceName;
            _contentType = contentType;
            _responseEncoding = responseEncoding;
        }

        public void ProcessRequest(HttpContext context)
        {
            //
            // Grab the resource stream from the manifest.
            //

            Type thisType = this.GetType();

            using (Stream stream = thisType.Assembly.GetManifestResourceStream(thisType, _resourceName))
            {

                //
                // Allocate a buffer for reading the stream. The maximum size
                // of this buffer is fixed to 4 KB.
                //

                byte[] buffer = new byte[Math.Min(stream.Length, 4096)];

                //
                // Set the response headers for indicating the content type 
                // and encoding (if specified).
                //

                HttpResponse response = context.Response;
                response.ContentType = _contentType;

                if (_responseEncoding != null)
                {
                    response.ContentEncoding = _responseEncoding;
                }

                //
                // Finally, write out the bytes!
                //

                int bytesWritten = 0;

                do
                {
                    int readCount = stream.Read(buffer, 0, buffer.Length);
                    response.OutputStream.Write(buffer, 0, readCount);
                    bytesWritten += readCount;
                }
                while (bytesWritten < stream.Length);
            }
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}
