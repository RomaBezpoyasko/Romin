using System;
using System.Collections.Generic;
using System.Text;

namespace Romin
{
    // =========================================================
    // VALUE KIND
    // =========================================================
    // Defines the types of values that can be represented by the
    // Romin runtime.
    public enum ValueKind
    {
        // Represents the absence of a value.
        Null,

        // 32-bit integer value.
        Int,

        // Double-precision floating-point value.
        Double,

        // Decimal-precision floating-point value.
        Decimal,

        // Boolean value.
        Bool,

        // String value.
        String,

        // Arbitrary CLR/.NET object.
        Object,

        // Romin module instance.
        Module,

        // Romin table containing indexed and key-value data.
        Table,

        // CLR/.NET type reference.
        Type
    }

    // =========================================================
    // VALUE
    // =========================================================
    // Runtime representation of a value inside the Romin VM.
    //
    // Value is implemented as a struct so values can be stored
    // efficiently in VM stacks, environments and tables.
    public struct Value : IEquatable<Value>
    {
        // Identifies which field contains the actual value.
        public ValueKind Kind;

        // Integer representation.
        public readonly int I;

        // Floating-point representation.
        public readonly double F;

        // Decimal floating-point representation.
        public readonly decimal M;

        // Reference to an arbitrary CLR object or runtime object.
        public readonly object O;

        // String representation.
        public readonly string S;

        // Reference to a Romin table.
        public readonly Table T;

        // =====================================================
        // CONSTRUCTORS
        // =====================================================

        // Creates a value with the specified kind.
        // Used primarily for special values such as Null.
        public Value(ValueKind kind)
        {
            Kind = kind;
            I = 0;
            F = 0;
            M = 0;
            O = null;
            S = null;
            T = null;
        }

        // Creates an integer value.
        public Value(int v)
        {
            Kind = ValueKind.Int;
            I = v;

            // Store the integer also as a double, decimal representation.
            F = v;
            M = v;

            O = null;
            S = null;
            T = null;
        }

        // Creates a floating-point value.
        public Value(double v)
        {
            Kind = ValueKind.Double;

            // Store the floating-point representation.
            F = v;

            // Keep an integer representation for cases where
            // the runtime needs an integer-compatible field.
            I = (int)v;

            M = (decimal)v;

            O = null;
            S = null;
            T = null;
        }

        public Value(decimal v)
        {
            Kind = ValueKind.Decimal;
            
            M = v;

            // Store the floating-point representation.
            F = (double)v;

            // Keep an integer representation for cases where
            // the runtime needs an integer-compatible field.
            I = (int)v;
            
            O = null;
            S = null;
            T = null;
        }

        // Creates a boolean value.
        //
        // Internally boolean values are represented as:
        // true  -> 1
        // false -> 0
        public Value(bool v)
        {
            Kind = ValueKind.Bool;
            I = v ? 1 : 0;
            F = v ? 1 : 0;
            O = null;
            S = null;
            T = null;
        }

        // Creates a string value.
        public Value(string v)
        {
            Kind = ValueKind.String;
            S = v;

            I = 0;
            F = 0;
            O = null;
            T = null;
        }

        // Creates a CLR object value.
        public Value(object v)
        {
            // A null CLR reference becomes a Romin Null value.
            if (v == null)
            {
                Kind = ValueKind.Null;
                I = 0;
                F = 0;
                O = null;
                S = null;
                return;
            }

            Kind = ValueKind.Object;
            S = null;
            O = v;
            I = 0;
            F = 0;
            T = null;
        }

        // Creates a Romin table value.
        public Value(Table table)
        {
            Kind = ValueKind.Table;
            T = table;

            I = 0;
            F = 0;
            O = null;
            S = null;
        }

        // Creates a value representing a CLR/.NET type.
        public Value(Type t)
        {
            Kind = ValueKind.Type;
            O = t;
        }

        // =====================================================
        // FACTORY METHODS
        // =====================================================

        // Creates a new empty Romin table.
        public static Value NewTable()
        {
            return new Value(new Table());
        }

        // =====================================================
        // BOOLEAN CONVERSION
        // =====================================================

        // Determines whether the value is considered "true"
        // by the Romin language.
        //
        // Null and zero/false values are treated as false.
        // Most object-like values are considered true when they
        // contain a valid reference.
        public bool IsTrue =>
            Kind switch
            {
                ValueKind.Null => false,

                ValueKind.Bool => I == 1,

                ValueKind.Int => I != 0,

                ValueKind.Double => F != 0,

                ValueKind.Decimal => M != 0,

                ValueKind.Object => O != null,

                ValueKind.Table => T != null,

                // Types and other runtime values are considered true.
                _ => true
            };

        // Checks whether this value represents Romin null.
        public bool IsNull => Kind == ValueKind.Null;

        // Shared immutable representation of the Romin null value.
        public static readonly Value Null = new Value(ValueKind.Null);

        // =====================================================
        // NUMERIC CONVERSION
        // =====================================================

        // Converts the value to a double.
        //
        // Supported source types:
        // Int, Double, Bool and String.
        public double AsDouble()
        {
            switch (Kind)
            {
                // Integer is directly convertible to double.
                case ValueKind.Int:
                    return I;

                // Already a double.
                case ValueKind.Double:
                    return F;

                // Convert to double
                case ValueKind.Decimal:
                    return (double)M;

                // Boolean is represented numerically as 1 or 0.
                case ValueKind.Bool:
                    return I != 0 ? 1.0 : 0.0;

                // Attempt to parse a numeric string.
                // Invalid numeric strings become 0.0.
                case ValueKind.String:
                    return double.TryParse(S, out var v) ? v : 0.0;

                // Other value kinds cannot be converted to double.
                default:
                    throw new InvalidCastException(
                        $"Cannot convert {Kind} to double");
            }
        }

        // =====================================================
        // CLR OBJECT CONVERSION
        // =====================================================

        // Converts a Romin Value into the corresponding CLR object.
        //
        // This method is used when Romin code interacts with
        // .NET/CLR methods and properties.
        public object AsObject()
        {
            return Kind switch
            {
                // Convert Romin integer to System.Int32.
                ValueKind.Int => I,

                // Convert Romin double to System.Double.
                ValueKind.Double => F,

                // Convert Romin decimal to System.Decimal.
                ValueKind.Decimal => M,

                // Return the underlying string.
                ValueKind.String => S,

                // Convert the internal integer representation
                // back to a CLR bool.
                ValueKind.Bool => I != 0,
                                
                // Romin null becomes CLR null.
                ValueKind.Null => null,

                ValueKind.Table => T,

                // For runtime objects return the underlying CLR object.
                //
                // If the object itself contains another Value,
                // recursively unwrap it.
                _ => O is Value v ? v.AsObject() : O
            };
        }

        // Converts the corresponding CLR object into Romin Value.
        //
        // This method is used when Romin code interacts with
        // .NET/CLR methods and properties.
        public static Value FromObject(object obj)
        {
            if (obj == null)
                return Value.Null;

            return obj switch
            {
                Value v => v,

                int v => new Value(v),
                double v => new Value(v),
                decimal v => new Value(v),

                bool v => new Value(v),
                string v => new Value(v),

                Table v => new Value(v),

                _ => new Value(obj)
            };
        }

        // =====================================================
        // EQUALITY
        // =====================================================

        // Compares two Romin values.
        //
        // Values of different kinds are considered different,
        // even if their underlying CLR representations could
        // otherwise be converted to the same value.
        public bool Equals(Value other)
        {
            // Different value kinds are not equal.
            if (Kind != other.Kind)
                return false;

            return Kind switch
            {
                // Compare integer values.
                ValueKind.Int => I == other.I,

                // Compare floating-point values.
                ValueKind.Double => F == other.F,
                ValueKind.Decimal => M == other.M,

                // Compare boolean representation.
                ValueKind.Bool => I == other.I,

                // Compare string contents.
                ValueKind.String => S == other.S,

                // All null values are equal.
                ValueKind.Null => true,

                // Runtime objects are compared by reference.
                _ => ReferenceEquals(O, other.O)
            };
        }

        // =====================================================
        // HASH CODE
        // =====================================================

        // Generates a hash code compatible with Equals().
        //
        // This is particularly important because Value is used
        // as a key in Dictionary<Value, Value> inside Table.
        public override int GetHashCode()
        {
            return Kind switch
            {
                // Include both type and actual value.
                ValueKind.Int => HashCode.Combine(Kind, I),

                ValueKind.Double => HashCode.Combine(Kind, F),
                ValueKind.Decimal => HashCode.Combine(Kind, M),

                ValueKind.Bool => HashCode.Combine(Kind, I),

                ValueKind.String => HashCode.Combine(Kind, S),

                ValueKind.Object => HashCode.Combine(Kind, O),

                // Null has a single hash representation.
                ValueKind.Null => 0,

                // Other value kinds currently use the default
                // fallback hash representation.
                _ => 0
            };
        }

        // =====================================================
        // STRING REPRESENTATION
        // =====================================================

        // Converts the runtime value into a human-readable string.
        //
        // This is used, among other things, by print() and string
        // concatenation.
        public override string ToString()
        {
            return Kind switch
            {
                // Romin null is displayed as "null".
                ValueKind.Null => "null",

                // Strings are returned without additional quotes.
                ValueKind.String => S,

                // Integer representation.
                ValueKind.Int => I.ToString(),

                // Floating-point representation.
                ValueKind.Double => F.ToString(),
                ValueKind.Decimal => M.ToString(),

                // Boolean is converted from its internal 1/0 value.
                ValueKind.Bool => (I == 1).ToString(),

                // For object-like values use their CLR ToString()
                // implementation. If the object is null, return
                // an empty string.
                _ => O?.ToString() ?? ""
            };
        }
    }
}
