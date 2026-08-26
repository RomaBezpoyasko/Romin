using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Romin
{
    // =========================================================
    // VALUE EXTENSIONS
    // =========================================================
    // Provides extension methods for the Value type.
    //
    // These methods implement common runtime operations used
    // by the VM, such as:
    // - addition;
    // - subtraction;
    // - multiplication;
    // - division;
    // - numeric conversion;
    // - enumerable conversion.
    public static class ValueExtensions
    {
        // =========================================================
        // ADDITION
        // =========================================================

        // Implements the '+' operator for Romin values.
        //
        // Supported operations:
        // - string + anything -> string concatenation;
        // - int + int -> integer addition;
        // - numeric values -> floating-point addition.
        public static Value Add(this Value a, Value b)
        {
            // string concat
            // If either operand is a string, convert both operands
            // to strings and concatenate them.
            if (a.Kind == ValueKind.String ||
                b.Kind == ValueKind.String)
            {
                return new Value(a.ToString() + b.ToString());
            }

            // Case operands are numeric
            if (IsNumber(a) && IsNumber(b))
            {
                return NumericResult(
                    a,
                    b,
                    (x, y) => x + y,
                    (x, y) => x + y,
                    (x, y) => x + y);
            }

            throw new Exception(
                $"Operator '+' not supported for {a.Kind} and {b.Kind}");
        }

        // =========================================================
        // SUBTRACTION
        // =========================================================

        // Implements the '-' operator for Romin values.
        public static Value Sub(this Value a, Value b)
        {
            if (IsNumber(a) && IsNumber(b))
            {
                return NumericResult(
                    a,
                    b,
                    (x, y) => x - y,
                    (x, y) => x - y,
                    (x, y) => x - y);
            }

            throw new Exception(
                $"Operator '-' not supported for {a.Kind} and {b.Kind}");
        }

        // =========================================================
        // MULTIPLICATION
        // =========================================================

        // Implements the '*' operator for Romin values.
        public static Value Mul(this Value a, Value b)
        {
            if (IsNumber(a) && IsNumber(b))
            {
                return NumericResult(
                    a,
                    b,
                    (x, y) => x * y,
                    (x, y) => x * y,
                    (x, y) => x * y);
            }

            throw new Exception(
                $"Operator '*' not supported for {a.Kind} and {b.Kind}");
        }
        // =========================================================
        // DIVISION
        // =========================================================

        // Implements the '/' operator for Romin values.
        public static Value Div(this Value a, Value b)
        {
            if (IsNumber(a) && IsNumber(b))
            {
                return NumericResult(
                    a,
                    b,
                    (x, y) => x / y,
                    (x, y) => x / y,
                    (x, y) => x / y);
            }

            throw new Exception(
                $"Operator '/' not supported for {a.Kind} and {b.Kind}");
        }
        // ------------------------------------
        // HELPERS
        // ------------------------------------

        // Priority in operation. First decimal then double and int 
        private static Value NumericResult(Value a, Value b, 
            Func<int, int, int> intOp, 
            Func<double, double, double> doubleOp, 
            Func<decimal, decimal, decimal> decimalOp)
        {
            // Decimal has the highest numeric priority.
            if (a.Kind == ValueKind.Decimal ||
                b.Kind == ValueKind.Decimal)
            {
                return new Value(
                    decimalOp(
                        ToDecimal(a),
                        ToDecimal(b)));
            }

            // Double has higher priority than Int.
            if (a.Kind == ValueKind.Double ||
                b.Kind == ValueKind.Double)
            {
                return new Value(
                    doubleOp(
                        ToDouble(a),
                        ToDouble(b)));
            }

            // Both values are integers.
            return new Value(
                intOp(a.I, b.I));
        }

        // =========================================================
        // NUMBER CHECK
        // =========================================================

        // Determines whether a Value contains a numeric value.
        //
        // Currently supported numeric types:
        // - Int
        // - Double
        // - Decimal
        public static bool IsNumber(this Value v)
        {
            return v.Kind == ValueKind.Int ||
                   v.Kind == ValueKind.Double ||
                   v.Kind == ValueKind.Decimal;
        }

        // =========================================================
        // NUMBER CONVERSION
        // =========================================================

        // Converts a numeric Value to double.
        //
        // Integer values are promoted to double when required
        // for mixed numeric operations.
        public static double ToDouble(this Value v)
        {
            return v.Kind switch
            {
                ValueKind.Int => v.I,
                ValueKind.Double => v.F,
                ValueKind.Decimal => (double)v.M,

                _ => throw new Exception($"Cannot convert {v.Kind} to number")
            };
        }

        public static decimal ToDecimal(this Value v)
        {
            return v.Kind switch
            {
                ValueKind.Int => v.I,
                ValueKind.Double => (decimal)v.F,
                ValueKind.Decimal => v.M,

                _ => throw new Exception(
                    $"Cannot convert {v.Kind} to decimal")
            };
        }

        // =========================================================
        // ENUMERABLE CONVERSION
        // =========================================================

        // Attempts to convert a Value into an IEnumerable.
        //
        // This method is used by the VM to support iteration
        // over different Romin and .NET values.
        public static IEnumerable? AsEnumerable(this Value v)
        {
            // Romin tables implement IEnumerable directly.
            if (v.Kind == ValueKind.Table && v.T != null)
                return v.T; // Table implements IEnumerable

            // If the underlying object implements IEnumerable,
            // return it directly.
            if (v.O is IEnumerable e)
                return e;

            // An integer can be used as a single-item enumerable.
            //
            // Example:
            // 5 -> [5]
            //
            // This allows the VM to treat an integer as an
            // iterable containing one Value.
            if (v.Kind == ValueKind.Int)
                return Enumerable.Range(v.I, 1)
                    .Select(x => (object)new Value(x));

            // The value cannot be enumerated.
            return null;
        }

    }
}