// LispSharp Lisp Interpreter
// Curtis Wilson (c) 2016
// LispInterpreter.LispUserFunction.cs
/* Defines the class used to store and evaluate user-defined functions. */

using System;
using System.Collections.Generic;

namespace LispInterpreter {
	partial class LispInterpreter {
		private class LispUserFunction {
			private string name;
			private List<LispSymbolicAtom> args;
			private LispDataType func;
			public LispUserFunction(string name, List<LispSymbolicAtom> arguments, LispDataType function) {
				args = arguments;
				func = function;
				this.name = name;
			}

			public LispDataType Evaluate(List<LispDataType> arguments, LispInterpreter context) {
				// replace function arguments
				if (arguments.Count != args.Count) throw new LispException(String.Format(ERR_INVALID_NUMBER_OF_ARGUMENTS, name));

				if (func is LispNumericAtom) {
					return func.Evaluate(context);
				} else if (func is LispList) {

					// Create the argument replacement map.
					Dictionary<string, LispDataType> argReplacements = new Dictionary<string, LispDataType>();
					for (int i = 0; i < args.Count; ++i) {
						LispDataType d = arguments[i].Evaluate(context);
						if (d is LispList) argReplacements[args[i].Value] = d.Copy();
						else argReplacements[args[i].Value] = d;
					}

					// Replace the arguments and evaluate.
					return ((LispList)func).Replace(argReplacements, false).Evaluate(context);
				} else {
					if (args.Count >= 1 && args[0].Value == ((LispSymbolicAtom)func).Value) {
						return arguments[0].Evaluate(context);
					} else
						return func.Evaluate(context);
				}
			}
		}
	}
}