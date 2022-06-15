﻿using System.IO;
using System.Threading.Tasks;
using ManagedCuda;
using ManagedCuda.NVRTC;
using NUnit.Framework;
using Strict.Language;
using Strict.Language.Expressions;

namespace Strict.Compiler.Cuda.Tests;

public class CSharpToCudaTranspilerTests
{
	[SetUp]
	public async Task CreateTranspiler() =>
		transpiler = new CSharpToCudaTranspiler(await GetStrictPackage());

	private static async Task<Package> GetStrictPackage() =>
		cachedStrictPackage ??= await GetRepositories().LoadFromUrl(Repositories.StrictUrl);

	private static Package? cachedStrictPackage;
	private static Repositories GetRepositories() => new(new MethodExpressionParser());
	private CSharpToCudaTranspiler transpiler = null!;

	[TestCase("")]
	public void EmptyInputWillNotWork(string input) =>
		Assert.That(() => transpiler.Convert(input),
			Throws.InstanceOf<CSharpToCudaTranspiler.InvalidCode>());

	[Ignore("")]
	[Test]
	public void GenerateInitializeDepthsCuda()
	{
		var inputCode = File.ReadAllText(@"..\..\..\Input\InitializeDepths.cs");
		var expectedOutput = File.ReadAllText(@"..\..\..\Output\InitializeDepths.cu");
		Assert.That(transpiler.Convert(inputCode), Is.EqualTo(expectedOutput));
	}

	[Test]
	public void ParseAddNumbers()
	{
		var type = GetParsedCSharpType(AddNumbers);
		Assert.That(type.Name, Is.EqualTo(AddNumbers));
		Assert.That(type.Methods, Has.Count.EqualTo(1));
		Assert.That(type.Methods[0].Name, Is.EqualTo("Add"));
		Assert.That(type.Methods[0].Parameters[1].Type, Is.EqualTo(type.FindType(Base.Number)));
		Assert.That(type.Methods[0].ReturnType, Is.EqualTo(type.FindType(Base.Number)));
		Assert.That(type.Methods[0].Body.Expressions[0].ToString(),
			Is.EqualTo("return first + second"));
	}

	//TODO: Add a unit test which returns MissingReturnStatement using invalid file
	private Type GetParsedCSharpType(string fileName) =>
		transpiler.ParseCSharp(@"..\..\..\Input\" + fileName + ".cs");

	private static CudaDeviceVariable<int> CreateAndRunKernel(CudaRuntimeCompiler rtc)
	{
		var context = new CudaContext(0);
		const int Count = 1;
		CudaDeviceVariable<int> first = new[] { 1 };
		CudaDeviceVariable<int> second = new[] { 2 };
		var output = new CudaDeviceVariable<int>(Count);
		var kernel = context.LoadKernelPTX(rtc.GetPTX(), AddNumbers);
		kernel.Run(first.DevicePointer, second.DevicePointer, output.DevicePointer, Count);
		return output;
	}

	private static CudaRuntimeCompiler CompileKernelAndSaveAsPtxFile(string code)
	{
		//generate as output language obviously from strict code
		var rtc = new CudaRuntimeCompiler(code, AddNumbers);
		// see http://docs.nvidia.com/cuda/nvrtc/index.html for usage and options
		//https://arnon.dk/matching-sm-architectures-arch-and-gencode-for-various-nvidia-cards/
		//nvcc .\vectorAdd.cu -use_fast_math -ptx -m 64 -arch compute_61 -code sm_61 -o .\vectorAdd.ptx
		rtc.Compile(new[] { "--gpu-architecture=compute_61" });
		return rtc;
	}

	[Category("Slow")]
	[TestCase(AddNumbers, 3)]
	[TestCase(SubtractNumbers, -1)]
	[TestCase(MultiplyNumbers, 2)]
	public void ParseGenerateCudaAndExecute(string fileName, int expectedNumber)
	{
		var type = GetParsedCSharpType(fileName);
		var cuda = transpiler.GenerateCuda(type);
		var expectedOutput = File.ReadAllText(@"..\..\..\Output\" + fileName + ".cu");
		Assert.That(cuda, Is.EqualTo(expectedOutput));
		var rtc = CompileKernelAndSaveAsPtxFile(cuda);
		var output = CreateAndRunKernel(rtc);
		Assert.That(output[0], Is.EqualTo(expectedNumber));
	}

	private const string AddNumbers = nameof(AddNumbers);
	private const string SubtractNumbers = nameof(SubtractNumbers);
	private const string MultiplyNumbers = nameof(MultiplyNumbers);
}