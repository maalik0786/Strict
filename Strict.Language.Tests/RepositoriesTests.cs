using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using NUnit.Framework;
using Strict.Language.Expressions;

namespace Strict.Language.Tests;

[MemoryDiagnoser]
[SimpleJob(RunStrategy.Throughput, warmupCount: 1, targetCount: 10)]
public class RepositoriesTests
{
	[Test]
	public void InvalidPathWontWork() =>
		Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
			repos.LoadFromPath(nameof(InvalidPathWontWork)));

	private readonly Repositories repos = new(new ExpressionParserTests());

	[Test]
	public void LoadingNonGithubPackageWontWork() =>
		Assert.ThrowsAsync<Repositories.OnlyGithubDotComUrlsAreAllowedForNow>(() =>
			repos.LoadFromUrl(new Uri("https://google.com")));

	[Test]
	public async Task LoadStrictBaseTypes()
	{
		var strictPackage = await repos.LoadFromUrl(Repositories.StrictUrl);
		var basePackage = strictPackage.FindSubPackage(nameof(Base))!;
		Assert.That(basePackage.FindDirectType(Base.Any), Is.Not.Null);
		Assert.That(basePackage.FindDirectType(Base.Number), Is.Not.Null);
		Assert.That(basePackage.FindDirectType(Base.App), Is.Not.Null);
	}

	[Test]
	public async Task LoadingSameRepositoryAgainUsesCache()
	{
		var tasks = new List<Task<Package>>();
		for (var index = 0; index < 10; index++)
			tasks.Add(repos.LoadFromUrl(Repositories.StrictUrl));
		await Task.WhenAll(tasks);
		foreach (var task in tasks)
			Assert.That(task.Result, Is.EqualTo(tasks[0].Result));
	}

	[Test]
	public async Task MakeSureParsingFailedErrorMessagesAreClickable()
	{
		var parser = new MethodExpressionParser();
		var strictPackage = await new Repositories(parser).LoadFromUrl(Repositories.StrictUrl);
		Assert.That(
			() => new Type(strictPackage.FindSubPackage("Examples")!,
				new TypeLines("Invalid", "has 1")).ParseMembersAndMethods(null!),
			Throws.InstanceOf<ParsingFailed>().With.Message.Contains(@"at Strict.Examples.Invalid in " +
				Repositories.DevelopmentFolder + @"\Examples\Invalid.strict:line 1"));
	}

	[Test]
	public void SortImplements()
	{
		var file1 = new TypeLines("File1", "implement File2");
		var file2 = new TypeLines("File2", "implement File3", "implement Number");
		var file3 = new TypeLines("File3", "implement Number");
		var filesWithImplements = new Dictionary<string, TypeLines>
		{
			{ file1.Name, file1 }, { file2.Name, file2 }, { file3.Name, file3 }
		};
		Assert.That(
			string.Join(", ",
				new Repositories(null!).SortFilesWithImplements(filesWithImplements).
					Select(file => file.Name)), Is.EqualTo("File3, File2, File1"));
	}

	//ncrunch: no coverage start
	[Test]
	[Category("Slow")]
	[Benchmark]
	public async Task LoadingZippedStrictBase()
	{
		var zipFilePath = Path.Combine(Repositories.DevelopmentFolder, "Base.zip");
		if (!File.Exists(zipFilePath))
			ZipFile.CreateFromDirectory(BaseFolder, zipFilePath);
		for (var iteration = 0; iteration < MaxIterations; iteration++)
		{
			var tasks = new List<Task>();
			foreach (var entry in ZipFile.OpenRead(zipFilePath).Entries)
				tasks.Add(new StreamReader(entry.Open()).ReadToEndAsync());
			await Task.WhenAll(tasks);
		}
	}

	private static string BaseFolder => Path.Combine(Repositories.DevelopmentFolder, "Base");
	private const int MaxIterations = 1000;

	[Test]
	[Category("Slow")]
	[Benchmark]
	public void LoadingAllStrictFilesSequentially()
	{
		for (var iteration = 0; iteration < MaxIterations; iteration++)
			foreach (var file in Directory.GetFiles(BaseFolder, "*.strict"))
				File.ReadAllLines(file);
	}

	/// <summary>
	/// By far the slowest way (2-3x slower) to load files (even though ReSharper suggests it)
	/// </summary>
	[Test]
	[Category("Slow")]
	[Benchmark]
	public async Task LoadingAllStrictFilesAsync()
	{
		for (var iteration = 0; iteration < MaxIterations; iteration++)
			foreach (var file in Directory.GetFiles(BaseFolder, "*.strict"))
				await File.ReadAllLinesAsync(file);
	}

	[Test]
	[Category("Slow")]
	[Benchmark]
	public async Task LoadingAllStrictFilesAsyncInParallel()
	{
		for (var iteration = 0; iteration < MaxIterations; iteration++)
		{
			var tasks = new List<Task>();
			foreach (var file in Directory.GetFiles(BaseFolder, "*.strict"))
				tasks.Add(File.ReadAllLinesAsync(file));
			await Task.WhenAll(tasks);
		}
	}

	[Test]
	[Category("Slow")]
	[Benchmark]
	public async Task LoadingAllStrictFilesAllTextAsyncParallel()
	{
		for (var iteration = 0; iteration < MaxIterations; iteration++)
		{
			var tasks = new List<Task>();
			foreach (var file in Directory.GetFiles(BaseFolder, "*.strict"))
			{
				async void Action() => (await File.ReadAllTextAsync(file)).Split("\n");
				tasks.Add(new Task(Action));
			}
			await Task.WhenAll(tasks);
		}
	}

	/// <summary>
	/// Zip file loading makes a difference (4-5 times faster), but otherwise there is close to zero
	/// impact how we load the files, parallel or not, async is only 10-20% faster and not important.
	/// </summary>
	[Test]
	[Category("Manual")]
	public void LoadingFilesPerformance() => BenchmarkRunner.Run<RepositoriesTests>();
}