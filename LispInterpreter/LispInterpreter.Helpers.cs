// LispSharp Lisp Interpreter
// Curtis Wilson (c) 2016
// LispInterpreter.Helpers.cs
/* Defines helper functions used by the interpreter. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

// Lisp Functions
namespace LispInterpreter {
	partial class LispInterpreter {

		private static List<LispDataType> ProcessInputBuffer(ref string input) {
			List<LispDataType> result = new List<LispDataType>();
			
			input = FulfillMacro(input, '\'', "QUOTE");

			MatchCollection matches = LispGeneralRegex.Matches(input.ToUpper());

			int depth = 0;
			string listString = string.Empty;
			
			foreach (Match match in matches) {
				string matchString = match.Value;

				if (matchString == "(") {
					++depth;
					listString = CombineStringsWithSpace(listString, matchString);

				} else if (matchString == ")") {
					--depth;
					listString = CombineStringsWithSpace(listString, matchString);
					if (depth < 0) throw new LispException(ERR_UNEXPECTED_LIST_CLOSURE);
					else if (depth == 0) {
						result.Add(new LispList(listString));
						listString = string.Empty;
					}

				} else if (depth == 0) {
					if (matchString == "NIL") {
						result.Add(new LispList());
					} else if (LispNumericAtomRegex.IsMatch(matchString)) {
						result.Add(new LispNumericAtom(matchString));
					} else {
						result.Add(new LispSymbolicAtom(matchString));
					}
					listString = string.Empty;
				} else {
					listString = CombineStringsWithSpace(listString, matchString);
				}
			}

			input = listString;
			return result;
		}

		private static string FulfillMacro(string input, char macroOriginal, string macroReplacement) {

			int targetIndex = input.IndexOf(macroOriginal);
			while (targetIndex != -1) {
				int i, depth = 0;
				bool wordStarted = false;
				for (i = targetIndex + 1; i < input.Count(); ++i) {
					if (input[i] == '(') {
						++depth;
					} else if (depth == 0) {
						if (wordStarted && (input[i] == ' ' || input[i] == ')')) {
							break;
						} else if (input[i] >= 'A' || input[i] <= 'Z' || input[i] >= '0' || input[i] <= '9' || input[i] == '+' || input[i] == '/' || input[i] == '-' || input[i] == '*' || input[i] == '\'') {
							wordStarted = true;
						}
					} else if (input[i] == ')') {
						--depth;
						if (depth == 0) break;
					}
				}
				if (i != input.Count() || wordStarted) {
					input = input.Insert(i, ")");
					input = input.Remove(targetIndex, 1);
					input = input.Insert(targetIndex, "(" + macroReplacement + " ");
					targetIndex = input.IndexOf(macroOriginal);
				} else break;
			}

			return input;
		}

		private static string CombineStringsWithSpace(string s1, string s2) {
			string result = s1;
			if (s1 != string.Empty && s1.Last() != '(' && s2.First() != ')') result += " ";
			return result + s2;
		}

		private static dynamic max(dynamic a, dynamic b) {
			return (a > b) ? a : b;
		}
	}
}
