﻿using System.Data;
using Strict.Language;
using Strict.Language.Expressions;

namespace Strict.VirtualMachine;

public sealed class VirtualMachine
{
	private bool conditionFlag;
	private int instructionIndex;
	private bool iteratorInitialized;
	private int loopIterationNumber;
	private IList<Statement> statements = new List<Statement>();
	public Memory Memory { get; private init; } = new();
	public Instance? Returns { get; private set; }

	public VirtualMachine Execute(IList<Statement> allStatements)
	{
		conditionFlag = false;
		instructionIndex = 0;
		statements.Clear();
		Returns = null;
		Memory.Registers.Clear();
		Memory.Variables.Clear();
		return RunStatements(allStatements);
	}

	private VirtualMachine RunStatements(IList<Statement> allStatements)
	{
		statements = allStatements;
		for (instructionIndex = 0; instructionIndex is not -1 && instructionIndex < allStatements.Count;
			instructionIndex++)
			ExecuteStatement(allStatements[instructionIndex]);
		return this;
	}

	private void ExecuteStatement(Statement statement)
	{
		if (TryExecuteReturn(statement))
			return;
		TryStoreInstructions(statement);
		TryLoadInstructions(statement);
		TryLoopInitInstruction(statement);
		TryLoopEndInstruction(statement);
		TryInvokeInstruction(statement);
		TryWriteToListInstruction(statement);
		TryWriteToTableInstruction(statement);
		TryExecuteRest(statement);
	}

	private void TryWriteToTableInstruction(Statement statement)
	{
		if (statement is not WriteToTableStatement writeToTableStatement)
			return;
		Memory.AddToDictionary(writeToTableStatement.Identifier, Memory.Registers[writeToTableStatement.Key],
			Memory.Registers[writeToTableStatement.Value]);
	}

	private void TryWriteToListInstruction(Statement statement)
	{
		if (statement is not WriteToListStatement writeToListStatement)
			return;
		Memory.AddToCollectionVariable(writeToListStatement.Identifier,
			Memory.Registers[writeToListStatement.Register].Value);
	}

	private void TryLoopEndInstruction(Statement statement)
	{
		if (statement is not IterationEndStatement iterationEndStatement)
			return;
		loopIterationNumber--;
		if (loopIterationNumber <= 0)
			return;
		instructionIndex -= iterationEndStatement.Steps + 1;
	}

	private void TryInvokeInstruction(Statement statement)
	{
		if (statement is not InvokeStatement { MethodCall: { } } invokeStatement)
			return;
		var methodStatements = GetByteCodeFromInvokedMethodCall(invokeStatement);
		var instance = new VirtualMachine
		{
			Memory = new Memory
			{
				Registers = Memory.Registers,
				Variables =
					new Dictionary<string, Instance>(
						Memory.Variables.Where(variable => variable.Value.IsMember))
			}
		}.RunStatements(methodStatements).Returns;
		if (instance != null)
			Memory.Registers[invokeStatement.Register] = instance;
	}

	private List<Statement> GetByteCodeFromInvokedMethodCall(InvokeStatement invokeStatement)
	{
		if (invokeStatement.PersistedRegistry == null || invokeStatement.MethodCall == null)
			throw new InvalidExpressionException(); //TODO: Cover this line ncrunch: no coverage
		if (invokeStatement.MethodCall.Instance == null)
			return new ByteCodeGenerator(
					new InvokedMethod(
						((Body)invokeStatement.MethodCall.Method.GetBodyAndParseIfNeeded()).Expressions,
						FormArgumentsForMethodCall(invokeStatement)), invokeStatement.PersistedRegistry).
				Generate();
		var instance = GetVariableInstanceFromMemory(invokeStatement.MethodCall.Instance.ToString());
		return new ByteCodeGenerator(
			new InstanceInvokedMethod(
				((Body)invokeStatement.MethodCall.Method.GetBodyAndParseIfNeeded()).Expressions,
				FormArgumentsForMethodCall(invokeStatement), instance),
			invokeStatement.PersistedRegistry).Generate();
	}

	private Instance GetVariableInstanceFromMemory(string variableIdentifier)
	{
		Memory.Variables.TryGetValue(variableIdentifier, out var methodCallInstance);
		if (methodCallInstance is null)
			throw new VariableNotFoundInMemory();
		return methodCallInstance;
	}

	private Dictionary<string, Instance> FormArgumentsForMethodCall(InvokeStatement invokeStatement)
	{
		var arguments = new Dictionary<string, Instance>();
		if (invokeStatement.MethodCall == null)
			return arguments; // ncrunch: no coverage
		for (var index = 0; index < invokeStatement.MethodCall.Method.Parameters.Count; index++)
		{
			var argument = invokeStatement.MethodCall.Arguments[index];
			var argumentInstance = argument is Value argumentValue
				? new Instance(argumentValue.ReturnType, argumentValue.Data)
				: Memory.Variables[argument.ToString()];
			arguments.Add(invokeStatement.MethodCall.Method.Parameters[index].Name, argumentInstance);
		}
		return arguments;
	}

	private bool TryExecuteReturn(Statement statement)
	{
		if (statement is not ReturnStatement returnStatement)
			return false;
		Returns = Memory.Registers[returnStatement.Register];
		if (!Returns.Value.GetType().IsPrimitive && Returns.Value is not Value)
			return false;
		instructionIndex = -2;
		return true;
	}

	private void TryLoopInitInstruction(Statement statement)
	{
		if (statement is not LoopBeginStatement initLoopStatement)
			return;
		ProcessLoopIndex();
		Memory.Variables.TryGetValue(initLoopStatement.Identifier, out var iterableVariable);
		if (iterableVariable is null)
			return; //ncrunch: no coverage
		if (!iteratorInitialized)
			InitializeIterator(iterableVariable); //TODO: Get rid of this and figure out something better. (LM)
		AlterValueVariable(iterableVariable);
	}

	private void ProcessLoopIndex()
	{
		if (Memory.Variables.ContainsKey("index"))
			Memory.Variables["index"].Value = Convert.ToInt32(Memory.Variables["index"].Value) + 1;
		else
			Memory.Variables.Add("index", new Instance(Base.Number, 0));
	}

	private void InitializeIterator(Instance iterableVariable)
	{
		loopIterationNumber = GetLength(iterableVariable);
		iteratorInitialized = true;
	}

	private static int GetLength(Instance iterableInstance)
	{
		if (iterableInstance.Value is string iterableString)
			return iterableString.Length;
		if (iterableInstance.Value is int or double)
			return Convert.ToInt32(iterableInstance.Value);
		return iterableInstance.ReturnType is { IsIterator: true }
			? ((IEnumerable<Expression>)iterableInstance.Value).Count()
			: 0; //ncrunch: no coverage
	}

	private void AlterValueVariable(Instance iterableVariable)
	{
		var index = Convert.ToInt32(Memory.Variables["index"].Value);
		var value = iterableVariable.Value.ToString();
		if (iterableVariable.ReturnType?.Name == Base.Text && value is not null)
			Memory.Variables["value"] = new Instance(Base.Text, value[index].ToString());
		else if (iterableVariable.ReturnType is GenericTypeImplementation genericIterable &&
			genericIterable.Generic.Name == Base.List)
			Memory.Variables["value"] = new Instance(((List<Expression>)iterableVariable.Value)[index]);
		else if (iterableVariable.ReturnType?.Name == Base.Number)
			Memory.Variables["value"] =
				new Instance(Base.Number, Convert.ToInt32(iterableVariable.Value) + index);
	}

	private void TryStoreInstructions(Statement statement)
	{
		if (statement.Instruction > Instruction.SetLoadSeparator)
			return;
		if (statement is SetStatement setStatement)
			Memory.Registers[setStatement.Register] = setStatement.Instance;
		else if (statement is StoreVariableStatement storeVariableStatement)
			Memory.Variables[storeVariableStatement.Identifier] = storeVariableStatement.Instance;
		else if (statement is StoreFromRegisterStatement storeFromRegisterStatement)
			Memory.Variables[storeFromRegisterStatement.Identifier] =
				Memory.Registers[storeFromRegisterStatement.Register];
	}

	private void TryLoadInstructions(Statement statement)
	{
		if (statement is LoadVariableStatement loadVariableStatement)
			Memory.Registers[loadVariableStatement.Register] =
				Memory.Variables[loadVariableStatement.Identifier];
		else if (statement is LoadConstantStatement loadConstantStatement)
			Memory.Registers[loadConstantStatement.Register] = loadConstantStatement.Instance;
	}

	private void TryExecuteRest(Statement statement)
	{
		if (statement is BinaryStatement binaryStatement)
		{
			if (binaryStatement.IsConditional())
				TryConditionalOperationExecution(binaryStatement);
			else
				TryBinaryOperationExecution(binaryStatement);
		}
		else if (statement is JumpStatement jumpStatement)
			TryJumpOperation(jumpStatement);
	}

	private void TryBinaryOperationExecution(BinaryStatement statement)
	{
		var (right, left) = GetOperands(statement);
		Memory.Registers[statement.Registers[^1]] = statement.Instruction switch
		{
			Instruction.Add => left + right,
			Instruction.Subtract => left - right,
			Instruction.Multiply => new Instance(right.ReturnType,
				Convert.ToDouble(left.Value) * Convert.ToDouble(right.Value)),
			Instruction.Divide => new Instance(right.ReturnType,
				Convert.ToDouble(left.Value) / Convert.ToDouble(right.Value)),
			Instruction.Modulo => new Instance(right.ReturnType,
				Convert.ToDouble(left.Value) % Convert.ToDouble(right.Value)),
			_ => Memory.Registers[statement.Registers[^1]] //ncrunch: no coverage
		};
	}

	private (Instance, Instance) GetOperands(BinaryStatement statement) =>
		Memory.Registers.Count < 2
			? throw new OperandsRequired()
			: (Memory.Registers[statement.Registers[1]], Memory.Registers[statement.Registers[0]]);

	private void TryConditionalOperationExecution(BinaryStatement statement)
	{
		var (right, left) = GetOperands(statement);
		NormalizeValues(right, left);
		conditionFlag = statement.Instruction switch
		{
			Instruction.GreaterThan => left > right,
			Instruction.LessThan => left < right,
			Instruction.Equal => left.Value.Equals(right.Value),
			Instruction.NotEqual => !left.Value.Equals(right.Value),
			_ => false //ncrunch: no coverage
		};
	}

	private static void NormalizeValues(params Instance[] instances)
	{
		foreach (var instance in instances)
		{
			if (instance.Value is not MemberCall member)
				continue;
			if (member.Member.Value != null)
				instance.Value = member.Member.Value;
		}
	}

	private void TryJumpOperation(JumpStatement statement)
	{
		if (conditionFlag && statement.Instruction is Instruction.JumpIfTrue ||
			!conditionFlag && statement.Instruction is Instruction.JumpIfFalse ||
			statement is JumpIfNotZeroStatement jumpIfNotZeroStatement &&
			Convert.ToInt32(Memory.Registers[jumpIfNotZeroStatement.Register].Value) > 0)
		{
			instructionIndex += Convert.ToInt32(((JumpIfStatement)statement).Steps);
		}
		else if (!conditionFlag && statement.Instruction is Instruction.JumpToIdIfFalse ||
			conditionFlag && statement.Instruction is Instruction.JumpToIdIfTrue)
		{
			var id = ((JumpToIdStatement)statement).Id;
			var endIndex = statements.IndexOf(statements.First(jumpStatement =>
				jumpStatement.Instruction is Instruction.JumpEnd &&
				jumpStatement is JumpToIdStatement jumpViaId && jumpViaId.Id == id));
			if (endIndex != -1)
				instructionIndex = endIndex;
		}
	}

	public sealed class OperandsRequired : Exception { }
	private sealed class VariableNotFoundInMemory : Exception { }
}