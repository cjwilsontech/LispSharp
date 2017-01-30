// LispSharp Lisp Interpreter
// Curtis Wilson (c) 2016
// LispInterpreter.LispFunctions.cs
/* Holds methods used for calling functions, and the class holding the C# implementation
 * of the built-in Lisp functions. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

// Lisp Functions
namespace LispInterpreter {
	partial class LispInterpreter {

		// Regex to match C...R functions (CAR, CDR).
		static readonly Regex crRegex = new Regex(@"^C([DA]+)R$");

		// Evaluates and verifies that the arguments are of the correct number and type.
		private List<LispDataType> CheckLispArguments(string fname, List<Type> types, List<LispDataType> input) {
			List<LispDataType> result = new List<LispDataType>();

			// If the number of arguments are different, they are invalid.
			if (types.Count != input.Count) throw new LispException(String.Format(ERR_INVALID_NUMBER_OF_ARGUMENTS, fname));

			for (int i = 0; i < types.Count; ++i) {

				Type inputType = input[i].Evaluate(this).GetType();

				if (inputType != types[i] && !(types[i] == typeof(LispDataType) && (inputType == typeof(LispList) || inputType == typeof(LispNumericAtom) || inputType == typeof(LispSymbolicAtom)))) {

					// The input type does not match the expected type.
					if (types[i] == typeof(LispList)) throw new LispException(String.Format(ERR_NOT_A_LIST, input[i].ToString()));
					if (types[i] == typeof(LispNumericAtom)) throw new LispException(String.Format(ERR_NOT_A_NUMBER, input[i].ToString()));
					if (types[i] == typeof(LispSymbolicAtom)) throw new LispException(String.Format(ERR_NOT_A_SYMBOL, input[i].ToString()));
					throw new Exception("Unknown type.");

					// The input needs to be evaluated before it should be accepted as input.
				} else result.Add(input[i].Evaluate(this));
			}

			return result;
		}

		// Given the contents of a list, ExecuteLispFunction will execute the corresponding function.
		private LispDataType ExecuteLispFunction(List<LispDataType> list) {

			if (list.Count == 0) return new LispList();

			int argCount = list.Count - 1;
			List<LispDataType> args = list.GetRange(1, list.Count - 1);
			List<LispDataType> verifiedArgs;
			List<LispDataType> argList = list.GetRange(1, argCount);
			string fname = list.First().ToString();

			// Lisp functions are called here. The name in the case is the name that the user will use to call the function.
			switch (fname) {

				case "+":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom), typeof(LispNumericAtom) }, argList);
					return add((LispNumericAtom)verifiedArgs[0], (LispNumericAtom)verifiedArgs[1]);

				case "-":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom), typeof(LispNumericAtom) }, argList);
					return subtract((LispNumericAtom)verifiedArgs[0], (LispNumericAtom)verifiedArgs[1]);

				case "*":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom), typeof(LispNumericAtom) }, argList);
					return multiply((LispNumericAtom)verifiedArgs[0], (LispNumericAtom)verifiedArgs[1]);

				case "/":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom), typeof(LispNumericAtom) }, argList);
					return divide((LispNumericAtom)verifiedArgs[0], (LispNumericAtom)verifiedArgs[1]);

				case "1+":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom) }, argList);
					return (LispNumericAtom)verifiedArgs[0] + 1;

				case "1-":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom) }, argList);
					return (LispNumericAtom)verifiedArgs[0] - 1;

				case "DEFUN":
					if (argCount != 3) throw new LispException(string.Format(ERR_INVALID_NUMBER_OF_ARGUMENTS, fname));
					if (argList[0] is LispSymbolicAtom && argList[1] is LispList)
						return defun((LispSymbolicAtom)argList[0], (LispList)argList[1], argList[2], this);
					else {
						if (argList[0] is LispSymbolicAtom) throw new LispException(string.Format(ERR_NOT_A_LIST, argList[1]));
						else throw new LispException(string.Format(ERR_NOT_A_SYMBOL, argList[0].ToString()));
					}

				case "FIRST":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispList) }, argList);
					return car((LispList)verifiedArgs[0]);

				case "LAST":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispList) }, argList);
					return last((LispList)verifiedArgs[0]);

				case "REST":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispList) }, argList);
					return cdr((LispList)verifiedArgs[0]);

				case "QUOTE":
					if (argCount != 1) throw new LispException(String.Format(ERR_INVALID_NUMBER_OF_ARGUMENTS, fname));
					return quote(args[0]);

				case "SET":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispSymbolicAtom), typeof(LispDataType) }, argList);
					return set((LispSymbolicAtom)verifiedArgs[0], verifiedArgs[1]);

				case "EVAL":
					if (argCount != 1) throw new LispException(string.Format(ERR_INVALID_NUMBER_OF_ARGUMENTS, fname));
					return eval(args[0]);

				case "SETQ":
					List<LispSymbolicAtom> variables = new List<LispSymbolicAtom>();
					List<LispDataType> values = new List<LispDataType>();
					for (int i = 0; i < args.Count; ++i) {
						if (i % 2 == 0) {
							if (args[i] is LispSymbolicAtom)
								variables.Add((LispSymbolicAtom)args[i]);
							else throw new LispException(string.Format(ERR_NOT_A_SYMBOL, args[i].ToString()));
						} else {
							values.Add(args[i]);
						}
					}
					return setq(variables, values);

				case "NULL":
				case "ENDP":
					if (argCount != 1) throw new LispException(string.Format(ERR_INVALID_NUMBER_OF_ARGUMENTS, fname));
					return isNull(args[0]);

				case "ATOM":
					if (argCount != 1) throw new LispException(string.Format(ERR_INVALID_NUMBER_OF_ARGUMENTS, fname));
					return isAtom(args[0]);

				case "LISTP":
					if (argCount != 1) throw new LispException(string.Format(ERR_INVALID_NUMBER_OF_ARGUMENTS, fname));
					return isList(args[0]);

				case "LIST":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispDataType) }, argList);
					return makeList(verifiedArgs[0]);

				case "APPEND":
					if (argCount == 0) return new LispList();
					if (argCount == 1) return args[0];
					if (!(args[0].Evaluate(this) is LispList)) throw new LispException(string.Format(ERR_NOT_A_LIST, args[0]));
					return append((LispList)args[0].Evaluate(this), args.GetRange(1, args.Count - 1));

				case "COND":
					List<LispList> cases = new List<LispList>();
					foreach (LispDataType data in args) {
						if (data is LispList) cases.Add((LispList)data);
						else throw new LispException(string.Format(ERR_NOT_A_LIST, data));
					}
					return cond(cases);

				case "IF":
					if (argCount == 2) return lispIf(args[0], args[1]);
					else if (argCount == 3) return lispIf(args[0], args[1], args[2]);
					else throw new LispException(string.Format(ERR_INVALID_NUMBER_OF_ARGUMENTS, fname));

				case "EQ":
				case "EQL":
					if (argCount != 2) throw new LispException(string.Format(ERR_INVALID_NUMBER_OF_ARGUMENTS));
					return eql(args[0], args[1]);

				case "EQUAL":
					if (argCount != 2) throw new LispException(string.Format(ERR_INVALID_NUMBER_OF_ARGUMENTS));
					return equal(args[0], args[1]);

				case "NOT":
					if (argCount != 1) throw new LispException(string.Format(ERR_INVALID_NUMBER_OF_ARGUMENTS));
					return not(args[0]);

				case "AND":
					return and(args);

				case "OR":
					return or(args);

				case "CONS":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispDataType), typeof(LispDataType) }, argList);
					return cons(verifiedArgs[0], verifiedArgs[1]);

				case "SYMBOLP":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispDataType) }, argList);
					return symbolp(verifiedArgs[0]);

				case "NUMBERP":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispDataType) }, argList);
					return numberp(verifiedArgs[0]);

				case "MINUSP":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom) }, argList);
					return minusp((LispNumericAtom)verifiedArgs[0]);

				case "PLUSP":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom) }, argList);
					return plusp((LispNumericAtom)verifiedArgs[0]);

				case "ZEROP":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom) }, argList);
					return zerop((LispNumericAtom)verifiedArgs[0]);

				case "LIST-LENGTH":
				case "LENGTH":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispList) }, argList);
					return listlength((LispList)verifiedArgs[0]);

				case "BOUNDP":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispSymbolicAtom) }, argList);
					return boundp((LispSymbolicAtom)verifiedArgs[0]);

				case "MAKUNBOUND":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispSymbolicAtom) }, argList);
					return makunbound((LispSymbolicAtom)verifiedArgs[0]);

				case "REVERSE":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispList) }, argList);
					return reverse((LispList)verifiedArgs[0]);

				case "MEMBER":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispDataType), typeof(LispList) }, argList);
					return member(verifiedArgs[0], (LispList)verifiedArgs[1]);

				case "REMOVE":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispDataType), typeof(LispList) }, argList);
					return remove(verifiedArgs[0], (LispList)verifiedArgs[1]);

				case "SUBST":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispDataType), typeof(LispDataType), typeof(LispList) }, argList);
					return subst(verifiedArgs[0], verifiedArgs[1], (LispList)verifiedArgs[2]);

				case "=":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom), typeof(LispNumericAtom) }, argList);
					return equals((LispNumericAtom)verifiedArgs[0], (LispNumericAtom)verifiedArgs[1]);

				case "/=":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom), typeof(LispNumericAtom) }, argList);
					return notequals((LispNumericAtom)verifiedArgs[0], (LispNumericAtom)verifiedArgs[1]);

				case ">=":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom), typeof(LispNumericAtom) }, argList);
					return morethanequals((LispNumericAtom)verifiedArgs[0], (LispNumericAtom)verifiedArgs[1]);

				case "<=":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom), typeof(LispNumericAtom) }, argList);
					return lessthanequals((LispNumericAtom)verifiedArgs[0], (LispNumericAtom)verifiedArgs[1]);

				case "<":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom), typeof(LispNumericAtom) }, argList);
					return lessthan((LispNumericAtom)verifiedArgs[0], (LispNumericAtom)verifiedArgs[1]);

				case ">":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom), typeof(LispNumericAtom) }, argList);
					return morethan((LispNumericAtom)verifiedArgs[0], (LispNumericAtom)verifiedArgs[1]);

				case "DOLIST":
					if (argCount != 2) throw new LispException(string.Format(ERR_INVALID_NUMBER_OF_ARGUMENTS, fname));
					if (!(args[0] is LispList)) throw new LispException(string.Format(ERR_NOT_A_LIST, args[0]));
					return dolist((LispList)args[0], args[1]);

				case "DOTIMES":
					if (argCount != 2) throw new LispException(string.Format(ERR_INVALID_NUMBER_OF_ARGUMENTS, fname));
					if (!(args[0] is LispList)) throw new LispException(string.Format(ERR_NOT_A_LIST, args[0]));
					return dotimes((LispList)args[0], args[1]);

				case "MIN":
				case "MAX":
				case "GCD":
					if (argCount < 1) throw new LispException(string.Format(ERR_INVALID_NUMBER_OF_ARGUMENTS, fname));
					List<LispNumericAtom> arguments = new List<LispNumericAtom>();
					foreach (LispDataType data in args) {
						if (data.Evaluate(this) is LispNumericAtom) arguments.Add((LispNumericAtom)data.Evaluate(this));
						else throw new LispException(string.Format(ERR_NOT_A_NUMBER, data));
					}
					if (fname == "MIN") return min(arguments);
					else if (fname == "MAX") return max(arguments);
					else return gcd(arguments);

				case "SQRT":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom) }, argList);
					return sqrt((LispNumericAtom)verifiedArgs[0]);

				case "SIN":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom) }, argList);
					return sin((LispNumericAtom)verifiedArgs[0]);

				case "COS":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom) }, argList);
					return cos((LispNumericAtom)verifiedArgs[0]);

				case "TAN":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom) }, argList);
					return tan((LispNumericAtom)verifiedArgs[0]);

				case "ASIN":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom) }, argList);
					return asin((LispNumericAtom)verifiedArgs[0]);

				case "ACOS":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom) }, argList);
					return acos((LispNumericAtom)verifiedArgs[0]);

				case "ATAN":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom) }, argList);
					return atan((LispNumericAtom)verifiedArgs[0]);

				case "MOD":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom), typeof(LispNumericAtom) }, argList);
					return mod((LispNumericAtom)verifiedArgs[0], (LispNumericAtom)verifiedArgs[1]);

				case "ABS":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom) }, argList);
					return abs((LispNumericAtom)verifiedArgs[0]);

				case "ROUND":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom) }, argList);
					return round((LispNumericAtom)verifiedArgs[0]);

				case "RANDOM":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom) }, argList);
					return random((LispNumericAtom)verifiedArgs[0]);

				case "EXP":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom) }, argList);
					return exp((LispNumericAtom)verifiedArgs[0]);

				case "EXPT":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispNumericAtom), typeof(LispNumericAtom) }, argList);
					return expt((LispNumericAtom)verifiedArgs[0], (LispNumericAtom)verifiedArgs[1]);

				case "LOG":
					if (argCount == 1) {
						if (args[0].Evaluate(this) is LispNumericAtom) return log((LispNumericAtom)args[0]);
						throw new LispException(string.Format(ERR_NOT_A_NUMBER, args[0]));
					} else if (argCount == 2) {
						if (args[0].Evaluate(this) is LispNumericAtom && args[1].Evaluate(this) is LispNumericAtom)
							return log((LispNumericAtom)args[0].Evaluate(this), (LispNumericAtom)args[1].Evaluate(this));
						if (args[1].Evaluate(this) is LispNumericAtom) throw new LispException(string.Format(ERR_NOT_A_NUMBER, args[0]));
						throw new LispException(string.Format(ERR_NOT_A_NUMBER, args[1]));
					} else throw new LispException(string.Format(ERR_INVALID_NUMBER_OF_ARGUMENTS, fname));


				case "LOAD":
					verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispSymbolicAtom) }, argList);
					return load((LispSymbolicAtom)verifiedArgs[0]);


				default:
					// CAR/CDR
					MatchCollection crMatches = crRegex.Matches(fname);
					if (crMatches.Count != 0) {
						verifiedArgs = CheckLispArguments(fname, new List<Type> { typeof(LispList) }, argList);
						LispDataType result = verifiedArgs[0];
						string operations = crMatches[0].Groups[1].Value;
						for (int i = 0; i < operations.Count(); ++i) {
							char operation = operations[operations.Count() - 1 - i];
							if (result is LispList) {
								if (operation == 'A') {
									result = car((LispList)result);
								} else {
									result = cdr((LispList)result);
								}
							} else throw new LispException(string.Format(ERR_NOT_A_LIST, result));
						}
						result.SetLiteral(true);
						return result;
					}

					// User-defined functions.
					if (LispUserFunctions.ContainsKey(fname)) return LispUserFunctions[fname].Evaluate(args, this);

					throw new LispException(String.Format(ERR_UNDEFINED_FUNCTION, fname));
			}
		}

		// Definitions of all the C# implementations of built-in Lisp functions.
		private LispDataType load(LispSymbolicAtom sPath) {
			string path = sPath.Evaluate(this).ToString();
			int fileExtension = path.LastIndexOf('.');
			if (fileExtension == -1) path += ".lsp";

			if (File.Exists(path)) {
				StreamReader fin = new StreamReader(path);
				string line;
				while ((line = fin.ReadLine()) != null) {
					int comment = line.IndexOf(';');

					// No comment found, push onto the command buffer.
					if (comment == -1) Read(' ' + line + ' ');

					// Ignore the comment by pushing everything before onto the command buffer.
					else Read(' ' + line.Substring(0, comment) + ' ');

				}
				fin.Close();
				Eval();
				return new LispSymbolicAtom("T");
			} else return new LispList();
		}

		private LispNumericAtom random(LispNumericAtom limit) {
			if (limit.IsDecimal) return new LispNumericAtom((new Random().NextDouble() * limit).ToString());
			return new LispNumericAtom(new Random().Next() % limit);
		}

		private LispNumericAtom round(LispNumericAtom number) {
			return number.Round();
		}

		private LispNumericAtom gcd(IEnumerable<LispNumericAtom> numbers) {
			return numbers.Aggregate(gcd);
		}
		private LispNumericAtom gcd(LispNumericAtom a, LispNumericAtom b) {
			return b == 0 ? a : gcd(b, a % b);
		}

		private LispNumericAtom expt(LispNumericAtom number, LispNumericAtom power) {
			return new LispNumericAtom(Math.Pow(number, power).ToString());
		}

		private LispNumericAtom exp(LispNumericAtom number) {
			return new LispNumericAtom(Math.Exp(number).ToString());
		}

		private LispNumericAtom log(LispNumericAtom number, LispNumericAtom baseNumber = null) {
			if (baseNumber == null) return new LispNumericAtom(Math.Log(number).ToString());
			return new LispNumericAtom(Math.Log(number, baseNumber).ToString());
		}

		private LispNumericAtom sqrt(LispNumericAtom number) {
			return new LispNumericAtom(Math.Sqrt(number).ToString());
		}

		private LispNumericAtom abs(LispNumericAtom number) {
			return LispNumericAtom.abs(number);
		}

		private LispNumericAtom sin(LispNumericAtom number) {
			return new LispNumericAtom(Math.Sin(number).ToString());
		}

		private LispNumericAtom cos(LispNumericAtom number) {
			return new LispNumericAtom(Math.Cos(number).ToString());
		}

		private LispNumericAtom tan(LispNumericAtom number) {
			return new LispNumericAtom(Math.Tan(number).ToString());
		}

		private LispNumericAtom asin(LispNumericAtom number) {
			return new LispNumericAtom(Math.Asin(number).ToString());
		}

		private LispNumericAtom acos(LispNumericAtom number) {
			return new LispNumericAtom(Math.Acos(number).ToString());
		}

		private LispNumericAtom atan(LispNumericAtom number) {
			return new LispNumericAtom(Math.Atan(number).ToString());
		}

		private LispNumericAtom mod(LispNumericAtom n1, LispNumericAtom n2) {
			return n1 % n2;
		}

		private LispNumericAtom min(IEnumerable<LispNumericAtom> numbers) {
			return numbers.Min();
		}

		private LispNumericAtom max(IEnumerable<LispNumericAtom> numbers) {
			return numbers.Max();
		}

		private LispDataType dotimes(LispList args, LispDataType body) {
			if (args.Count != 2 && args.Count != 3) throw new LispException(string.Format(ERR_INVALID_NUMBER_OF_ARGUMENTS, "DOTIMES"));
			if (!(args[0] is LispSymbolicAtom)) throw new LispException(string.Format(ERR_NOT_A_SYMBOL, args[0]));
			LispSymbolicAtom counterVar = (LispSymbolicAtom)args[0];

			LispSymbolicAtom[] symbolArray = new LispSymbolicAtom[] { counterVar };
			setq(symbolArray, new LispNumericAtom[] { new LispNumericAtom((LispNumericAtom)args[1].Evaluate(this)) - 1 });

			while ((LispNumericAtom)counterVar.Evaluate(this) >= 0) {
				body.Evaluate(this);
				setq(symbolArray, new LispNumericAtom[] { new LispNumericAtom((LispNumericAtom)counterVar.Evaluate(this)) - 1 });
			}

			if (args.Count == 3) return args[2].Evaluate(this);
			return new LispList();
		}

		private LispDataType dolist(LispList args, LispDataType body) {
			if (args.Count != 2 && args.Count != 3) throw new LispException(string.Format(ERR_INVALID_NUMBER_OF_ARGUMENTS, "DOLIST"));
			if (!(args.First() is LispSymbolicAtom)) throw new LispException(string.Format(ERR_NOT_A_SYMBOL, args.First()));
			LispDataType list = args[1].Evaluate(this);
			if (!(list is LispList)) throw new LispException(string.Format(ERR_NOT_A_LIST, list));

			List<LispSymbolicAtom> varList = new List<LispSymbolicAtom> { (LispSymbolicAtom)args.First() };
			foreach (LispDataType data in (LispList)list) {
				setq(varList, new List<LispDataType>{ data });
				body.Evaluate(this);
			}

			if (args.Count == 3) return args[2].Evaluate(this);
			else return new LispList();
		}

		private LispList remove(LispDataType target, LispList list) {
			LispList result = list.Evaluate(this).Copy();
			result.Data.RemoveAll(d => eqlBoolean(target, d));
			return result;
		}

		private LispList subst(LispDataType newData, LispDataType oldData, LispList list) {
			Dictionary<string, LispDataType> subs = new Dictionary<string, LispDataType>();
			subs[oldData.Evaluate(this).ToString()] = newData.Evaluate(this);
			return list.Replace(subs);
		}

		private LispList member(LispDataType target, LispList list) {
			target = target.Evaluate(this);
			for (int i = 0; i < list.Count; ++i) {
				if (eql(list[i], target)) {
					return list.GetRange(i, list.Count - i);
				}
			}
			return new LispList();
		}

		private LispList reverse(LispList list) {
			LispList result = new LispList(list);
			result.Data.Reverse();
			return result;
		}

		private LispSymbolicAtom makunbound(LispSymbolicAtom a) {
			LispGlobals.Remove(((LispSymbolicAtom)a.Evaluate(this)).Value);
			return a;
		}

		private LispDataType boundp(LispSymbolicAtom a) {
			if (LispGlobals.ContainsKey(((LispSymbolicAtom)a.Evaluate(this)).Value)) return new LispSymbolicAtom("T");
			else return new LispList();
		}

		private LispNumericAtom listlength(LispList list) {
			if (list.IsDotted) throw new LispException(string.Format(ERR_INVALID_LIST_ENDING, list.Last()));
			return new LispNumericAtom(list.Count);
		}

		private LispDataType zerop(LispNumericAtom n) {
			if (n == 0) return new LispSymbolicAtom("T");
			else return new LispList();
		}

		private LispDataType minusp(LispNumericAtom n) {
			if (n.IsNegative) return new LispSymbolicAtom("T");
			else return new LispList();
		}

		private LispDataType plusp(LispNumericAtom n) {
			if (!n.IsNegative) return new LispSymbolicAtom("T");
			else return new LispList();
		}

		private LispDataType symbolp(LispDataType d) {
			if (d is LispSymbolicAtom || (d is LispList && d.IsAtom)) return new LispSymbolicAtom("T");
			else return new LispList();
		}

		private LispDataType numberp(LispDataType d) {
			if (d is LispNumericAtom) return new LispSymbolicAtom("T");
			else return new LispList();
		}

		private LispList cons(LispDataType d1, LispDataType d2) {
			return append(makeList(d1), new List<LispDataType> { d2 });
		}

		private LispDataType and(IEnumerable<LispDataType> list) {
			if (list.All(d => d.Evaluate(this))) return new LispSymbolicAtom("T");
			else return new LispList();
		}

		private LispDataType or(IEnumerable<LispDataType> list) {
			if (list.Any(d => d.Evaluate(this))) return new LispSymbolicAtom("T");
			else return new LispList();
		}

		private LispDataType not(LispDataType d) {
			if (!d.Evaluate(this)) return new LispSymbolicAtom("T");
			else return new LispList();
		}

		private LispDataType equal(LispDataType d1, LispDataType d2) {
			if (d1.Evaluate(this).ToString() == d2.Evaluate(this).ToString()) return new LispSymbolicAtom("T");
			else return new LispList();
		}

		private LispDataType eql(LispDataType d1, LispDataType d2) {
			LispSymbolicAtom t = new LispSymbolicAtom("T");
			LispList nil = new LispList(); ;
			d1 = d1.Evaluate(this);
			d2 = d2.Evaluate(this);

			if (d1 is LispList && d2 is LispList) {
				if (((LispList)d1).Count == 0 && ((LispList)d2).Count == 0) return t;
				else return nil;
			} else if (d1.ToString() == d2.ToString()) return t;
			else return nil;
		}

		private bool eqlBoolean(LispDataType d1, LispDataType d2) {
			d1 = d1.Evaluate(this);
			d2 = d2.Evaluate(this);

			if (d1 is LispList && d2 is LispList) {
				if (((LispList)d1).Count == 0 && ((LispList)d2).Count == 0) return true;
				else return false;
			} else if (d1.ToString() == d2.ToString()) return true;
			else return false;
		}

		private LispDataType lispIf(LispDataType pred1, LispDataType result, LispDataType elseResult = null) {
			if (pred1.Evaluate(this)) return result.Evaluate(this);
			else if (elseResult != null) return elseResult.Evaluate(this);
			else return new LispList();
		}

		private LispDataType cond(IEnumerable<LispList> cases) {
			foreach (LispList iCase in cases) {
				// If the evaluation of the case returns true, return the evaluation of the latter part of it.
				if (iCase.First().Evaluate(this)) {
					return iCase.Last().Evaluate(this);
				}
			}
			// Return NIL if no case matched.
			return new LispList();
		}

		private LispSymbolicAtom defun(LispSymbolicAtom name, LispList arguments, LispDataType function, LispInterpreter context) {
			List<LispSymbolicAtom> argList = new List<LispSymbolicAtom>();
			foreach (LispDataType arg in arguments) {
				if (arg is LispSymbolicAtom) argList.Add((LispSymbolicAtom)arg);
				else throw new LispException(string.Format(ERR_NOT_A_SYMBOL, arg.ToString()));
			}
			LispUserFunctions[name.Value] = new LispUserFunction(name.Value, argList, function);
			LispSymbolicAtom result = new LispSymbolicAtom(name);
			result.SetLiteral(true);
			return result;
		}

		private LispList makeList(LispDataType data) {
			LispList result = new LispList();
			result.Data.Add(data);
			result.SetLiteral(true);
			return result; 
		}

		private LispDataType isNull(LispDataType data) {
			if (data.Evaluate(this).ToString() == "NIL") return new LispSymbolicAtom("T");
			else return new LispList();
		}

		private LispDataType isList(LispDataType data) {
			if (data.Evaluate(this).IsList) return new LispSymbolicAtom("T");
			else return new LispList();
		}

		private LispDataType isAtom(LispDataType data) {
			if (data.Evaluate(this).IsAtom) return new LispSymbolicAtom("T");
			else return new LispList();
		}

		private LispDataType last(LispList list) {
			LispList result = new LispList(list);
			result.Data.RemoveRange(0, result.Data.Count - 1);
			result.SetLiteral(true);
			result.IsDotted = list.IsDotted;
			return result;
		}

		private LispDataType car(LispList list) {
			if (list.IsAtom) return list;
			return list.First();
		}

		private LispDataType cdr(LispList list) {
			if (list.IsAtom) return list;
			if (list.Count == 2 && list.IsDotted) return list.Last();
			return list.GetRange(1, list.Count - 1);
		}

		private LispDataType quote(LispDataType input) {
			input.SetLiteral(true);
			return input;
		}

		private LispDataType set(LispSymbolicAtom symbol, LispDataType value) {
            LispGlobals[symbol.Value] = value.Evaluate(this);
            return value;
        }

		private LispDataType setq(ICollection<LispSymbolicAtom> symbols, ICollection<LispDataType> values) {

            // If there were no pairs given, return the empty list.
            if (symbols.Count == 0 && values.Count == 0) return new LispList();

            int i;

            // Make sure that the symbols and values are all in pairs, else throw an exception.
            if (symbols.Count != values.Count) {

                // Give back the list of pairs with the exception.
                int max = (values.Count > symbols.Count) ? values.Count : symbols.Count;
                string args = "(";
                for (i = 0; i < max; ++i) {
                    if (i < symbols.Count) args = CombineStringsWithSpace(args, symbols.ElementAt(i).Value);
                    if (i < values.Count) args = CombineStringsWithSpace(args, values.ElementAt(i).ToString());
                }

                throw new LispException(String.Format(ERR_ODD_NUMBER_OF_ARGUMENTS, "SETQ", args + ")"));
            }

            // Set each of the symbols with its corresponding value.
            for (i = 0; i < symbols.Count; ++i) {
				LispGlobals[symbols.ElementAt(i).Value] = values.ElementAt(i).Evaluate(this);
            }

            // Return the last value set.
            return symbols.ElementAt(i - 1).Evaluate(this);
        }

		private LispDataType eval(LispDataType value) {
			bool lit = value.IsLiteral;
			dynamic temp = value.Copy();
			if (temp is LispList) ((LispList)temp).SetLiteral(false, false);
			else temp.SetLiteral(false);
			temp = temp.Evaluate(this);
			temp.SetLiteral(false, false);
			//LispDataType result = temp.Evaluate(this);
			//result.SetLiteral(lit);
			return temp;
		}

		private LispList append(LispList list, List<LispDataType> values) {
			if (values.Count == 0) return new LispList(list);
			if (list.IsDotted) throw new LispException(string.Format(ERR_INVALID_LIST_ENDING, list.Last()));
			List<LispDataType> evaluatedData = new List<LispDataType>(values);
			LispList result = new LispList(list);
				
			if (values.First().Evaluate(this) is LispList) {
				// TODO: ADDRANGE AND VALUES NOT BEING COPIED?
				result.Data.AddRange(((LispList)values.First().Evaluate(this)).Data);
				result.IsDotted = list.IsDotted;
			} else {
				result.Data.Add(values.First().Evaluate(this));
				result.IsDotted = true;
			}
			result.SetLiteral(true);
			return append(result, values.GetRange(1, values.Count - 1));
		}

		private LispNumericAtom add(LispNumericAtom a1, LispNumericAtom a2) {
			return a1 + a2;
		}

		private LispNumericAtom subtract(LispNumericAtom a1, LispNumericAtom a2) {
			return a1 - a2;
		}

		private LispNumericAtom multiply(LispNumericAtom a1, LispNumericAtom a2) {
			return a1 * a2;
		}

		private LispNumericAtom divide(LispNumericAtom a1, LispNumericAtom a2) {
			return a1 / a2;
		}

		private LispDataType lessthan(LispNumericAtom a1, LispNumericAtom a2) {
			if (a1 < a2) return new LispSymbolicAtom("T");
			return new LispList();
		}

		private LispDataType lessthanequals(LispNumericAtom a1, LispNumericAtom a2) {
			if (a1 <= a2) return new LispSymbolicAtom("T");
			return new LispList();
		}

		private LispDataType morethan(LispNumericAtom a1, LispNumericAtom a2) {
			if (a1 > a2) return new LispSymbolicAtom("T");
			return new LispList();
		}

		private LispDataType morethanequals(LispNumericAtom a1, LispNumericAtom a2) {
			if (a1 >= a2) return new LispSymbolicAtom("T");
			return new LispList();
		}

		private LispDataType equals(LispNumericAtom a1, LispNumericAtom a2) {
			if (a1 == a2) return new LispSymbolicAtom("T");
			return new LispList();
		}

		private LispDataType notequals(LispNumericAtom a1, LispNumericAtom a2) {
			if (a1 != a2) return new LispSymbolicAtom("T");
			return new LispList();
		}
	}
}
