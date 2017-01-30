// LispSharp Lisp Interpreter
// Curtis Wilson (c) 2016
// LispInterpreter.LispNumericAtom.cs
/* Defines the class used to store, operate on, and evaluate Lisp Numeric Atoms */

using System;
using System.Collections.Generic;
using System.Linq;

namespace LispInterpreter
{
    partial class LispInterpreter
    {
		// Lisp Numeric Atom
		private class LispNumericAtom : LispDataType, IComparable<LispNumericAtom>, IEquatable<LispNumericAtom> {
			protected List<int> Data = new List<int>();
			protected List<int> Decimal = new List<int>();
			public bool IsNegative { get; set; } = false;
			public bool IsDecimal { get; set; } = false;

			public LispNumericAtom() : base(true, false) {
				Data.Add(0);
			}

			public LispNumericAtom(string input) : base(true, false) {
				if (input.First() == '-') {
					IsNegative = true;
					input = input.Remove(0, 1);
				}
				foreach (char c in input) {
					if (c == '.') {
						if (IsDecimal) throw new ArgumentException();
						IsDecimal = true;
					} else if (!IsDecimal) Data.Add((int)(c - '0'));
					else Decimal.Add((int)(c - '0'));
				}
			}

			public LispNumericAtom(LispNumericAtom n) : base(true, false) {
				Data = new List<int>(n.Data);
				Decimal = new List<int>(n.Decimal);
				IsNegative = n.IsNegative;
				IsDecimal = n.IsDecimal;
			}


			public LispNumericAtom(int input) : base(true, false) {
				if (input < 0) {
					IsNegative = true;
					input = Math.Abs(input);
				}
				while (true) {
					Data.Insert(0, input % 10);
					if (input < 10) break;
					input = input / 10;
				}
			}

			public LispNumericAtom(List<int> list, List<int> Decimal = null) : base(true, false) {
				Data = list;
				this.Decimal = (Decimal != null) ? Decimal : new List<int>();

				if (Decimal.Count > 0) IsDecimal = true;
				if (list.Count > 0) {
					if (list.First() == '-') {
						IsNegative = true;
						Data.RemoveAt(0);
					}
				}
			}

			public override LispDataType Evaluate(LispInterpreter context) {
				return this;
			}

			public override int GetHashCode() {
				return ToString().GetHashCode();
			}

			public override bool Equals(object obj) {
				return base.Equals(obj);
			}

			public static bool operator <(LispNumericAtom n1, LispNumericAtom n2) {
				if (n1.Data.Count < n2.Data.Count) return true;
				else if (n1.Data.Count > n2.Data.Count) return false;
				else if (n1.Data.Count == n2.Data.Count) {
					// TODO: FIX FOR DECIMAL, NEGATIVE
					for (int i = 0; i < n1.Data.Count; ++i) if (n1.Data[i] < n2.Data[i]) return true;
					for (int i = 0; i < n1.Decimal.Count; ++i) {
						if (i >= n2.Decimal.Count) return false;
						if (n1.Decimal[i] < n2.Decimal[i]) return true;
					}
				}
				return false;
			}

			public static bool operator >(LispNumericAtom n1, LispNumericAtom n2) {
				return !(n1 < n2 || n1 == n2);
			}

			public static bool operator <=(LispNumericAtom n1, LispNumericAtom n2) {
				return n1 < n2 || n1 == n2;
			}

			public static bool operator >=(LispNumericAtom n1, LispNumericAtom n2) {
				return n1 > n2 || n1 == n2;
			}

			public static LispNumericAtom abs(LispNumericAtom n) {
				LispNumericAtom result = new LispNumericAtom(n);
				result.IsNegative = false;
				return result;
			}

			public static LispNumericAtom neg(LispNumericAtom n) {
				LispNumericAtom result = new LispNumericAtom(n);
				result.IsNegative = true;
				return result;
			}

			public static LispNumericAtom operator +(LispNumericAtom left, LispNumericAtom right) {
				if (!left.IsNegative && right.IsNegative) return left - abs(right);
				if (left.IsNegative && !right.IsNegative) return right - abs(left);

				List<int> resultWhole = new List<int>(),
					resultDecimal = new List<int>();

				// DECIMAL
				// Get the maximum number of places to account for.
				int leftCount = left.Decimal.Count,
					rightCount = right.Decimal.Count,
					maxCount = max(leftCount, rightCount);

				// Perform the adding operation.
				int carry = 0;
				for (int i = maxCount - 1; i >= 0; --i) {
					int tempResult = carry;
					if (i < leftCount) tempResult += left.Decimal[i];
					if (i < rightCount) tempResult += right.Decimal[i];
					carry = tempResult / 10;
					resultDecimal.Insert(0, tempResult % 10);
				}
				TrimTrailingZeroes(ref resultDecimal);

				// WHOLE
				// Get the maximum number of places to account for.
				leftCount = left.Data.Count;
				rightCount = right.Data.Count;
				maxCount = max(leftCount, rightCount);

				// Perform the adding operation.
				for (int i = 0; i < maxCount; ++i) {
					int tempResult = carry;
					if (leftCount > i) tempResult += left.Data[leftCount - i - 1];
					if (rightCount > i) tempResult += right.Data[rightCount - i - 1];
					carry = tempResult / 10;
					resultWhole.Insert(0, tempResult % 10);
				}
				if (carry != 0) resultWhole.Insert(0, carry);
				TrimLeadingZeroes(ref resultWhole);


				// Build the final answer.
				LispNumericAtom result = new LispNumericAtom(resultWhole, resultDecimal);
				// At this point both are negative, or both are positive, in which case our result will be set accordingly.
				result.IsNegative = left.IsNegative;
				result.IsDecimal = left.IsDecimal || right.IsDecimal;
				return result;
			}

			public static LispNumericAtom operator -(LispNumericAtom left, LispNumericAtom right) {
				if (left.IsNegative || right.IsNegative) return left + abs(right);
				if (right > left) return neg(right - left);

				// Make a copy of the left number to work off of, as borrow will modify the original.
				LispNumericAtom leftNum = new LispNumericAtom(left);
				List<int> resultWhole = new List<int>(),
					resultDecimal = new List<int>();

				int leftCount = leftNum.Decimal.Count,
					rightCount = right.Decimal.Count,
					maxCount = max(leftCount, rightCount);
				// Decimal
				if (leftCount < maxCount) {
					borrow(ref leftNum, maxCount - 1, false);
					leftCount = leftNum.Decimal.Count;
					// Left should now have more than or equal number of decimal places.
				}
				for (int i = 0; i < maxCount; ++i) {
					if (leftCount - i <= rightCount) {
						if (leftNum.Decimal[leftCount - i - 1] < right.Decimal[leftCount - i - 1]) {
							borrow(ref leftNum, leftCount - i - 1, false);
						}
						resultDecimal.Insert(0, leftNum.Decimal[leftCount - i - 1] - right.Decimal[leftCount - i - 1]);
					} else resultDecimal.Insert(0, leftNum.Decimal[leftCount - i - 1]);
				}

				// Whole
				leftCount = leftNum.Data.Count;
				rightCount = right.Data.Count;
				for (int i = 0; i < leftCount; ++i) {
					if (i < rightCount) {
						if (leftNum.Data[leftCount - 1 - i] < right.Data[rightCount - 1 - i]) borrow(ref leftNum, leftCount - 1 - i, true);
						resultWhole.Insert(0, leftNum.Data[leftCount - 1 - i] - right.Data[rightCount - 1 - i]);
					} else resultWhole.Insert(0, leftNum.Data[leftCount - 1 - i]);
				}

				TrimLeadingZeroes(ref resultWhole);
				TrimTrailingZeroes(ref resultDecimal);
				LispNumericAtom result = new LispNumericAtom(resultWhole, resultDecimal);
				return result;
			}

			public static LispNumericAtom operator *(LispNumericAtom left, LispNumericAtom right) {
				LispNumericAtom result = new LispNumericAtom();

				// Multiply from decimal.
				int leftCount = left.Decimal.Count,
					rightCount = right.Decimal.Count;
				for (int i = rightCount - 1; i >= 0; --i) {
					List<int> resultWhole = new List<int>(),
						resultDecimal = new List<int>();
					int mulNum = right.Decimal[i];

					LispNumericAtom temp = new LispNumericAtom();
					while (mulNum-- > 0) temp += left;
					temp.MoveDecimal(i + 1);
					result += temp;
				}

				// Multiply from Data.
				leftCount = left.Data.Count;
				rightCount = right.Data.Count;
				for (int i = rightCount - 1; i >= 0; --i) {
					List<int> resultWhole = new List<int>(),
						resultDecimal = new List<int>();
					int mulNum = right.Data[i];

					LispNumericAtom temp = new LispNumericAtom();
					while (mulNum-- > 0) temp += left;
					temp.MoveDecimal(-i);
					result += temp;
				}

				result.IsNegative = left.IsNegative ^ right.IsNegative;
				return result;
			}

			public static LispNumericAtom operator /(LispNumericAtom left, LispNumericAtom right) {
				if (right == 0) throw new LispException(ERR_DIVISION_BY_ZERO);
				return new LispNumericAtom((left.ToDouble() / right.ToDouble()).ToString());
			}

			public static LispNumericAtom operator %(LispNumericAtom left, LispNumericAtom right) {
				return new LispNumericAtom((left.ToDouble() % right.ToDouble()).ToString());
			}

			public LispNumericAtom Round() {
				LispNumericAtom result = new LispNumericAtom(this);
				if (result.Decimal.Count > 0 && result.Decimal.First() >= 5) result += 1;
				result.Decimal.Clear();
				result.IsDecimal = false;
				return result;
			}

			private void MoveDecimal(int places) {
				// Places is the relative position.
				// (12.34).MoveDecimal(-1) = 123.4
				// (12.34).MoveDecimal(1) = 1.234
				if (places < 0) {
					// Shift left.
					while (places++ < 0) {
						if (Decimal.Count > 0) {
							Data.Add(Decimal.First());
							Decimal.RemoveAt(0);
						} else Data.Add(0);
					}
				} else if (places > 0) {
					// Shift right.
					while (places-- > 0) {
						if (Data.Count > 0) {
							Decimal.Insert(0, Data.Last());
							Data.RemoveAt(Data.Count - 1);
						} else Decimal.Insert(0, 0);
					}
				}
				TrimLeadingZeroes(ref Data);
				TrimTrailingZeroes(ref Decimal);
				if (!IsDecimal && Decimal.Count > 0) IsDecimal = true;
			}

			public static LispNumericAtom operator -(LispNumericAtom left, int right) {
				return left - new LispNumericAtom(right);
			}

			public static LispNumericAtom operator -(int left, LispNumericAtom right) {
				return new LispNumericAtom(left) - right;
			}

			public static LispNumericAtom operator +(LispNumericAtom left, int right) {
				return left + new LispNumericAtom(right);
			}

			public static LispNumericAtom operator +(int left, LispNumericAtom right) {
				return new LispNumericAtom(left) + right;
			}

			public int ToInteger() {
				string s = ((IsNegative) ? "-" : "");
				foreach (int i in Data) s += i;
				return int.Parse(s);
			}

			public double ToDouble() {
				return double.Parse(ToString());
			}

			public float ToFloat() {
				return float.Parse(ToString());
			}

			public long ToLong() {
				string s = ((IsNegative) ? "-" : "");
				foreach (int i in Data) s += i;
				return long.Parse(s);
			}

			public decimal ToDecimal() {
				return decimal.Parse(ToString());
			}

			public override string ToString() {
				string result = (IsNegative) ? "-" : string.Empty;
				if (Data.Count > 0) foreach (int number in Data) result += number;
				else result += '0';

				if (IsDecimal) {
					result += '.';
					if (Decimal.Count == 0) result += '0';
					else foreach (int number in Decimal) result += number;
				}

				return result;
			}

			public static implicit operator bool(LispNumericAtom number) {
				// Returns false if it is equal to 0.
				return !(number.Data.Count == 1 && number.Data.First() == 0);
			}

			public static implicit operator int(LispNumericAtom n) {
				return n.ToInteger();
			}

			public static implicit operator long(LispNumericAtom n) {
				return n.ToLong();
			}

			public static implicit operator float(LispNumericAtom n) {
				return n.ToFloat();
			}

			public static implicit operator double(LispNumericAtom n) {
				return n.ToDouble();
			}

			public static implicit operator decimal(LispNumericAtom n) {
				return n.ToDecimal();
			}

			public static bool operator ==(LispNumericAtom n1, LispNumericAtom n2) {
				return (ReferenceEquals(n1, n2) && (ReferenceEquals(null, n1)) || n1.ToString() == n2.ToString());
			}

			public static bool operator !=(LispNumericAtom n1, LispNumericAtom n2) {
				return n1.ToString() != n2.ToString();
			}

			// Helper functions.
			private static void TrimLeadingZeroes(ref List<int> list) {
				int index = list.FindIndex(i => i != 0);
				list.RemoveRange(0, (index != -1) ? index : max(0, list.Count - 1));
			}

			private static void TrimTrailingZeroes(ref List<int> list) {
				int index = list.FindLastIndex(i => i != 0);
				if (list.Count > 0) list.RemoveRange(list.Count - 1, (index != -1) ? list.Count - 1 - index : max(0, list.Count - 1));
			}

			private static void borrow(ref LispNumericAtom number, int position, bool positionIsWhole) {
				int borrowPosition;
				if (!positionIsWhole) {
					borrowPosition = number.Decimal.FindLastIndex(i => i != 0 && i < position);
					if (borrowPosition != -1) {
						for (; borrowPosition < position - 1; ++borrowPosition) {
							--number.Decimal[borrowPosition];
							number.Decimal[borrowPosition + 1] += 10;
						}
					}
				}

				if (positionIsWhole) {
					// Distribute to position in whole.
					borrowPosition = -1;
					for (int i = position - 1; i >= 0 && borrowPosition == -1; --i) if (number.Data[i] != 0) borrowPosition = i;
					for (; borrowPosition < position; ++borrowPosition) {
						--number.Data[borrowPosition];
						number.Data[borrowPosition + 1] += 10;
					}
				} else {

					// Distribute to whole.
					borrowPosition = number.Data.FindLastIndex(i => i != 0);
					--number.Data[borrowPosition++];
					for (; borrowPosition < number.Data.Count; ++borrowPosition) {
						number.Data[borrowPosition] += 9;
					}

					// Distribute to position in decimal.
					for (int i = -1; i < position; ++i) {
						if (i != -1) --number.Decimal[i];
						if (number.Decimal.Count > i + 1) number.Decimal[i + 1] += 10;
						else number.Decimal.Add(10);
					}

				}
			}

			public int CompareTo(LispNumericAtom other) {
				if (this == other) return 0;
				if (this < other) return -1;
				return 1;
			}

			public bool Equals(LispNumericAtom other) {
				return this == other;
			}
		}
	}
}
