﻿namespace Strict.VM;

public class Instruction
{
	public Instruction(OperationCode operationCode, int value = 0)
	{
		OperationCode = operationCode;
		Value = value;
	}

	public OperationCode OperationCode { get; set; }
	public int Value { get; set; }
}