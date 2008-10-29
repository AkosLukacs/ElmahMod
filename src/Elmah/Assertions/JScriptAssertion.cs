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
    using System.Collections;
    using System.Collections.Specialized;
    using System.IO;
    using System.Reflection;
    using System.Security;
    using System.Security.Permissions;
    using System.Text.RegularExpressions;
    using Microsoft.JScript;
    using Microsoft.JScript.Vsa;
    using Convert=Microsoft.JScript.Convert;

    #endregion

    /// <summary>
    /// An assertion implementation that uses a JScript expression to
    /// determine the outcome.
    /// </summary>
    /// <remarks>
    /// Each instance of this type maintains a separate copy of the JScript 
    /// engine so use it sparingly. For example, instead of creating several
    /// objects, each with different a expression, try and group all
    /// expressions that apply to particular context into a single compound 
    /// JScript expression using the conditional-OR (||) operator.
    /// </remarks>

    public sealed class JScriptAssertion : IAssertion
    {
        private static readonly Regex _directiveExpression = new Regex(
            @"^ \s* // \s* @([a-zA-Z]+)", 
            RegexOptions.IgnorePatternWhitespace
            | RegexOptions.Singleline 
            | RegexOptions.CultureInvariant
            | RegexOptions.IgnoreCase);

        private readonly EvaluationStrategy _evaluationStrategy;

        public JScriptAssertion(string expression) : 
            this(expression, null, null) {}

        public JScriptAssertion(string expression, string[] assemblyNames, string[] imports)
        {
            if (expression == null
                || expression.Length == 0
                || expression.TrimStart().Length == 0)
            {
                return;
            }

            ProcessDirectives(expression, ref assemblyNames, ref imports);

            VsaEngine engine = VsaEngine.CreateEngineAndGetGlobalScope(/* fast */ false, 
                assemblyNames != null ? assemblyNames : new string[0]).engine;

            if (imports != null && imports.Length > 0)
            {
                foreach (string import in imports)
                    Import.JScriptImport(import, engine);
            }

            //
            // We pick on of two expression evaluation strategies depending 
            // on the level of trust available. The full trust version is
            // faster as it compiles the expression once into a JScript 
            // function and then simply invokes at the time it needs to
            // evaluate the context. The partial trust strategy is slower
            // as it compiles the expression each time an evaluation occurs
            // using the JScript eval.
            //

            _evaluationStrategy = FullTrustEvaluationStrategy.IsApplicable() ? (EvaluationStrategy)
                new FullTrustEvaluationStrategy(expression, engine) : 
                new PartialTrustEvaluationStrategy(expression, engine);
        }

        public JScriptAssertion(NameValueCollection settings) :
            this(settings["expression"], 
                 settings.GetValues("assembly"), 
                 settings.GetValues("import")) {}

        public bool Test(object context)
        {
            if (context == null) 
                throw new ArgumentNullException("context");

            return _evaluationStrategy != null && 
                   _evaluationStrategy.Eval(context);
        }

        private static void ProcessDirectives(string expression, ref string[] assemblyNames, ref string[] imports)
        {
            Debug.Assert(expression != null);

            ArrayList assemblyNameList = null, importList = null;

            using (StringReader reader = new StringReader(expression))
            {
                string line;
                int lineNumber = 0;

                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;

                    if (line.Trim().Length == 0)
                        continue;

                    Match match = _directiveExpression.Match(line);
                    
                    if (!match.Success) // Exit processing on first non-match
                        break;

                    string directive = match.Groups[1].Value;
                    string tail = line.Substring(match.Index + match.Length).Trim();

                    try
                    {
                        switch (directive)
                        {
                            case "assembly": assemblyNameList = AddDirectiveParameter(directive, tail, assemblyNameList, assemblyNames); break;
                            case "import": importList = AddDirectiveParameter(directive, tail, importList, imports); break;
                            default:
                                throw new FormatException(string.Format("'{0}' is not a recognized directive.", directive));
                        }
                    }
                    catch (FormatException e)
                    {
                        throw new ArgumentException(
                            string.Format(
                                "Error processing directives section (lead comment) of the JScript expression (see line {0}). {1}", 
                                lineNumber.ToString("N0"), e.Message), 
                            "expression");
                    }
                }
            }

            assemblyNames = ListOrElseArray(assemblyNameList, assemblyNames);
            imports = ListOrElseArray(importList, imports);
        }

        private static ArrayList AddDirectiveParameter(string directive, string parameter, ArrayList list, string[] inits)
        {
            Debug.AssertStringNotEmpty(directive);
            Debug.Assert(parameter != null);

            if (parameter.Length == 0)
                throw new FormatException(string.Format("Missing parameter for {0} directive.", directive));

            if (list == null)
            {
                list = new ArrayList(/* capacity */ (inits != null ? inits.Length : 0) + 4);
                if (inits != null) list.AddRange(inits);
            }

            list.Add(parameter);
            return list;
        }

        private static string[] ListOrElseArray(ArrayList list, string[] array)
        {
            return list != null ? (string[]) list.ToArray(typeof(string)) : array;
        }

        private abstract class EvaluationStrategy
        {
            private readonly VsaEngine _engine;

            protected EvaluationStrategy(VsaEngine engine)
            {
                Debug.Assert(engine != null);
                _engine = engine;
            }

            public VsaEngine Engine { get { return _engine; } }
            public abstract bool Eval(object context);
        }

        /// <summary>
        /// Uses the JScript eval function to compile and evaluate the
        /// expression against the context on each evaluation.
        /// </summary>
        
        private sealed class PartialTrustEvaluationStrategy : EvaluationStrategy
        {
            private readonly string _expression;
            private readonly GlobalScope _scope;
            private readonly FieldInfo _contextField;

            public PartialTrustEvaluationStrategy(string expression, VsaEngine engine)
                : base(engine)
            {
                //
                // Following is equivalent to declaring a "var" in JScript
                // at the level of the Global object.
                //

                _scope = (GlobalScope)engine.GetGlobalScope().GetObject();
                _contextField = _scope.AddField("$context");
                _expression = expression;
            }

            public override bool Eval(object context)
            {
                VsaEngine engine = Engine;

                //
                // Following is equivalent to calling eval in JScript,
                // with the value of the context variable established at the
                // global scope in order for it to be available to the 
                // expression source.
                //

                _contextField.SetValue(_scope, context);

                try
                {
                    With.JScriptWith(context, engine);
                    return Convert.ToBoolean(Microsoft.JScript.Eval.JScriptEvaluate(_expression, engine));
                }
                finally
                {
                    engine.PopScriptObject(/* with */);
                }
            }
        }

        /// <summary>
        /// Compiles the given expression into a JScript function at time of 
        /// construction and then simply invokes it during evaluation, using
        /// the context as a parameter.
        /// </summary>

        private sealed class FullTrustEvaluationStrategy : EvaluationStrategy
        {
            private readonly object _function;
            
            public FullTrustEvaluationStrategy(string expression, VsaEngine engine)
                : base(engine)
            {
                //
                // Equivalent to following in JScript:
                // new Function('$context', 'with ($context) return (' + expression + ')');
                //
                // IMPORTANT! Leave the closing parentheses surrounding the 
                // return expression on a separate line. This is to guard
                // against someone using a double-slash (//) to comment out 
                // the remainder of an expression.
                //

                const string context = "$context";

                _function = LateBinding.CallValue(
                    DefaultThisObject(engine), 
                    engine.LenientGlobalObject.Function, 
                    new object[] {
                        /* parameters */ context, /* body... */ @"
                        with (" + context + @") {
                            return (
                                " + expression + @"
                            );
                        }"
                    }, 
                    /* construct */ true, /* brackets */ false, engine);
            }

            public override bool Eval(object context)
            {
                //
                // Following is equivalent to calling apply in JScript.
                // See http://msdn.microsoft.com/en-us/library/84ht5z59.aspx.
                //

                object result = LateBinding.CallValue(
                    DefaultThisObject(Engine), 
                    _function, /* args */ new object[] { context }, 
                    /* construct */ false, /* brackets */ false, Engine);
                
                return Convert.ToBoolean(result);
            }

            private static object DefaultThisObject(VsaEngine engine)
            {
                Debug.Assert(engine != null);
                return ((IActivationObject) engine.ScriptObjectStackTop()).GetDefaultThisObject();
            }

            public static bool IsApplicable()
            {
                try
                {
                    //
                    // FullTrustEvaluationStrategy uses Microsoft.JScript.GlobalObject.Function.CreateInstance,
                    // which requires unmanaged code permission...
                    //

                    new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
                    return true;
                }
                catch (SecurityException)
                {
                    return false;
                }
            }
        }
    }
}
