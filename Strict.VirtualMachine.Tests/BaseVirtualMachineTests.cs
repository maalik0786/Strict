﻿using Strict.Language;
using Strict.Language.Expressions;
using Strict.Language.Expressions.Tests;
using Strict.Language.Tests;
using Type = Strict.Language.Type;

namespace Strict.VirtualMachine.Tests;

public class BaseVirtualMachineTests : TestExpressions
{
	//ncrunch: no coverage start
	protected static readonly Type NumberType = new TestPackage().FindType(Base.Number)!;
	protected static readonly Type TextType = new TestPackage().FindType(Base.Text)!;
	protected static readonly string[] ArithmeticFunctionExample =
	{
		"has First Number", "has Second Number", "Calculate(operation Text) Number",
		"\tArithmeticFunction(10, 5).Calculate(\"add\") is 15",
		"\tArithmeticFunction(10, 5).Calculate(\"subtract\") is 5",
		"\tArithmeticFunction(10, 5).Calculate(\"multiply\") is 50", "\tif operation is \"add\"",
		"\t\treturn First + Second", "\tif operation is \"subtract\"", "\t\treturn First - Second",
		"\tif operation is \"multiply\"", "\t\treturn First * Second",
		"\tif operation is \"divide\"", "\t\treturn First / Second"
	};
	protected static readonly Statement[] ExpectedStatementsOfArithmeticFunctionExample =
	{
		new StoreStatement(new Instance(NumberType, 10), "First"),
		new StoreStatement(new Instance(NumberType, 5), "Second"),
		new StoreStatement(new Instance(TextType, "add"), "operation"),
		new LoadVariableStatement(Register.R0, "operation"),
		new LoadConstantStatement(Register.R1, new Instance(TextType, "add")),
		new(Instruction.Equal, Register.R0, Register.R1),
		new JumpViaIdStatement(Instruction.JumpToIdIfFalse, 0),
		new LoadVariableStatement(Register.R2, "First"),
		new LoadVariableStatement(Register.R3, "Second"),
		new(Instruction.Add, Register.R2, Register.R3, Register.R0),
		new ReturnStatement(Register.R0), new JumpViaIdStatement(Instruction.JumpEnd, 0),
		new LoadVariableStatement(Register.R1, "operation"),
		new LoadConstantStatement(Register.R2, new Instance(TextType, "subtract")),
		new(Instruction.Equal, Register.R1, Register.R2),
		new JumpViaIdStatement(Instruction.JumpToIdIfFalse, 1),
		new LoadVariableStatement(Register.R3, "First"),
		new LoadVariableStatement(Register.R0, "Second"),
		new(Instruction.Subtract, Register.R3, Register.R0, Register.R1),
		new ReturnStatement(Register.R1), new JumpViaIdStatement(Instruction.JumpEnd, 1),
		new LoadVariableStatement(Register.R2, "operation"),
		new LoadConstantStatement(Register.R3, new Instance(TextType, "multiply")),
		new(Instruction.Equal, Register.R2, Register.R3),
		new JumpViaIdStatement(Instruction.JumpToIdIfFalse, 2),
		new LoadVariableStatement(Register.R0, "First"),
		new LoadVariableStatement(Register.R1, "Second"),
		new(Instruction.Multiply, Register.R0, Register.R1, Register.R2),
		new ReturnStatement(Register.R2), new JumpViaIdStatement(Instruction.JumpEnd, 2),
		new LoadVariableStatement(Register.R3, "operation"),
		new LoadConstantStatement(Register.R0, new Instance(TextType, "divide")),
		new(Instruction.Equal, Register.R3, Register.R0),
		new JumpViaIdStatement(Instruction.JumpToIdIfFalse, 3),
		new LoadVariableStatement(Register.R1, "First"),
		new LoadVariableStatement(Register.R2, "Second"),
		new(Instruction.Divide, Register.R1, Register.R2, Register.R3),
		new ReturnStatement(Register.R3), new JumpViaIdStatement(Instruction.JumpEnd, 3)
	};
	protected static readonly string[] SimpleLoopExample =
	{
		"has number", "GetMultiplicationOfNumbers Number", "\tlet result = Mutable(1)",
		"\tlet multiplier = 2", "\tfor number", "\t\tresult = result * multiplier", "\tresult"
	};
	protected static readonly string[] RemoveParenthesesKata =
	{
		"has text", "Remove Text", "\tlet result = Mutable(\"\")", "\tlet count = Mutable(0)",
		"\tfor text", "\t\tif value is \"(\"", "\t\t\tcount = count + 1", "\t\tif value is \")\"",
		"\t\t\tcount = count - 1", "\t\tif count is 0", "\t\t\tresult = result + value", "\tresult"
	};
	protected static readonly Statement[] ExpectedStatementsOfRemoveParanthesesKata =
	{
		new StoreStatement(new Instance(TextType, "some(thing)"), "text"),
		new StoreStatement(new Instance(TextType, "\"\""), "result"),
		new StoreStatement(new Instance(NumberType, 0), "count"),
		new LoadConstantStatement(Register.R0, new Instance(NumberType, 11)),
		new LoadConstantStatement(Register.R1, new Instance(NumberType, 1)),
		new InitLoopStatement("text"), new LoadVariableStatement(Register.R2, "value"),
		new LoadConstantStatement(Register.R3, new Instance(TextType, "(")),
		new(Instruction.Equal, Register.R2, Register.R3),
		new JumpViaIdStatement(Instruction.JumpToIdIfFalse, 0),
		new LoadVariableStatement(Register.R2, "count"),
		new LoadConstantStatement(Register.R3, new Instance(NumberType, 1)),
		new(Instruction.Add, Register.R2, Register.R3, Register.R2),
		new JumpViaIdStatement(Instruction.JumpEnd, 0),
		new LoadVariableStatement(Register.R3, "value"),
		new LoadConstantStatement(Register.R2, new Instance(TextType, ")")),
		new(Instruction.Equal, Register.R3, Register.R2),
		new JumpViaIdStatement(Instruction.JumpToIdIfFalse, 1),
		new LoadVariableStatement(Register.R3, "count"),
		new LoadConstantStatement(Register.R2, new Instance(NumberType, 1)),
		new(Instruction.Subtract, Register.R3, Register.R2, Register.R3),
		new JumpViaIdStatement(Instruction.JumpEnd, 1),
		new LoadVariableStatement(Register.R2, "count"),
		new LoadConstantStatement(Register.R3, new Instance(NumberType, 0)),
		new(Instruction.Equal, Register.R2, Register.R3),
		new JumpViaIdStatement(Instruction.JumpToIdIfFalse, 2),
		new LoadVariableStatement(Register.R2, "result"),
		new LoadVariableStatement(Register.R3, "value"),
		new(Instruction.Add, Register.R2, Register.R3, Register.R2),
		new JumpViaIdStatement(Instruction.JumpEnd, 2),
		new(Instruction.Subtract, Register.R0, Register.R1, Register.R0),
		new JumpStatement(Instruction.JumpIfNotZero, -26), new ReturnStatement(Register.R2)
	};
	//ncrunch: no coverage end

	protected MethodCall GenerateMethodCallFromSource(string programName, string methodCall,
		params string[] source)
	{
		if (type.Package.FindDirectType(programName) == null)
			new Type(type.Package, new TypeLines(programName, source)).ParseMembersAndMethods(
				new MethodExpressionParser());
		return (MethodCall)ParseExpression(methodCall);
	}
}