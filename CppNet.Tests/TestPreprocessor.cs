using Xunit.Abstractions;
namespace CppNet.Tests;

using CppNet;

public class TestPreprocessor
{

    private readonly ITestOutputHelper _logger;

    private readonly Preprocessor _preprocessor;

    public TestPreprocessor(ITestOutputHelper logger)
    {
        _preprocessor = new Preprocessor();
        _logger = logger;
    }

    private void TestInput(string input, params ReadOnlySpan<object> output)
    {
        _preprocessor.addInput(new StringLexerSource(input, "test_file.c"));

        foreach (object expected in output)
        {
            Token token = _preprocessor.token();
            _logger.WriteLine($"Token is: {token.ToString()}");

            if (expected is string s)
            {
                if (token.getType() != Token.STRING)
                {
                    Assert.Fail($"mismatching type, got {token.getType()}");
                }
                Assert.Equal(s, (string)token.getValue());

            }
            else if (expected is Identifier i)
            {
                Assert.True(token.getType() == Token.IDENTIFIER);
                Assert.Equal(i.ToString(), token.getText());
            }
            else if (expected is char c)
            {
                Assert.Equal(c, token.getType());
            }
            else if (expected is int num)
            {
                Assert.Equal(num, token.getType());
            }
            else
            {
                Assert.Fail($"Bad type: {expected.GetType()}");
            }
        }

        Token remaining = _preprocessor.token();
        bool failed = false;
        while (remaining.getType() != Token.EOF)
        {
            failed = true;
            _logger.WriteLine($"REMAINING INPUT: {remaining.ToString()}");
            remaining = _preprocessor.token();
        }

        if (failed)
        {
            Assert.Fail("There was unconsumed test input remaining");
        }
    }

    private static Identifier I(string literal) => new Identifier(literal);

    [Fact]
    public void TestBuiltins()
    {
        TestInput("line = __LINE__", I("line"), Token.WHITESPACE, '=', Token.WHITESPACE, Token.INTEGER);
        TestInput("file = __FILE__", I("file"), Token.WHITESPACE, '=', Token.WHITESPACE, Token.STRING);
    }

    [Fact]
    public void TestSimpleDefines()
    {
        TestInput("#define A a /* a defined */", Token.EOF);
        TestInput("#define B b /* b defined */", Token.EOF);
        TestInput("#define C c /* c defined */", Token.EOF);
    }

    [Fact]
    public void TestExpansion()
    {
        TestInput("#define A a", Token.EOF);
        TestInput("#define EXPAND(x) x", Token.EOF);
        TestInput("EXPAND(a)", I("a"));
        TestInput("EXPAND(A)", I("a"));
    }

    [Fact]
    public void TestStringification()
    {
        TestInput("#define A a /* a defined */", Token.EOF);
        TestInput("#define _STRINGIFY(x) #x", Token.EOF);
        TestInput("_STRINGIFY(A)", "A", Token.EOF);
        TestInput("#define STRINGIFY(x) _STRINGIFY(x)", Token.EOF);
        TestInput("STRINGIFY(b)", "b");
        TestInput("STRINGIFY(A)", "a");
    }

    [Fact]
    public void TestConcatenation()
    {
        TestInput("#define A a /* a defined */", Token.EOF);
        TestInput("#define B b /* b defined */", Token.EOF);
        TestInput("#define C c /* c defined */", Token.EOF);

        TestInput("#define _CONCAT(x, y) x ## y", Token.EOF);
        TestInput("_CONCAT(A, B)", I("AB"));
        TestInput("#define A_CONCAT done_a_concat\n", Token.EOF);
        TestInput("_CONCAT(A, _CONCAT(B, C))",
                I("done_a_concat"), '(', I("b"), ',', Token.WHITESPACE, I("c"), ')'
            );
        TestInput("#define CONCAT(x, y) _CONCAT(x, y)", Token.EOF);
        TestInput("CONCAT(A, CONCAT(B, C))", I("abc"), Token.EOF);
        TestInput("#define _CONCAT3(x, y, z) x ## y ## z", Token.EOF);
        TestInput("_CONCAT3(a, b, c)", I("abc"));
        TestInput("_CONCAT3(A, B, C)", I("ABC"));
        TestInput("_CONCAT(test_, inline)", I("test_inline"));
        TestInput("_CONCAT(test_, \nnewline)", I("test_newline"));
    }

    [Fact]
    public void TestDefinitions()
    {
        // TODO: my little idea of just adding string inputs comes to a grinding halt with this,
        // since #undef skips inputs. We'll have to bite the bullet and see if we can get some kind of consuming
        // buffer flusher thingy going
        TestInput("#define two three\n", Token.EOF);
        TestInput("one /* one */", I("one"), Token.WHITESPACE, Token.CCOMMENT);
        TestInput("#define one two", Token.EOF);
        TestInput("one /* three */\n", I("three"), Token.WHITESPACE, Token.CCOMMENT, Token.WHITESPACE);
        TestInput("#undef two\n", Token.WHITESPACE, Token.EOF);
        TestInput("#define two five", Token.EOF);
        TestInput("one /* five */", I("five"), Token.WHITESPACE, Token.CCOMMENT);
        TestInput("#undef two", Token.EOF);
        TestInput("one /* two */", I("two"), Token.WHITESPACE, Token.CCOMMENT);
        TestInput("#undef one", Token.EOF);
        TestInput("#define one four", Token.EOF);
        TestInput("one /* four */", I("four"), Token.WHITESPACE, Token.CCOMMENT);
        TestInput("#undef one", Token.EOF);
        TestInput("#define one one", Token.EOF);
        TestInput("one /* one */", I("one"), Token.WHITESPACE, Token.CCOMMENT);
    }

    private sealed class Identifier
    {
        private readonly string _literal;

        public Identifier(string literal)
        {
            _literal = literal;
        }

        public override string ToString() => _literal;
    }
}
