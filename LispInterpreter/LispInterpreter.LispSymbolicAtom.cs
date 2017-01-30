// LispSharp Lisp Interpreter
// Curtis Wilson (c) 2016
// LispInterpreter.LispNumericAtom.cs
/* Defines the class used to store, and evaluate Lisp Symbolic Atoms */

using System;
using System.Collections.Generic;

namespace LispInterpreter {
	partial class LispInterpreter {
		private class LispSymbolicAtom : LispDataType, IEquatable<LispSymbolicAtom> {
			public string Value { get; }

			public LispSymbolicAtom(LispSymbolicAtom input) : base(true, false) {
				Value = input.Value;
				IsLiteral = input.IsLiteral;
			}

			public LispSymbolicAtom(string Value) : base(true, false) {
				this.Value = Value;
				if (Value == "T") IsLiteral = true;
			}

			public static bool operator ==(LispSymbolicAtom a1, LispSymbolicAtom a2) {
				return a1.Value == a2.Value;
			}

			public static bool operator !=(LispSymbolicAtom a1, LispSymbolicAtom a2) {
				return a1.Value != a2.Value;
			}

			public override bool Equals(object obj) {
				return base.Equals(obj);
			}

			public override LispDataType Evaluate(LispInterpreter context) {
				if (IsLiteral) {
					return this;
				} else {
					if (context.LispGlobals.ContainsKey(Value)) {
						return context.LispGlobals[Value];
					} else throw new LispException(string.Format(ERR_UNDEFINED_VARIABLE, Value));
				}
			}

			public bool Equals(LispSymbolicAtom other) {
				return Value == other.Value;
			}

			public override string ToString() {
				return Value;
			}

			public override int GetHashCode() {
				return ToString().GetHashCode();
			}
		}
	}
}
