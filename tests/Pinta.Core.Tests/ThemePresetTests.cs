using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class ThemePresetTests
{
	private static IEnumerable<ThemePreset> Presets => ThemePresets.All;

	[TestCaseSource (nameof (Presets))]
	public void ExportImportPreservesSkinAndEditableColors (ThemePreset preset)
	{
		var edited = preset.Palette with { Accent = "#4268ab", Canvas = "#506070" };
		var imported = ThemePalette.FromJson (edited.ToJson ());
		Assert.That (imported, Is.EqualTo (edited));
		Assert.That (imported.Style, Is.EqualTo (preset.Palette.Style));
		Assert.That (preset.Palette.IsValid, Is.True);
	}

	[Test]
	public void LegacySevenColorJsonRetainsItsColorsAndUsesWindowsSkin ()
	{
		var imported = ThemePalette.FromJson ("{\"Background\":\"#123456\",\"Surface\":\"#234567\",\"Foreground\":\"#ffffff\",\"Accent\":\"#456789\",\"Border\":\"#56789a\",\"Canvas\":\"#6789ab\",\"Selection\":\"#789abc\"}");
		Assert.That (imported.Style, Is.EqualTo (ThemeStyle.Windows10));
		Assert.That (imported.Background, Is.EqualTo ("#123456"));
		Assert.That (imported.Selection, Is.EqualTo ("#789abc"));
	}

	[Test]
	public void UnknownNumericSkinCannotProduceCss ()
	{
		Assert.Throws<FormatException> (() => ThemePalette.FromJson ("{\"Style\":999}"));
		Assert.Throws<FormatException> (() => (ThemePalette.Light with { Style = (ThemeStyle) 999 }).ToCss ());
	}
}
