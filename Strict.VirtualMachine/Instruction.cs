﻿namespace Strict.VirtualMachine;

public enum Instruction
{
	Set,
	StoreVariable,
	StoreConstant,
	StoreFromRegister,
	Load,
	LoadConstant,
	InitLoopStatement,
	SetLoadSeparator = 100,
	Add,
	Subtract,
	Multiply,
	Divide,
	Modulo,
	BinaryOperatorsSeparator = 200,
	GreaterThan,
	LessThan,
	Equal,
	NotEqual,
	ConditionalSeparator = 300,
	JumpIfTrue,
	JumpIfFalse,
	JumpIfNotZero,
	JumpEnd,
	JumpToIdIfFalse,
	JumpToIdIfTrue,
	JumpsSeparator = 400,
	Return
}