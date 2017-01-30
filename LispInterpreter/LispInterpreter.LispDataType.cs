// LispSharp Lisp Interpreter
// Curtis Wilson (c) 2016
// LispInterpreter.LispDataType.cs
/* Defines the abstract Lisp datatype, which Lisp lists, symbols, and numbers all inherit from. */

namespace LispInterpreter
{
	partial class LispInterpreter {
		private abstract class LispDataType {
			public bool IsAtom;
			public bool IsList;
			public bool IsLiteral = false;

			public LispDataType(bool isAtom, bool isList) {
				IsAtom = isAtom;
				IsList = isList;
			}

			public bool ToBoolean() {
				// Anything that is not NIL is true.
				return !(IsAtom && IsList);
			}

			public static implicit operator bool(LispDataType data) {
				return data.ToBoolean();
			}

			public abstract LispDataType Evaluate(LispInterpreter context);
			public virtual void SetLiteral(bool literal) {
				IsLiteral = literal;
			}
			public bool GetIsLiteral() {
				return IsLiteral;
			}

			// Returns a copy of the datatype.
			public dynamic Copy() {
				if (this is LispList) return new LispList((LispList)this);
				else if (this is LispSymbolicAtom) return new LispSymbolicAtom((LispSymbolicAtom)this);
				else return new LispNumericAtom((LispNumericAtom)this);
			}

			public override abstract string ToString();
		}
	}
}
