// LispSharp Lisp Interpreter
// Curtis Wilson (c) 2016
// LispInterpreter.LispException.cs
using System;

namespace LispInterpreter {
	partial class LispInterpreter {
		// Custom exception class for the LispInterpreter.
		public class LispException : Exception {
			public LispException(string message) : base(message) { }
		}
	}
}
