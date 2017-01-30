// LispSharp Lisp Interpreter
// Curtis Wilson (c) 2016
// LispInterpreter.Strings.cs
/* Stores the constant strings used by the interpreter. */

namespace LispInterpreter {
	partial class LispInterpreter {
		private const string ERR_NOT_A_LIST = "{0} is not a list.";
		private const string ERR_NOT_A_NUMBER = "{0} is not a numeric atom.";
		private const string ERR_NOT_A_SYMBOL = "{0} is not a symbolic atom.";
		private const string ERR_UNDEFINED_FUNCTION = "Undefined function {0}.";
        private const string ERR_UNDEFINED_VARIABLE = "Variable {0} is undefined.";
		private const string ERR_INVALID_NUMBER_OF_ARGUMENTS = "Wrong number of arguments for function {0}.";
		private const string ERR_UNEXPECTED_LIST_CLOSURE = "Unexpected ).";
        private const string ERR_ODD_NUMBER_OF_ARGUMENTS = "{0}: odd number of arguments: {1}.";
		private const string ERR_INVALID_LIST_ENDING = "A proper list cannot end with {0}.";
		private const string ERR_DIVISION_BY_ZERO = "Cannot divide by zero.";
	}
}
