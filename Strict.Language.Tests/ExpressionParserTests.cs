using System;
using System.Collections.Generic;
using NUnit.Framework;
using Strict.Language.Expressions;

namespace Strict.Language.Tests;

public class ExpressionParserTests : ExpressionParser
{
	[SetUp]
	public void CreateType() =>
		type = new Type(new TestPackage(), new MockRunTypeLines()).ParseMembersAndMethods(this);

	private Type type = null!;

	[Test]
	public void ParsingHappensAfterCallingBody()
	{
		Assert.That(parseWasCalled, Is.False);
		Assert.That(type.Methods[0].Body, Is.Not.Null);
		Assert.That(parseWasCalled, Is.True);
		Assert.That(type.Methods[0].Body.Expressions, Is.Not.Empty);
		Assert.That(type.Methods[0].Body.ReturnType, Is.EqualTo(type.Methods[0].ReturnType));
	}

	private bool parseWasCalled;

	public class TestExpression : Expression
	{
		public TestExpression(Type returnType) : base(returnType) { }
	}

	//ncrunch: no coverage start, not the focus here
	public override Expression ParseAssignmentExpression(Type assignmentType,
		ReadOnlySpan<char> initializationLine, int fileLineNumber) =>
		null!;

	public override Expression ParseMethodLine(Method.Line line, ref int methodLineNumber)
	{
		parseWasCalled = true;
		return new Number(line.Method, 1);
	}

	public override Expression ParseExpression(Method.Line line, Range range) => null!;

	public override List<Expression> ParseListArguments(Method.Line line, Range range) => null!;
	//ncrunch: no coverage end

	[Test]
	public void CompareExpressions()
	{
		var expression = new TestExpression(type);
		Assert.That(expression, Is.EqualTo(new TestExpression(type)));
		Assert.That(expression.GetHashCode(), Is.EqualTo(new TestExpression(type).GetHashCode()));
		Assert.That(new TestExpression(type.Methods[0].ReturnType),
			Is.Not.EqualTo(new TestExpression(type)));
		Assert.That(expression.Equals((object)new TestExpression(type)), Is.True);
	}

	[Test]
	public void EmptyLineIsNotValidInMethods() =>
		Assert.That(() => new Method(type, 0, this, new[] { "Run", "" }),
			Throws.InstanceOf<Type.EmptyLineIsNotAllowed>());

	[Test]
	public void NoIndentationIsNotValidInMethods() =>
		Assert.That(() => new Method(type, 0, this, new[] { "Run", "abc" }),
			Throws.InstanceOf<Method.InvalidIndentation>());

	[Test]
	public void TooMuchIndentationIsNotValidInMethods() =>
		Assert.That(() => new Method(type, 0, this, new[] { "Run", new string('\t', 4) }),
			Throws.InstanceOf<Method.InvalidIndentation>());

	[Test]
	public void ExtraWhitespacesAtBeginningOfLineAreNotAllowed() =>
		Assert.That(() => new Method(type, 0, this, new[] { "Run", "\t let abc = 3" }),
			Throws.InstanceOf<Type.ExtraWhitespacesFoundAtBeginningOfLine>());

	[Test]
	public void ExtraWhitespacesAtEndOfLineAreNotAllowed() =>
		Assert.That(() => new Method(type, 0, this, new[] { "Run", "\tlet abc = 3 " }),
			Throws.InstanceOf<Type.ExtraWhitespacesFoundAtEndOfLine>());

	[Test]
	public void GetSingleLine()
	{
		var method = new Method(type, 0, this, new[] { "Run", MethodTests.LetNumber });
		Assert.That(method.bodyLines, Has.Length.EqualTo(1));
		Assert.That(method.bodyLines[0].ToString(), Is.EqualTo(MethodTests.LetNumber));
	}

	[Test]
	public void GetMultipleLines()
	{
		var method = new Method(type, 0, this,
			new[] { "Run", MethodTests.LetNumber, MethodTests.LetOther });
		Assert.That(method.bodyLines, Has.Length.EqualTo(2));
		Assert.That(method.bodyLines[0].ToString(), Is.EqualTo(MethodTests.LetNumber));
		Assert.That(method.bodyLines[1].ToString(), Is.EqualTo(MethodTests.LetOther));
	}

	[Test]
	public void GetNestedLines()
	{
		var method = new Method(type, 0, this, MethodTests.NestedMethodLines);
		Assert.That(method.bodyLines, Has.Length.EqualTo(4));
		Assert.That(method.bodyLines[0].ToString(), Is.EqualTo(MethodTests.LetNumber));
		Assert.That(method.bodyLines[1].ToString(), Is.EqualTo("	if bla is 5"));
		Assert.That(method.bodyLines[2].ToString(), Is.EqualTo("		return true"));
		Assert.That(method.bodyLines[3].ToString(), Is.EqualTo("	false"));
	}
}