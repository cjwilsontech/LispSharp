// LispSharp Lisp Interpreter
// Curtis Wilson (c) 2016
// LispInterpreter.cs
/* Defines the class which performs the top-level operations of the interpreter. */

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LispInterpreter {
	public partial class LispInterpreter {
		private bool exit = false;
		private Queue<LispDataType> CommandQueue = new Queue<LispDataType>();
		protected string buffer = string.Empty;
		private Dictionary<string, LispDataType> LispGlobals = new Dictionary<string, LispDataType>();
		private Dictionary<string, LispUserFunction> LispUserFunctions = new Dictionary<string, LispUserFunction>();

		// Regex to assist in parsing input strings to Lisp datatypes.
		private static readonly Regex LispGeneralRegex = new Regex(@"([A-Z+\-*/\d<>=.?!@%:#$,&|_\\]+|\(|\)|'|\d+(\.\d+)?)");
		private static readonly Regex LispNumericAtomRegex = new Regex(@"^-?\d*\.?\d*$(?<=\d)");
		private static readonly Regex LispSymbolicAtomRegex = new Regex(@"^[A-Z+\-*/'\d<>=.?!@%:#$,&|_\\]+$");

		// Returns if the LispInterpreter should continue.
		public bool Continue { get { return !exit; } }

		public LispInterpreter() {
			// Intialize predefined globals.
			LispGlobals["PI"] = new LispNumericAtom("3.14159265358979323846");
		}

		// Takes an input string and pushes onto the execution buffer.
		public void Read(string input) {
			int comment = input.IndexOf(';');

			// No comment found, push onto the command buffer.
			if (comment == -1) buffer += input;

			// Ignore the comment by pushing everything before onto the command buffer.
			else buffer += input.Substring(0, comment);
		}

		// Creates Lisp datatypes out of the command buffer and executes them.
		// Incomplete lists will be preserved in the buffer.
		public string Eval() {
			string outputString = string.Empty;
			
			try {
				// Parse the list.
				foreach (LispDataType data in ProcessInputBuffer(ref buffer))
					CommandQueue.Enqueue(data);

			} catch (LispException e) {
				outputString += e.Message;
				buffer = string.Empty;
			}

			try {

				while (CommandQueue.Count > 0) {
					LispDataType command = CommandQueue.Dequeue();

					// Exit command.
					if (command is LispList) {
						LispList cmd = (LispList)command;
						if (cmd.Count >= 1 && cmd.First().ToString() == "EXIT") {
							exit = true;
							return outputString;
						}
					}

					// Evaluate the command.
					outputString += command.Evaluate(this);

					if (CommandQueue.Count > 0)
						outputString += '\n';
				}
			} catch (LispException e) {
				outputString += e.Message;
				CommandQueue.Clear();
			}

			return outputString;
		}
	}
}
