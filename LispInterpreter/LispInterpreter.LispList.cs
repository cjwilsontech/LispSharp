// LispSharp Lisp Interpreter
// Curtis Wilson (c) 2016
// LispInterpreter.LispList.cs
/* Defines the class used to store and operate on Lisp Lists. */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LispInterpreter {
	partial class LispInterpreter {
		private class LispList : LispDataType, IEnumerable<LispDataType>, ICollection<LispDataType>, IList<LispDataType>, IEquatable<LispList> {
			public List<LispDataType> Data = new List<LispDataType>();
			public bool IsDotted = false;

			public int Count => Data.Count;

			public bool IsReadOnly {
				get {
					return ((ICollection<LispDataType>)Data).IsReadOnly;
				}
			}

			public LispList() : base(true, true) {
				IsLiteral = true;
			}

			public LispList(LispList list) : base(list.IsAtom, list.IsList) {
				Data = new List<LispDataType>(list.Data);
				IsDotted = list.IsDotted;
				IsLiteral = list.IsLiteral;
			}

			public LispList(List<LispDataType> list) : base(false, true) {
				Data = list;

				// Handle dotted list input.
				LispSymbolicAtom dot = new LispSymbolicAtom(".");
				int dotCount = Data.Count(sym => sym is LispSymbolicAtom && (LispSymbolicAtom)sym == dot);
				if (dotCount > 1) throw new LispException(string.Format(ERR_INVALID_LIST_ENDING, Data.Last()));
				else if (dotCount == 1) {
					if (Data.FindIndex(sym => sym is LispSymbolicAtom && (LispSymbolicAtom)sym == dot) != Data.Count - 2) throw new LispException(string.Format(ERR_INVALID_LIST_ENDING, Data.Last()));
					else {
						Data.RemoveAt(Data.Count - 2);
						IsDotted = true;
					}
				}

				if (list.Count == 0) {
					IsAtom = true;
					IsLiteral = true;
				}
			}

			public LispList(LispDataType input) : base(false, true) {
				Data.Add(input);

				if (input is LispList) {
					IsLiteral = input.IsLiteral;
					IsDotted = ((LispList)input).IsDotted;
				}
			}

			public LispList(string input) : base(false, true) {

				// Omit the beginning and the ending parenthesis, we just want the items inside the list.
				input = input.Substring(1, input.Length - 2).ToUpper();

				Data = ProcessInputBuffer(ref input);

				// Handle dotted list input.
				LispSymbolicAtom dot = new LispSymbolicAtom(".");
				int dotCount = Data.Count(sym => sym is LispSymbolicAtom && (LispSymbolicAtom)sym == dot);
				if (dotCount > 1) throw new LispException(string.Format(ERR_INVALID_LIST_ENDING, Data.Last()));
				else if (dotCount == 1) {
					if (Data.FindIndex(sym => sym is LispSymbolicAtom && (LispSymbolicAtom)sym == dot) != Data.Count - 2) throw new LispException(string.Format(ERR_INVALID_LIST_ENDING, Data.Last()));
					else {
						Data.RemoveAt(Data.Count - 2);
						IsDotted = true;
					}
				}

				// List is both an atom and a list when empty (nil).
				if (Data.Count == 0)
					IsAtom = true;

			}

			public LispDataType this[int index] {
				get { return this.Data[index]; }
				set { this.Data[index] = value; }
			}

			public override LispDataType Evaluate(LispInterpreter context) {
				if (IsLiteral) {
					return this;
				} else {
					LispDataType result = context.ExecuteLispFunction(Data).Evaluate(context).Copy();
					return result;
				}
			}

			// Return the contents of the list without evaluating.
			public override string ToString() {
				if (Data.Count == 0) return "NIL";
				string output = string.Empty;
				int i = 0;
				foreach (LispDataType data in Data) {
					if (IsDotted && i == Data.Count - 1)
						output = CombineStringsWithSpace(output, ". " + data.ToString());
					else
						output = CombineStringsWithSpace(output, data.ToString());
					++i;
				}
				return "(" + output + ")";
			}

			public void SetLiteral(bool literal, bool recursive = true) {
				IsLiteral = literal;
				foreach (LispDataType data in Data) {
					if (recursive) data.SetLiteral(literal);
					else data.IsLiteral = literal;
				}
			}

			public override void SetLiteral(bool literal) {
				IsLiteral = literal;
				foreach (LispDataType data in Data) {
					data.SetLiteral(literal);
				}
			}

			// Return a copy of the list made of the given range.
			public LispList GetRange(int index, int count) {
				LispList result = Copy();
				result.Data = Data.GetRange(index, count);
				if (result.Count == 1 && result.IsDotted) throw new LispException(string.Format(ERR_INVALID_LIST_ENDING, result.Last()));
				if (result.Count == 0) result.IsAtom = true;
				return result;
			}

			public LispDataType First() {
				return Data.First();
			}

			public LispDataType Last() {
				return Data.Last();
			}

			// Replaces matching atoms with corresponding Lisp datatypes.
			public LispList Replace(Dictionary<string, LispDataType> replacements, bool replaceLiterals = true) {
				List<LispDataType> newData = new List<LispDataType>();
				foreach (LispDataType data in Data) {
					if (data is LispList) {
						if ((!replaceLiterals && data.IsLiteral) || (((LispList)data).First() is LispSymbolicAtom && ((LispSymbolicAtom)((LispList)data).First()).Value == "QUOTE"))
							newData.Add(data.Copy()); 
						else newData.Add(data.Copy().Replace(replacements, replaceLiterals));
					} else if (data is LispSymbolicAtom) {

						// Check if this symbol is one we need to replace.
						if (replacements.ContainsKey(((LispSymbolicAtom)data).ToString()) && (replaceLiterals || (!replaceLiterals && !data.IsLiteral))) {
							LispDataType replacement = replacements[((LispSymbolicAtom)data).ToString()].Copy();
							newData.Add(replacement);
						} else
							newData.Add(data.Copy());
					} else newData.Add(data);
				}
				LispList result = Copy();
				result.Data = newData;
				return result;
			}

			IEnumerator<LispDataType> IEnumerable<LispDataType>.GetEnumerator() {
				return Data.GetEnumerator();
			}

			IEnumerator IEnumerable.GetEnumerator() {
				return Data.GetEnumerator();
			}

			public void Add(LispDataType item) {
				Data.Add(item);
			}

			public void AddRange(IEnumerable<LispDataType> collection) {
				Data.AddRange(collection);
			}

			public void Clear() {
				Data.Clear();
			}

			public bool Contains(LispDataType item) {
				return Data.Contains(item);
			}

			public void CopyTo(LispDataType[] array, int arrayIndex) {
				Data.CopyTo(array, arrayIndex);
			}

			public bool Remove(LispDataType item) {
				return Data.Remove(item);
			}

			public int IndexOf(LispDataType item) {
				return Data.IndexOf(item);
			}

			public void Insert(int index, LispDataType item) {
				Data.Insert(index, item);
			}

			public void RemoveAt(int index) {
				Data.RemoveAt(index);
			}

			public bool Equals(LispList other) {
				return ToString() == other.ToString();
			}
		}
	}
}
