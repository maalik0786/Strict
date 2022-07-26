﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Strict.Language.Expressions;

public sealed class Binary : MethodCall
{
	public Binary(Expression left, Method operatorMethod, Expression right) : base(left,
		operatorMethod, right) { }

	public Expression Left => Instance!; // TODO: This is a hack, use proper method arguments 0 & 1
	public Expression Right => Arguments[0];
	public override string ToString() => Left + " " + Method.Name + " " + Right;

	public static Expression? TryParse(Method.Line line, Stack<Range> postfixTokens) =>
		postfixTokens.Count >= 3
			? BuildBinaryExpression(line, postfixTokens.Pop(), postfixTokens)
			: null;

	private static Expression BuildBinaryExpression(Method.Line line, Range operatorTokenRange, Stack<Range> tokens)
	{
		var right = GetUnaryOrBuildNestedBinary(line, tokens.Pop(), tokens);
		var left = GetUnaryOrBuildNestedBinary(line, tokens.Pop(), tokens);
		if (List.HasMismatchingTypes(left, right))
			throw new MismatchingTypeFound(line);
		var operatorToken = line.Text[operatorTokenRange]; //TODO: make more efficient
		if (operatorToken == "*" && List.HasIncompatibleDimensions(left, right))
			throw new List.ListsHaveDifferentDimensions(line, left + " " + right);
		var operatorMethod = left.ReturnType.Methods.FirstOrDefault(m => m.Name == operatorToken) ?? // TODO: Match operator param types before
			line.Method.GetType(Base.BinaryOperator).Methods.FirstOrDefault(m => m.Name == operatorToken) ??
			throw new NoMatchingOperatorFound(right.ReturnType, operatorToken);
		return new Binary(left, operatorMethod, right);
	}

	private static Expression GetUnaryOrBuildNestedBinary(Method.Line line, Range nextTokenRange,
		Stack<Range> tokens)
	{
		var nextToken = line.Text.GetSpanFromRange(nextTokenRange);//TODO: only needed for operator check, can be done more efficiently
		return nextToken[0].IsSingleCharacterOperator() || nextToken.IsMultiCharacterOperator()
			? BuildBinaryExpression(line, nextTokenRange, tokens)
			: line.Method.TryParseExpression(line, nextTokenRange) ??
			throw new MethodExpressionParser.UnknownExpression(line);
	}

	//TODO; Remove
	//private static Expression TryParseBinary(Method.Line line, IReadOnlyList<string> parts)
	//{
	//	var left = line.Method.TryParseExpression(line, parts[0]) ??
	//		throw new MethodExpressionParser.UnknownExpression(line, parts[0]);
	//	var right = line.Method.TryParseExpression(line, parts[2]) ??
	//		throw new MethodExpressionParser.UnknownExpression(line, parts[2]);
	//	if (List.HasMismatchingTypes(left, right))
	//		throw new MismatchingTypeFound(line, parts[2]);
	//	if (parts[1] == "*" && List.HasIncompatibleDimensions(left, right))
	//		throw new List.ListsHaveDifferentDimensions(line, parts[0] + " " + parts[2]);
	//	CheckForAnyExpressions(line, left, right);
	//	var operatorMethod = left.ReturnType.Methods.FirstOrDefault(m => m.Name == parts[1]) ??
	//		line.Method.GetType(Base.BinaryOperator).Methods.FirstOrDefault(m => m.Name == parts[1]) ??
	//		throw new NoMatchingOperatorFound(left.ReturnType, parts[1]);
	//	return new Binary(left, operatorMethod, right);
	//}

	// TODO: check if this needs to be called anywhere
	private static void CheckForAnyExpressions(Method.Line line, Expression left, Expression right)
	{
		if (left.ReturnType == line.Method.GetType(Base.Any))
			throw new AnyIsNotAllowed(line.Method, left);
		if (right.ReturnType == line.Method.GetType(Base.Any))
			throw new AnyIsNotAllowed(line.Method, right);
	}

	private sealed class AnyIsNotAllowed : Exception
	{
		public AnyIsNotAllowed(Method lineMethod, Expression operand) : base("\n" + lineMethod +
			"\n" + string.Join('\n', lineMethod.bodyLines) + "\noperand=" + operand + ", type=" +
			operand.ReturnType) { }
	}

	public sealed class MismatchingTypeFound : ParsingFailed
	{
		public MismatchingTypeFound(Method.Line line, string error = "") : base(line, error) { }
	}

	public sealed class NoMatchingOperatorFound : Exception
	{
		public NoMatchingOperatorFound(Type leftType, string operatorMethod) : base(nameof(leftType) + "=" + leftType + " or " + Base.BinaryOperator + " does not contain " + operatorMethod) { }
	}
}
