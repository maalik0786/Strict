using System.Linq;
using NUnit.Framework;

namespace Strict.Language.Expressions.Tests;

public class BinaryTests : TestExpressions
{
	[Test]
	public void ParseBinary() =>
		ParseAndCheckOutputMatchesInput("5 + 5",
			new Binary(number, number.ReturnType.Methods.First(m => m.Name == "+"), number));

	[Test]
	public void Add5ToNumber() =>
		ParseAndCheckOutputMatchesInput("bla + 5",
			new Binary(new MemberCall(bla), number.ReturnType.Methods.First(m => m.Name == "+"),
				number));

	[Test]
	public void MissingLeftExpression() =>
		Assert.That(() => ParseExpression("unknown + 5"),
			Throws.Exception.InstanceOf<MemberCall.MemberNotFound>());

	[Test]
	public void MissingRightExpression() =>
		Assert.That(() => ParseExpression("5 + unknown"),
			Throws.Exception.InstanceOf<MemberCall.MemberNotFound>());
}