using System;
using System.Text.Json;
using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class ThemePaletteTests
{
	[Test]
	public void RoundTripPreservesCustomColors ()
	{
		var theme = ThemePalette.Dark with { Accent = "#eab123", Canvas = "#100a23" };
		Assert.That (ThemePalette.FromJson (theme.ToJson ()), Is.EqualTo (theme));
		Assert.That (theme.ToCss (), Does.Contain ("#eab123").And.Contain ("#100a23"));
	}

	[TestCase ("red")]
	[TestCase ("#fff")]
	[TestCase ("#fffffff")]
	[TestCase ("#gg0000")]
	[TestCase ("#000000; } window { opacity: 0" )]
	public void InvalidColorsCannotGenerateCss (string value)
	{
		var theme = new ThemePalette (Accent: value);
		Assert.That (theme.IsValid, Is.False);
		Assert.Throws<FormatException> (() => theme.ToCss ());
		Assert.Throws<FormatException> (() => ThemePalette.FromJson (theme.ToJson ()));
	}

	[Test]
	public void RejectsNullAndMalformedInput ()
	{
		Assert.Throws<FormatException> (() => ThemePalette.FromJson ("null"));
		Assert.Throws<JsonException> (() => ThemePalette.FromJson ("["));
		Assert.Throws<FormatException> (() => ThemePalette.FromJson ("{\"Accent\": null}"));
	}

	[TestCase ("<Primary>H", "<Primary><Alt>H")]
	[TestCase ("<Primary>M", "<Primary><Alt>M")]
	[TestCase ("<Primary>Y", "<Primary><Shift>Z")]
	[TestCase ("<Primary><Shift>Z", "<Primary><Alt>Z")]
	[TestCase ("<Primary>F", "<Primary>F")]
	public void MacAlternativesKeepSystemAndRedoKeysDistinct (string source, string mac)
	{
		Assert.That (ShortcutPolicy.ForPlatform (source, true), Is.EqualTo (mac));
		Assert.That (ShortcutPolicy.ForPlatform (source, false), Is.EqualTo (source));
	}
}
