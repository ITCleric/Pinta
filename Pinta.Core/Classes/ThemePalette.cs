using System;
using System.Linq;
using System.Text.Json;

namespace Pinta.Core;

/// <summary>A portable theme. Colors are restricted to hex to keep generated CSS safe.</summary>
public sealed record ThemePalette (
	string Background = "#f7f7f7",
	string Surface = "#ffffff",
	string Foreground = "#252525",
	string Accent = "#1685c5",
	string Border = "#a6a6a6",
	string Canvas = "#cfcfcf",
	string Selection = "#c9e7f7")
{
	public static ThemePalette Light { get; } = new ();
	public static ThemePalette Dark { get; } = new (
		"#252525", "#303030", "#ededed", "#419bd0", "#555e64", "#202020", "#245675");

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
		bool dark = Brightness (Background) < 128;
		string paletteHeader = dark ? $"mix({Background}, {Accent}, 0.18)" : Accent;
		string headerText = dark ? Foreground : Brightness (Accent) > 165 ? "#17212a" : "#ffffff";
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
		window.pinta-modern { background-color: {{Background}}; color: {{Foreground}}; font-family: "Segoe UI", "Helvetica Neue", sans-serif; font-size: 12px; }
		.pinta-modern #workspace_layout, .pinta-modern .canvas-viewport { background-color: {{Canvas}}; }
		.pinta-modern .command-deck { background-color: {{Background}}; padding: 0 3px; border-bottom: 1px solid {{Border}}; }
		.pinta-modern .editor-menubar { background: none; padding: 0; min-height: 23px; }
		.pinta-modern .editor-menubar > item { padding: 2px 7px; min-height: 19px; border-radius: 0; }
		.pinta-modern #main_toolbar { padding: 0 3px 2px; min-height: 25px; background: none; }
		.pinta-modern #tool_toolbar { background-color: {{Background}}; border-bottom: 1px solid {{Border}}; padding: 1px 5px; min-height: 27px; }
		.pinta-modern #statusbar { background-color: {{Background}}; border-top: 1px solid {{Border}}; padding: 0 4px; min-height: 23px; }
		.pinta-modern button { border-radius: 0; min-height: 20px; padding: 2px 4px; }
		.pinta-modern button:checked { background-color: {{Selection}}; box-shadow: inset 0 0 0 1px {{Accent}}; }
		.pinta-modern .toolbar button { -gtk-icon-size: 16px; min-width: 20px; min-height: 20px; padding: 1px 3px; }
		.pinta-modern .toolbar entry, .pinta-modern .toolbar spinbutton, .pinta-modern .toolbar dropdown > button,
		.pinta-modern .floating-palette combobox > box > button, .pinta-modern .floating-palette entry {
		  border-radius: 0; min-height: 20px; padding: 0 3px; box-shadow: none;
		}
		.pinta-modern .tool-box-button { -gtk-icon-size: 18px; min-width: 20px; min-height: 20px; padding: 1px; border: 1px solid transparent; }
		.pinta-modern .tool-box-button:hover { background-color: alpha({{Selection}}, 0.6); border-color: {{Accent}}; }
		.pinta-modern .tool-box-button:checked { border-color: {{Accent}}; }
		.pinta-modern .floating-palette { background-color: {{Surface}}; border: 1px solid {{Border}}; border-radius: 0; box-shadow: 1px 3px 9px alpha(black, .28); }
		.pinta-modern .palette-titlebar { background-color: {{paletteHeader}}; color: {{headerText}}; min-height: 23px; padding: 0 2px; }
		.pinta-modern .palette-titlebar label { font-size: 12px; font-weight: normal; padding: 0 2px; }
		.pinta-modern .palette-titlebar button { -gtk-icon-size: 10px; background: none; color: {{headerText}}; min-width: 16px; min-height: 17px; padding: 0; margin: 2px 0 2px 2px; border: none; box-shadow: none; }
		.pinta-modern .palette-titlebar button:hover { background-color: #c74b48; color: white; }
		.pinta-modern .palette-toolbar { background-color: {{Background}}; border-top: 1px solid {{Border}}; padding: 1px 2px; }
		.pinta-modern .palette-toolbar button { -gtk-icon-size: 16px; min-width: 20px; min-height: 20px; padding: 1px 2px; }
		.pinta-modern .palette-swatch { min-width: 8px; min-height: 9px; padding: 0; border: none; box-shadow: none; border-radius: 0; }
		.pinta-modern .active-color-swatch { min-width: 26px; min-height: 26px; padding: 2px; background-color: {{Surface}}; border: 1px solid {{Border}}; }
		.pinta-modern .active-color-swatch.selected-color { border-color: {{Foreground}}; }
		.pinta-modern .color-swap { -gtk-icon-size: 10px; min-width: 12px; min-height: 12px; padding: 0; background: none; border: none; box-shadow: none; }
		.pinta-modern .workspace-toggles { padding: 3px 0 0; }
		.pinta-modern .workspace-toggles button { -gtk-icon-size: 16px; min-width: 20px; min-height: 20px; padding: 1px 2px; background: none; }
		.pinta-modern .workspace-toggles button:checked { background-color: {{Selection}}; }
		.pinta-modern .document-thumbnails { padding: 3px; }
		.pinta-modern .document-preview-button { min-height: 56px; min-width: 82px; padding: 3px; border: 1px solid transparent; background: none; box-shadow: none; }
		.pinta-modern .document-preview-button:hover { border-color: {{Border}}; background-color: alpha({{Selection}}, .4); }
		.pinta-modern .document-preview-button:checked { border-color: {{Accent}}; background-color: {{Selection}}; box-shadow: inset 0 0 0 1px {{Surface}}; }
		.pinta-modern .document-close { -gtk-icon-size: 10px; min-width: 15px; min-height: 15px; padding: 0; margin: 2px; border: none; background-color: {{Surface}}; opacity: 0; }
		.pinta-modern .document-thumbnail:hover .document-close, .pinta-modern .document-close:focus-visible { opacity: 1; }
		.pinta-modern .document-close:hover { background-color: #c74b48; color: white; }
		.pinta-modern .document-dirty { color: {{Accent}}; font-size: 20px; margin: 0 0 0 4px; }
		.pinta-modern listview { background-color: {{Surface}}; color: {{Foreground}}; }
		.pinta-modern listview row { padding: 0 2px; border-radius: 0; }
		.pinta-modern listview row:selected { background-color: {{Selection}}; color: {{Foreground}}; box-shadow: inset 0 0 0 1px alpha({{Accent}}, .5); }
		.pinta-modern .status-zoom-slider { padding: 0 3px; min-height: 20px; }
		.pinta-modern .status-zoom-slider trough { min-height: 3px; border-radius: 0; }
		.pinta-modern .status-zoom-slider slider { min-width: 7px; min-height: 13px; border-radius: 1px; }
		.pinta-modern separator { background-color: {{Border}}; }
		.pinta-modern .effect-content { padding: 6px; }

		""";
	}
	private static double Brightness (string color) =>
		.2126 * Convert.ToInt32 (color.Substring (1, 2), 16) +
		.7152 * Convert.ToInt32 (color.Substring (3, 2), 16) +
		.0722 * Convert.ToInt32 (color.Substring (5, 2), 16);

}
