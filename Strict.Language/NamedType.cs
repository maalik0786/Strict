﻿namespace Strict.Language;

public abstract class NamedType
{
	protected NamedType(Context definedIn, ReadOnlySpan<char> nameAndType, Type? typeFromValue = null)
	{
		if (typeFromValue == null)
		{
			var parts = nameAndType.Split();
			parts.MoveNext();
			Name = parts.Current.ToString();
			if (!Name.IsWord())
				throw new Context.NameMustBeAWordWithoutAnySpecialCharactersOrNumbers(Name);
			if (Name.IsKeyword())
				throw new CannotUseKeywordsAsName(Name);
			Type = definedIn.GetType(parts.MoveNext()
				? GetTypeName(nameAndType[(Name.Length + 1)..].ToString())
				: Name.MakeFirstLetterUppercase());
		}
		else
		{
			Name = nameAndType.ToString();
			Type = typeFromValue;
			if (Name.Contains(' '))
				throw new AssignmentWithInitializerTypeShouldNotHaveNameWithType(Name);
			if (!Name.IsWord())
				throw new Context.NameMustBeAWordWithoutAnySpecialCharactersOrNumbers(Name);
		}
		if (!Name.Length.IsWithinLimit())
			throw new NameLengthIsNotWithinTheAllowedLimit(Name);
	}

	public sealed class CannotUseKeywordsAsName : Exception
	{
		public CannotUseKeywordsAsName(string name) : base(name + " is a keyword and cannot be used as a identifier name. Keywords List: " + Keyword.GetAllKeywords.ToWordList()) { }
	}

	private static string GetTypeName(string typeName)
	{
		if (typeName.StartsWith(Base.List + "(", StringComparison.Ordinal) && !typeName.Contains(Context.DoubleOpenBrackets))
			throw new ListPrefixIsNotAllowedUseImplementationTypeNameInPlural(typeName);
		return typeName;
	}

	public bool IsMutable { get; protected init; }

	public sealed class ListPrefixIsNotAllowedUseImplementationTypeNameInPlural : Exception
	{
		public ListPrefixIsNotAllowedUseImplementationTypeNameInPlural(string typeName) : base($"List should not be used as prefix for {typeName} instead use {typeName.GetTextInsideBrackets()}s") { }
	}

	public sealed class AssignmentWithInitializerTypeShouldNotHaveNameWithType : Exception
	{
		public AssignmentWithInitializerTypeShouldNotHaveNameWithType(string name) : base(name) { }
	}

	public sealed class NameLengthIsNotWithinTheAllowedLimit : Exception
	{
		public NameLengthIsNotWithinTheAllowedLimit(string name) : base($"Name {name} length is {name.Length} but allowed limit is between {Limit.NameMinLimit} and {Limit.NameMaxLimit}") { }
	}

	public string Name { get; }
	public Type Type { get; protected set; }
	public override bool Equals(object? obj) => obj is NamedType other && Name == other.Name;
	public override int GetHashCode() => Name.GetHashCode();
	public override string ToString() => Name + " " + Type;
}