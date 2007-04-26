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

    using ReaderWriterLock = System.Threading.ReaderWriterLock;
    using Timeout = System.Threading.Timeout;
    using NameObjectCollectionBase = System.Collections.Specialized.NameObjectCollectionBase;
    using IList = System.Collections.IList;
    using IDictionary = System.Collections.IDictionary;
    using CultureInfo = System.Globalization.CultureInfo;

	#endregion

    /// <summary>
    /// An <see cref="ErrorLog"/> implementation that uses memory as its 
    /// backing store. 
    /// </summary>
    /// <remarks>
    /// All <see cref="MemoryErrorLog"/> instances will share the same memory 
    /// store that is bound to the application (not an instance of this class).
    /// </remarks>

    public sealed class MemoryErrorLog : ErrorLog
	{
        //
        // The collection that provides the actual storage for this log
        // implementation and a lock to guarantee concurrency correctness.
        //

        private static EntryCollection _entries;
        private readonly static ReaderWriterLock _lock = new ReaderWriterLock();

        //
        // IMPORTANT! The size must be the same for all instances
        // for the entires collection to be intialized correctly.
        //

        private int _size;

        /// <summary>
        /// The maximum number of errors that will ever be allowed to be stored
        /// in memory.
        /// </summary>
        
        public static readonly int MaximumSize = 500;
        
        /// <summary>
        /// The maximum number of errors that will be held in memory by default 
        /// if no size is specified.
        /// </summary>
        
        public static readonly int DefaultSize = 15;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryErrorLog"/> class
        /// with a default size for maximum recordable entries.
        /// </summary>

        public MemoryErrorLog() : this(DefaultSize) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryErrorLog"/> class
        /// with a specific size for maximum recordable entries.
        /// </summary>

        public MemoryErrorLog(int size)
        {
            if (size < 0 || size > MaximumSize)   
                throw new ArgumentOutOfRangeException("size");

            _size = size;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryErrorLog"/> class
        /// using a dictionary of configured settings.
        /// </summary>
        
        public MemoryErrorLog(IDictionary config)
        {
            if (config == null)
            {
                _size = DefaultSize;
            }
            else
            {
                string sizeString = StringEtc.MaskNull((string) config["size"]);

                if (sizeString.Length == 0)
                {
                    _size = DefaultSize;
                }
                else
                {
                    _size = Convert.ToInt32(sizeString, CultureInfo.InvariantCulture);
                    _size = Math.Max(0, Math.Min(MaximumSize, _size));
                }
            }
        }

        /// <summary>
        /// Gets the name of this error log implementation.
        /// </summary>

        public override string Name
        {
            get { return "In-Memory Error Log"; }
        }

        /// <summary>
        /// Logs an error to the application memory.
        /// </summary>
        /// <remarks>
        /// If the log is full then the oldest error entry is removed.
        /// </remarks>

        public override void Log(Error error)
        {
            if (error == null)
                throw new ArgumentNullException("error");

            //
            // Make a copy of the error to log since the source is mutable.
            // Assign a new GUID and create an entry for the error.
            //

            error = (Error) ((ICloneable) error).Clone();
            error.ApplicationName = this.ApplicationName;
            Guid newId = Guid.NewGuid();
            ErrorLogEntry entry = new ErrorLogEntry(this, newId.ToString(), error);

            _lock.AcquireWriterLock(Timeout.Infinite); 

            try
            {
                if (_entries == null)
                {
                    _entries = new EntryCollection(_size);
                }

                _entries.Add(newId, entry);
            }
            finally
            {
                _lock.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// Returns the specified error from application memory, or null 
        /// if it does not exist.
        /// </summary>

        public override ErrorLogEntry GetError(string id)
        {
            _lock.AcquireReaderLock(Timeout.Infinite);

            ErrorLogEntry entry;

            try
            {
                if (_entries == null)
                {
                    return null;
                }

                entry = _entries[id];
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }

            if (entry == null)
            {
                return null;
            }

            //
            // Return a copy that the caller can party on.
            //

            Error error = (Error) ((ICloneable) entry.Error).Clone();
            return new ErrorLogEntry(this, entry.Id, error);
        }

        /// <summary>
        /// Returns a page of errors from the application memory in
        /// descending order of logged time.
        /// </summary>

        public override int GetErrors(int pageIndex, int pageSize, IList errorEntryList)
        {
            if (pageIndex < 0)
                throw new ArgumentOutOfRangeException("pageIndex");

            if (pageSize < 0)
                throw new ArgumentOutOfRangeException("pageSite");

            //
            // To minimize the time for which we hold the lock, we'll first
            // grab just references to the entries we need to return. Later,
            // we'll make copies and return those to the caller. Since Error 
            // is mutable, we don't want to return direct references to our 
            // internal versions since someone could change their state.
            //

            ErrorLogEntry[] selectedEntries;
            int totalCount;

            _lock.AcquireReaderLock(Timeout.Infinite);

            try
            {
                if (_entries == null)
                {
                    return 0;
                }

                int lastIndex = Math.Max(0, _entries.Count - (pageIndex * pageSize)) - 1;
                selectedEntries = new ErrorLogEntry[lastIndex + 1];

                int sourceIndex = lastIndex;
                int targetIndex = 0;

                while (sourceIndex >= 0)
                {
                    selectedEntries[targetIndex++] = _entries[sourceIndex--];
                }

                totalCount = _entries.Count;
            }
            finally
            {
                _lock.ReleaseReaderLock();
            }

            //
            // Return copies of fetched entries. If the Error class would 
            // be immutable then this step wouldn't be necessary.
            //

            foreach (ErrorLogEntry entry in selectedEntries)
            {
                Error error = (Error) ((ICloneable) entry.Error).Clone();
                errorEntryList.Add(new ErrorLogEntry(this, entry.Id, error));
            }

            return totalCount;
        }

        private class EntryCollection : NameObjectCollectionBase
        {
            private int _size;

            public EntryCollection(int size) : base(size)
            {
                _size = size;
            }

            public ErrorLogEntry this[int index]
            {
                get { return (ErrorLogEntry) BaseGet(index); }
            }

            public ErrorLogEntry this[Guid id]
            {
                get { return (ErrorLogEntry) BaseGet(id.ToString()); }
            }

            public ErrorLogEntry this[string id]
            {
                get { return this[new Guid(id)]; }
            }

            public void Add(Guid id, ErrorLogEntry entry)
            {
                Debug.Assert(entry != null);
                Debug.AssertStringNotEmpty(entry.Id);

                Debug.Assert(this.Count <= _size);

                if (this.Count == _size)
                {
                    BaseRemoveAt(0);
                }                    

                BaseAdd(entry.Id, entry);
            }
        }
	}
}
