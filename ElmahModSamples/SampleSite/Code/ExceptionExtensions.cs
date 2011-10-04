
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections;

namespace ElmahModSampleSite
{
   public static class ExceptionExtensions
   {
      /// <summary>
      /// Adds the supplied debug data to the exceptions data dictionary and returns
      /// the exception allowing chaining.
      /// innen: http://blog.freakcode.com/2010/12/exception-data-evolved.html
      /// 2011.02.07 10:04 - LAK - másolva + kicsit módosítva
      /// </summary>
      /// <typeparam name="T">The exception type, you should not need to specify this explicitly</typeparam>
      /// <param name="exception">The exception.</param>
      /// <param name="key">The key of the debug value to be inserted into the exceptions data dictionary.</param>
      /// <param name="value">The value to be inserted into the exceptions data dictionary.</param>
      /// <exception cref="System.ArgumentNullException">key is null</exception>
      /// <exception cref="System.ArgumentException">An element with the same key already exists in the Data dictionary</exception>
      public static T AddData<T>(this T exception, string key, object value) where T : Exception
      {
         if (exception == null)
            throw new ArgumentNullException("exception");

         if (key == null)
            throw new ArgumentNullException("key");

         /* Key or value is not serializable (or key is null). The default internal structure which
         * implements the IDictionary is going to throw an exception in Add() so instead of 
         * throwing another exception while preparing to throw the first one we silently ignore the
         * error. Unless we're building in debug mode that is, then we'll fail. */
         if (value != null && !value.GetType().IsSerializable)
         {
            //2011.02.07 10:05 - LAK - mod:ha nem lenne szerializálható, akkor próbáljuk meg bepakolni a ToStringjét
            if (value != null)
            { exception.Data.Add(key, value.ToString()); }
            else
            {
               exception.Data.Add(key, "[NULL!]");
            }
            //Debug.Fail("Attempt to add non-serializable value to exception data");
         }
         else
         {
            exception.Data.Add(key, value);
         }

         return exception;
      }
   }
}