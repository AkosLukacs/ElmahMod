using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections;

namespace ElmahModSampleSite
{
   /// <summary>
   /// A few sample exceptions
   /// </summary>
   public class ThrowException
   {
      /// <summary>
      /// Throw an Exception with a singel Data item
      /// </summary>
      /// <param name="parameter"></param>
      public void ThrowWithData(string parameter)
      {
         try
         {
            TryToDoSomething(parameter);
         }
         catch (Exception ex)
         {

            var toThrow = new Exception("Something terrible happened during an inner operation, see InnerException for details.", ex);
            toThrow.Data.Add("parameter", parameter);
            throw toThrow;
         }
      }

      /// <summary>
      /// Throw an Exception with an Inner Exception
      /// </summary>
      public void ThrowWithInner()
      {
         try
         {
            TryToDoSomething("oops");
         }
         catch (Exception ex)
         {

            throw new Exception("Something terrible happened during an inner operation, see InnerException for details.", ex);
         }
      }

      /// <summary>
      /// Throws a simple exception
      /// </summary>
      /// <param name="parameter"></param>
      private void TryToDoSomething(string parameter)
      {
         throw new ArgumentOutOfRangeException("parameter", parameter, "Hey, this is not valid!");
      }


      /// <summary>
      /// Just a demonstration of throwing a complex Exception with inner exceptions, and some Data
      /// </summary>
      /// <param name="parameter"></param>
      internal void ThrowComplex(string parameter)
      {

         List<string> listToAdd = new List<string> { 
                                        "first list item",
                                        "another list item"
                                    };

         Dictionary<string, string> dictToAdd = new Dictionary<string, string>{
                                                    {"one element", "one value"},
                                                    {"another element", "another value"},
                                                    {"third element", "third value"}
                                                };

         Hashtable ht = new Hashtable{
                                {"htElem1", 1},
                                {"htElem2", DateTime.Now}                        
                            };

         Exception toRaise =
                 new Exception("This is the outermost exception",
                     new InvalidCastException("This is the first innerException",
                         new ArgumentOutOfRangeException("This is the second inner exception")
                                                         .AddData("data in the AOORException", 123))
                                             .AddData("Data in the first Inner Exception", "x"))
                           .AddData("parameter", parameter)
                           .AddData("data1", "d1")
                           .AddData("and the Hashtable", ht)
                           .AddData("data2", "data 2")
                           .AddData("this is a list", listToAdd)
                           .AddData("A fourth data item", "this is really important data!!!")
                           .AddData("and a Dictionary", dictToAdd)
                           ;

         throw toRaise;
      }
   }
}