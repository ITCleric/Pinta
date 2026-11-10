using System;
using System.Linq;
using System.Text.Json;

namespace Pinta.Core;

/// <summary>A portable theme. Colors are restricted to hex to keep generated CSS safe.</summary>
public sealed record ThemePalette (
	string Background = "#eef2f6",
	string Surface = "#ffffff",
	string Foreground = "#202830",
	string Accent = "#0078d4",
	string Border = "#b6c4d2",
	string Canvas = "#8d98a4",
	string Selection = "#cce8ff")
{
	public static ThemePalette Light { get; } = new ();
	public static ThemePalette Dark { get; } = new (
		"#202428", "#2b3036", "#edf1f5", "#60b9ff", "#46515d", "#15181c", "#244b68");

	public bool IsValid => new[] { Background, Surface, Foreground, Accent, Border, Canvas, Selection }.All (IsColor);

	public static bool IsColor (string? value) => value is { Length: 7 } && value[0] == '#' && value.Skip (1).All (Uri.IsHexDigit);

	public string ToJson () => JsonSerializer.Serialize (this, new JsonSerializerOptions { WriteIndented = true });

	public static ThemePalette FromJson (string json)
	{
		var result = JsonSerializer.Deserialize<ThemePalette> (json) ?? throw new FormatException ("Empty theme.");
		return result.IsValid ? result : throw new FormatException ("Theme colors must use #RRGGBB.");
	}

	public string ToCss ()
	{
		if (!IsValid) throw new FormatException ("Theme colors must use #RRGGBB.");
		return $$"""
		@define-color window_bg_color {{Background}};
		@define-color window_fg_color {{Foreground}};
		@define-color view_bg_color {{Surface}};
		@define-color view_fg_color {{Foreground}};
		@define-color headerbar_bg_color {{Background}};
		@define-color headerbar_fg_color {{Foreground}};
		@define-color card_bg_color {{Surface}};
		@define-color card_fg_color {{Foreground}};
		@define-color popover_bg_color {{Surface}};
		@define-color popover_fg_color {{Foreground}};
		@define-color accent_bg_color {{Accent}};
		@define-color accent_color {{Accent}};
		window.pinta-modern { background-color: {{Background}}; color: {{Foreground}}; }
		.pinta-modern #workspace_layout, .pinta-modern .canvas-viewport { background-color: {{Canvas}}; }
		.pinta-modern #main_toolbar, .pinta-modern #statusbar, .pinta-modern #tool_toolbar {
		  background-color: {{Background}}; border-bottom: 1px solid {{Border}}; padding: 2px 4px;
		}
		.pinta-modern button { border-radius: 3px; min-height: 22px; padding: 3px; }
		.pinta-modern button:checked { background-color: {{Selection}}; box-shadow: inset 0 0 0 1px {{Accent}}; }
		.pinta-modern .tool-box-button { -gtk-icon-size: 20px; min-width: 24px; min-height: 24px; padding: 3px; }
		.pinta-modern notebook header { background-color: {{Background}}; }
		.pinta-modern listview { background-color: {{Surface}}; color: {{Foreground}}; }
		.pinta-modern listview row:selected { background-color: {{Selection}}; color: {{Foreground}}; }
		.pinta-modern .floating-palette { background-color: {{Background}}; border: 1px solid {{Border}}; border-radius: 4px; box-shadow: 0 2px 8px alpha(black, .22); }
		.pinta-modern .palette-swatch { min-width: 12px; min-height: 12px; padding: 0; border-radius: 0; }
		.pinta-modern separator { background-color: {{Border}}; }
		""";
	}
}
