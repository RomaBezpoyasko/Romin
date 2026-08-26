using System;
using System.Collections.Generic;
using System.Text;

namespace Romin
{

    // =========================================================
    // TOKENS
    // =========================================================
    // Defines all lexical token types recognized by the Romin
    // language. The lexer converts source code into these tokens,
    // which are then consumed by the parser.
    public enum TokenType
    {
        // =====================================================
        // MODULE / IDENTIFIERS / LITERALS
        // =====================================================

        // Includes another Romin source file.
        Base,

        // User-defined names such as variables, functions,
        // types, and other identifiers.
        Identifier,

        // Numeric literal.
        Number,

        // Regular string literal.
        String,

        // String containing interpolation expressions.
        InterpolatedString,

        // Boolean and null literals.
        True,
        False,
        Null,

        // =====================================================
        // COMPARISON
        // =====================================================

        // Equality comparison: ==
        EqualEqual,

        // Inequality comparison: !=
        NotEqual,

        // =====================================================
        // FUNCTIONS
        // =====================================================

        // Function declaration keyword.
        Fn,

        // Returns a value from the current function.
        Return,

        // =====================================================
        // CONTROL FLOW
        // =====================================================

        // Conditional statement.
        If,

        // Alternative branch of an if statement.
        Else,

        // While loop.
        While,

        // For loop.
        For,

        // Specifies the collection/range being iterated.
        In,

        // Range operator: ..
        DotDot,

        // =====================================================
        // LOGICAL OPERATORS
        // =====================================================

        // Word-based logical AND.
        And,

        // Word-based logical OR.
        Or,

        // Symbolic logical AND: &&
        AndAnd,

        // Symbolic logical OR: ||
        OrOr,

        // =====================================================
        // ARITHMETIC / ASSIGNMENT OPERATORS
        // =====================================================

        // Addition operator: +
        Plus,

        // Subtraction operator: -
        Minus,

        // Addition assignment: +=
        PlusEqual,

        // Multiplication operator: *
        Star,

        // Division operator: /
        Slash,

        // Increment operator: ++
        PlusPlus,

        // Decrement operator: --
        MinusMinus,

        // =====================================================
        // COMPARISON OPERATORS
        // =====================================================

        // Greater than: >
        Greater,

        // Less than: <
        Less,

        // Greater than or equal: >=
        GreaterEqual,

        // Less than or equal: <=
        LessEqual,

        // Assignment operator: =
        Equals,

        // =====================================================
        // FUNCTION / MEMBER ACCESS
        // =====================================================

        // Function/lambda arrow: ->
        Arrow,

        // Member access operator: .
        Dot,

        // =====================================================
        // DELIMITERS
        // =====================================================

        // Opening parenthesis: (
        LParen,

        // Closing parenthesis: )
        RParen,

        // Opening bracket: [
        LBracket,

        // Closing bracket: ]
        RBracket,

        // Separates function arguments and other elements.
        Comma,

        // =====================================================
        // LINE / FILE STRUCTURE
        // =====================================================

        // Represents a line break in the source code.
        // Romin uses newlines as part of its indentation-based
        // syntax.
        NewLine,

        // Marks the end of the token stream.
        EOF,

        // =====================================================
        // EXCEPTION HANDLING
        // =====================================================

        // Starts a try block.
        Try,

        // Starts a catch block.
        Catch,

        // =====================================================
        // CONDITIONAL / NULL OPERATORS
        // =====================================================

        // Question mark used by conditional/null-related syntax.
        Question,

        // Colon used to separate parts of expressions/statements.
        Colon,

        // Null-coalescing operator: ??
        Coalesce,

        // Logical NOT operator: !
        Not,

        // =====================================================
        // OBJECT / TYPE SYSTEM
        // =====================================================

        // Creates a new CLR or Romin object.
        New,

        // Loads or enables a .NET assembly.
        Use,

        // =====================================================
        // INDENTATION
        // =====================================================

        // Marks the beginning of an indented block.
        Indent,

        // Marks the end of an indented block.
        Dedent
    }

    // =========================================================
    // TOKEN
    // =========================================================
    // Represents one lexical element produced by the lexer.
    // The parser uses tokens to construct the program structure.
    public class Token
    {
        // Type of the token.
        public TokenType Type;

        // Textual value associated with the token.
        // For example, an identifier name or literal value.
        public string Value;

        // Source line where the token was found.
        public int Line;

        // Source column where the token starts.
        public int Column;

        // Absolute character position in the source text.
        public int Position;

        // Source file from which the token originated.
        // This is especially useful when the program consists
        // of multiple files loaded through the 'base' mechanism.
        public string File;
    }
}
