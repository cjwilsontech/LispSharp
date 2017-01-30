// LispSharp Lisp Interpreter
// Curtis Wilson (c) 2016
// Program.cs
/* Wrapper program to demonstrate the LispInterpreter class. */

using System;
using System.IO;

namespace LispSharp {
	class Program {
		static void Main(string[] args) {
			string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
			Console.WriteLine(string.Format("LispSharp Common Lisp Interpreter {0}\n", version));

			byte[] inputBuffer = new byte[2048];
			Console.SetIn(new StreamReader(Console.OpenStandardInput(inputBuffer.Length), Console.InputEncoding, false, inputBuffer.Length));

			// Create a new instance of the Lisp interpreter.
			LispInterpreter.LispInterpreter lisp = new LispInterpreter.LispInterpreter();
			uint count = 0;

			// Perform the Read-Eval-Print loop.
			while (lisp.Continue) {
				++count;
				Console.Write("[{0}]> ", count);
				lisp.Read(Console.ReadLine());

				string output = lisp.Eval();
				if (output != string.Empty)
					Console.WriteLine(output);
			}
		}
	}
}
