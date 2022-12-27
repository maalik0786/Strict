﻿using NUnit.Framework;
using static Strict.Language.Expressions.ConstantDeclaration;

namespace Strict.Language.Expressions.Tests;

public sealed class MutableTests : TestExpressions
{
	[SetUp]
	public void CreateParser() => parser = new MethodExpressionParser();

	private MethodExpressionParser parser = null!;

	[Test]
	public void MutableMemberConstructorWithType()
	{
		var program = new Type(type.Package,
				new TypeLines(nameof(MutableMemberConstructorWithType), "mutable something Number",
					"Add(input Number) Number",
					"\tconstant result = something + input")).
			ParseMembersAndMethods(parser);
		Assert.That(program.Members[0].IsMutable, Is.True);
		Assert.That(program.Methods[0].GetBodyAndParseIfNeeded().ReturnType,
			Is.EqualTo(type.GetType(Base.Number)));
	}

	//TODO: Mutable method parameter has valid any use case? should it be mutable input Number?
	//[Test]
	//public void MutableMethodParameterWithType()
	//{
	//	var program = new Type(type.Package,
	//			new TypeLines(nameof(MutableMethodParameterWithType), "has something Number",
	//				"Add(input Mutable(Number)) Number",
	//				"\tconstant result = something + input")).
	//		ParseMembersAndMethods(parser);
	//	Assert.That(program.Methods[0].Parameters[0].IsMutable,
	//		Is.True);
	//	Assert.That(program.Methods[0].GetBodyAndParseIfNeeded().ReturnType,
	//		Is.EqualTo(type.GetType(Base.Number)));
	//}

	[Test]
	public void MutableMemberWithTextType()
	{
		var program = new Type(type.Package,
				new TypeLines(nameof(MutableMemberWithTextType), "mutable something Number",
					"Add(input Text) Text",
					"\tconstant result = input + something")).
			ParseMembersAndMethods(parser);
		Assert.That(() => program.Methods[0].GetBodyAndParseIfNeeded(),
			Throws.InstanceOf<Type.ArgumentsDoNotMatchMethodParameters>());
	}
	/*TODO
	[Test]
	public void MutableVariablesWithSameImplementationTypeShouldUseSameType()
	{
		var program = new Type(type.Package,
			new TypeLines(nameof(MutableVariablesWithSameImplementationTypeShouldUseSameType), "has unused Number",
				"UnusedMethod Number",
				"\tmutable first = 5",
				"\tmutable second = 6",
				"\tfirst + second")).ParseMembersAndMethods(parser);
		var body = (Body)program.Methods[0].GetBodyAndParseIfNeeded();
		Assert.That(body.Expressions[0].ReturnType.Name, Is.EqualTo(Base.Mutable + "(TestPackage." + Base.Number + ")"));
		Assert.That(body.Expressions[0].ReturnType, Is.EqualTo(body.Expressions[1].ReturnType));
	}

	[TestCase("AssignNumberToTextType", "mutable something Text",
		"TryChangeMutableDataType Text", "\tsomething = 5")]
	[TestCase("AssignNumbersToTexts", "mutable something Texts",
		"TryChangeMutableDataType Text", "\tsomething = (5, 4, 3)")]
	public void ValueTypeNotMatchingWithAssignmentType(string testName, params string[] code) =>
		Assert.That(
			() => new Type(type.Package, new TypeLines(testName, code)).ParseMembersAndMethods(parser).
				Methods[0].GetBodyAndParseIfNeeded(), Throws.InstanceOf<Mutable.ValueTypeNotMatchingWithAssignmentType>());
	*/

	[Test]
	public void MutableVariableInstanceUsingSpace()
	{
		var program = new Type(type.Package,
				new TypeLines(nameof(MutableVariableInstanceUsingSpace), "has log",
					"Add(input Number) Number",
					"\tmutable result = 5",
					"\tresult = result + input")).
			ParseMembersAndMethods(parser);
		var body = (Body)program.Methods[0].GetBodyAndParseIfNeeded();
		Assert.That(((ConstantDeclaration)body.Expressions[0]).Value.ToString(), Is.EqualTo("5"));
	}

	[Test]
	public void MissingMutableArgument() =>
		Assert.That(
			() => new Type(type.Package,
					new TypeLines(nameof(MissingMutableArgument), "has log", "Add(input Number) Number",
						"\tconstant result =", "\tresult = result + input")).
				ParseMembersAndMethods(parser).
				Methods[0].GetBodyAndParseIfNeeded(),
			Throws.InstanceOf<MissingAssignmentValueExpression>());

	[TestCase("(1, 2, 3)", "Numbers", "MutableTypeWithListArgumentIsAllowed")]
	[TestCase("Range(1, 10).Start", "Number", "MutableTypeWithNestedCallShouldUseBrackets")]
	public void MutableTypeWithListArgumentIsAllowed(string code, string returnType, string testName)
	{
		var program = new Type(type.Package,
				new TypeLines(testName, "has log",
					$"Add(input Number) {returnType}",
					$"\tmutable result = {code}",
					"\tresult = result + input",
					"\tresult")).
			ParseMembersAndMethods(parser);
		var body = (Body)program.Methods[0].GetBodyAndParseIfNeeded();
		Assert.That(((ConstantDeclaration)body.Expressions[0]).Value.ToString(),
			Is.EqualTo(code));
	}

	[Test]
	public void AssignmentWithMutableKeyword()
	{
		var program = new Type(type.Package,
				new TypeLines(nameof(AssignmentWithMutableKeyword), "has something Character",
					"CountEvenNumbers(limit Number) Number",
					"\tmutable counter = 0",
					"\tfor Range(0, limit)",
					"\t\tcounter = counter + 1",
					"\tcounter")).
			ParseMembersAndMethods(parser);
		var body = (Body)program.Methods[0].GetBodyAndParseIfNeeded();
		Assert.That(body.ReturnType,
			Is.EqualTo(type.GetType(Base.Number)));
		Assert.That(body.Expressions[0].ReturnType.Name,
			Is.EqualTo("Number"));
	}

	[Test]
	public void MissingAssignmentValueExpression()
	{
		var program = new Type(type.Package,
				new TypeLines(nameof(MissingAssignmentValueExpression), "has something Character",
					"CountEvenNumbers(limit Number) Number",
					"\tmutable counter =",
					"\tcounter")).
			ParseMembersAndMethods(parser);
		Assert.That(() => program.Methods[0].GetBodyAndParseIfNeeded(),
			Throws.InstanceOf<MissingAssignmentValueExpression>());
	}

	//TODO: Remove since this usecase is not valid anymore
	//[Test]
	//public void DirectUsageOfMutableTypesOrImplementsAreForbidden()
	//{
	//	var program = new Type(type.Package,
	//			new TypeLines(nameof(DirectUsageOfMutableTypesOrImplementsAreForbidden), "has unused Character",
	//				"DummyCount(limit Number) Number",
	//				"\tconstant result = Mutable(5)",
	//				"\tresult")).
	//		ParseMembersAndMethods(parser);
	//	Assert.That(() => program.Methods[0].GetBodyAndParseIfNeeded(),
	//		Throws.InstanceOf<MutableAssignment.DirectUsageOfMutableTypesOrImplementsAreForbidden>()!);
	//}

	[Test]
	public void GenericTypesCannotBeUsedDirectlyUseImplementation()
	{
		var program = new Type(type.Package,
				new TypeLines(nameof(GenericTypesCannotBeUsedDirectlyUseImplementation), "has unused Character",
					"DummyCount Number",
					"\tconstant result = List(5, 5)",
					"\tresult")).
			ParseMembersAndMethods(parser);
		Assert.That(() => program.Methods[0].GetBodyAndParseIfNeeded(),
			Throws.InstanceOf<Type.GenericTypesCannotBeUsedDirectlyUseImplementation>()!);
	}

	[Test]
	public void MemberDeclarationUsingMutableKeyword()
	{
		var program = new Type(type.Package,
				new TypeLines(nameof(MemberDeclarationUsingMutableKeyword), "mutable input = 0",
					"DummyAssignment(limit Number) Number",
					"\tif limit > 5",
					"\t\tinput = 5",
					"\telse",
					"\t\tinput = 10",
					"\tinput")).
			ParseMembersAndMethods(parser);
		program.Methods[0].GetBodyAndParseIfNeeded();
		Assert.That(program.Members[0].IsMutable, Is.True);
		Assert.That(program.Members[0].Value?.ToString(), Is.EqualTo("10"));
	}

	[TestCase("Mutable", "Mutable(Number)")]
	[TestCase("Count", "Count")]
	public void MutableTypesOrImplementsUsageInMembersAreForbidden(string testName, string code) =>
		Assert.That(
			() => new Type(type.Package,
				new TypeLines(testName + nameof(MutableTypesOrImplementsUsageInMembersAreForbidden),
					$"mutable something {code}", "Add(input Count) Number",
					"\tconstant result = something + input")).ParseMembersAndMethods(parser),
			Throws.InstanceOf<ParsingFailed>().With.InnerException.InstanceOf<Context.TypeNotFound>());
}