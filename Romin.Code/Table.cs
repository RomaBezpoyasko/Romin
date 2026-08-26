using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Romin
{
    // =========================================================
    // TABLE
    // =========================================================
    // Represents the main table data structure used by the Romin
    // runtime. A table can work both as an indexed array and as
    // a key-value dictionary.
    public sealed class Table : IEnumerable<TableEntry>
    {
        // =====================================================
        // STORAGE
        // =====================================================

        // Stores sequential integer-indexed values.
        // Romin uses 1-based indexing for this part of the table.
        public readonly List<Value> Array = new();

        // Stores arbitrary key-value pairs.
        // Used for string keys and other non-integer keys.
        public readonly Dictionary<Value, Value> Map = new();

        // =====================================================
        // ENUMERATION
        // =====================================================

        // Enumerates both array elements and dictionary entries.
        // Array elements are returned first, followed by map entries.
        public IEnumerator<TableEntry> GetEnumerator()
        {
            // Enumerate array elements.
            // Convert the internal zero-based C# index to the
            // one-based index used by the Romin language.
            for (int i = 0; i < Array.Count; i++)
            {
                yield return new TableEntry()
                {
                    Key = new Value(i + 1),
                    Value = Array[i]
                };
            }

            // Enumerate key-value pairs stored in the map.
            foreach (var kv in Map)
            {
                yield return new TableEntry()
                {
                    Key = kv.Key,
                    Value = kv.Value
                };
            }
        }

        // Non-generic IEnumerable implementation required by
        // the standard .NET collection interfaces.
        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        // =====================================================
        // GET
        // =====================================================

        // Returns a value associated with the specified key.
        // Missing keys return Value.Null instead of throwing.
        public Value Get(Value key)
        {
            // Integer keys address the array portion of the table.
            if (key.Kind == ValueKind.Int)
            {
                // Convert Romin's 1-based index to C#'s 0-based index.
                int i = key.I - 1;

                // Return the array value when the index is valid.
                if (i >= 0 && i < Array.Count)
                    return Array[i];

                // Return null value for an invalid or missing index.
                return Value.Null;
            }

            // Non-integer keys are stored in the dictionary.
            // Return Value.Null when the key does not exist.
            return Map.TryGetValue(key, out var v)
                ? v
                : Value.Null;
        }

        // =====================================================
        // SET
        // =====================================================

        // Assigns a value to the specified table key.
        public void Set(Value key, Value value)
        {
            // Integer keys address the array portion of the table.
            if (key.Kind == ValueKind.Int)
            {
                // Convert Romin's 1-based index to C#'s 0-based index.
                int i = key.I - 1;

                // Ignore indexes below the first valid Romin index.
                if (i < 0)
                    return;

                // Expand the array when necessary.
                // Newly created elements are initialized with Value.Null.
                while (Array.Count <= i)
                    Array.Add(Value.Null);

                // Store the value at the requested index.
                Array[i] = value;
                return;
            }

            // Non-integer keys are stored in the dictionary.
            Map[key] = value;
        }

        // =====================================================
        // HAS
        // =====================================================

        /// <summary>
        /// Checks whether the specified key exists in the table.
        /// </summary>
        /// <param name="key">Key to search for.</param>
        /// <returns>
        /// True when the key exists; otherwise false.
        /// </returns>
        public bool Has(Value key)
        {
            // Null cannot be used as an existing table key.
            if (key.Kind == ValueKind.Null)
                return false;

            // Integer keys refer to the array portion.
            if (key.Kind == ValueKind.Int)
            {
                // Convert the Romin 1-based index to a zero-based index.
                int i = key.I;
                i--;

                // The key exists when the resulting index is inside
                // the current array bounds.
                return i >= 0 && i < Array.Count;
            }

            // All other keys are checked in the dictionary.
            return Map.ContainsKey(key);
        }

        // =====================================================
        // CONTAINS
        // =====================================================

        /// <summary>
        /// Checks whether the specified value exists in the table.
        /// </summary>
        /// <param name="value">Value to search for.</param>
        /// <returns>
        /// True when the value is present; otherwise false.
        /// </returns>
        public bool Contains(Value value)
        {
            // Special handling for Value.Null.
            // Null values can exist both in the array and in the map.
            if (value.Kind == ValueKind.Null)
            {
                // Search null values in the array.
                foreach (var item in Array)
                    if (item.Kind == ValueKind.Null)
                        return true;

                // Search null values among dictionary values.
                foreach (var kv in Map.Values)
                    if (kv.Kind == ValueKind.Null)
                        return true;

                return false;
            }

            // Search for the value in the array portion.
            for (int i = 0; i < Array.Count; i++)
                if (Equals(Array[i], value))
                    return true;

            // Search for the value among dictionary values.
            foreach (var kv in Map)
                if (Equals(kv.Value, value))
                    return true;

            return false;
        }

        // =====================================================
        // COUNT
        // =====================================================

        /// <summary>
        /// Returns the total number of stored entries.
        /// </summary>
        /// <returns>
        /// Number of array elements plus number of map entries.
        /// </returns>
        public int Count()
        {
            return Array.Count + Map.Count;
        }

        // =====================================================
        // CLEAR
        // =====================================================

        /// <summary>
        /// Removes all elements from the table.
        /// </summary>
        /// <returns>
        /// True after the table has been cleared.
        /// </returns>
        public bool Clear()
        {
            // Remove all indexed values.
            Array.Clear();

            // Remove all dictionary entries.
            Map.Clear();

            return true;
        }
    }

    // =========================================================
    // TABLE ENTRY
    // =========================================================
    // Represents one key-value pair returned during table
    // enumeration.
    public struct TableEntry
    {
        // Entry key.
        public Value Key;

        // Entry value.
        public Value Value;
    }
}
