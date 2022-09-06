﻿using System;

namespace Strict.Language.Expressions;

//TODO: docusaurus link for all expressions!
public sealed class For : Expression
{
	private const string ForName = "for";

	public For(Expression value, Expression body) : base(value.ReturnType)
	{
		Value = value;
		Body = body;
	}

	public Expression Value { get; } //TODO: for Range, for list, etc.
	public Expression Body { get; }
	public override int GetHashCode() => Value.GetHashCode();
	public override string ToString() => $"for {Value}";
	public override bool Equals(Expression? other) => other is For a && Equals(Value, a.Value);

	public static Expression? TryParse(Body body, ReadOnlySpan<char> line)
	{
		if (!line.StartsWith(ForName, StringComparison.Ordinal))
			return null;
		if (line.Length <= ForName.Length)
			throw new MissingExpression(body);
		var innerBody = body.FindCurrentChild();
		if (innerBody == null)
			throw new MissingInnerBody(body);
		var bodyExpression = innerBody.Parse();
		return new For(body.Method.ParseExpression(body, line[4..]), bodyExpression);
	}

	public sealed class MissingExpression : ParsingFailed
	{
		public MissingExpression(Body body) : base(body) { }
	}

	public sealed class MissingInnerBody : ParsingFailed
	{
		public MissingInnerBody(Body body) : base(body) { }
	}
}