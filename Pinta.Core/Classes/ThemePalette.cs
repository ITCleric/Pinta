using System;
using System.Linq;
using System.Text.Json;

namespace Pinta.Core;

/// <summary>A portable theme. Colors are restricted to hex to keep generated CSS safe.</summary>
public sealed record ThemePalette (
	string Background = "#f3f3f3",
	string Surface = "#ffffff",
	string Foreground = "#202020",
	string Accent = "#0067c0",
	string Border = "#92979d",
	string Canvas = "#bfc1c4",
	string Selection = "#b8d9f4")
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
		string tileTop = dark ? $"mix({Surface}, white, .06)" : Surface;
		string titleTop = dark ? paletteHeader : $"mix({Accent}, white, .08)";
		string tileBottom = $"mix({Surface}, {Background}, .85)";
		string tileHighlight = dark ? "alpha(white, .12)" : "alpha(white, .85)";
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
		window.pinta-modern { background-color: {{Background}}; color: {{Foreground}}; font-family: "Segoe UI", "Tahoma", "Arial", sans-serif; font-size: 12px; }
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
		.pinta-modern .tool-box-button { -gtk-icon-size: 22px; min-width: 24px; min-height: 24px; padding: 2px; }

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
		.pinta-modern .document-preview-button { min-height: 48px; min-width: 56px; padding: 3px; border: 1px solid {{Border}}; background-color: {{Surface}}; box-shadow: inset 0 1px 0 {{tileHighlight}}; }
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
		.pinta-modern .status-zoom-slider slider { min-width: 9px; min-height: 16px; margin: -6px 0; padding: 0; border: 1px solid {{Accent}}; background-color: mix({{Selection}}, {{Accent}}, .35); border-radius: 0; }
		.pinta-modern separator { background-color: {{Border}}; }
		.pinta-modern .effect-content { padding: 6px; }
		.pinta-modern .effect-identity { padding: 0 0 6px; margin-bottom: 3px; border-bottom: 1px solid {{Border}}; }
		/* Desktop controls: explicit square frames override libadwaita's rounded defaults. */
		.pinta-modern button, .pinta-modern entry, .pinta-modern spinbutton,
		.pinta-modern dropdown > button, .pinta-modern combobox > box > button,
		.pinta-modern popover > contents, .pinta-modern .card, .pinta-modern .boxed-list,
		.pinta-modern .boxed-list > row, .pinta-modern stacksidebar row, .pinta-modern frame,
		.pinta-modern switch, .pinta-modern switch slider, .pinta-modern checkbutton check,
		.pinta-modern scale slider, .pinta-modern progressbar trough, .pinta-modern progressbar progress {
		  border-radius: 0;
		}
		.pinta-modern .editor-toolbar { border-radius: 0; }
		.pinta-modern .editor-toolbar > separator { min-width: 1px; margin: 2px 3px; background-color: {{Border}}; }
		.pinta-modern .editor-tile, .pinta-modern .toolbar .editor-tile,
		.pinta-modern .workspace-toggles .editor-tile, .pinta-modern .tool-box-button.editor-tile {
		  border-radius: 0; border: 1px solid mix({{Border}}, {{Surface}}, .2);
		  background-color: {{Background}};
		  background-image: linear-gradient(to bottom, {{tileTop}}, {{tileBottom}});
		  box-shadow: inset 0 1px 0 {{tileHighlight}}, inset 1px 0 0 {{tileHighlight}}, 0 1px 1px alpha(black, .1);
		  margin: 1px; transition: none;
		}
		.pinta-modern .editor-tile:hover, .pinta-modern .toolbar .editor-tile:hover,
		.pinta-modern .workspace-toggles .editor-tile:hover, .pinta-modern .tool-box-button.editor-tile:hover {
		  border-color: {{Accent}}; background-color: {{Selection}};
		  background-image: linear-gradient(to bottom, mix({{Surface}}, {{Selection}}, .6), {{Selection}});
		}
		.pinta-modern .editor-tile:checked, .pinta-modern .editor-tile:active,
		.pinta-modern .toolbar .editor-tile:checked, .pinta-modern .toolbar .editor-tile:active,
		.pinta-modern .workspace-toggles .editor-tile:checked, .pinta-modern .tool-box-button.editor-tile:checked {
		  border-color: {{Accent}}; background-image: none; background-color: {{Selection}};
		  box-shadow: inset 1px 1px 2px alpha(black, .24), inset -1px -1px 0 {{tileHighlight}};
		}
		.pinta-modern .editor-tile:disabled { opacity: .45; box-shadow: none; }
		.pinta-modern .editor-tile:focus-visible { outline: 1px dotted {{Foreground}}; outline-offset: -4px; }


		.pinta-modern .canvas-paper { outline: 1px solid mix({{Border}}, black, .25); outline-offset: 0; box-shadow: 0 1px 3px alpha(black, .18); }
		/* Classic Windows desktop presentation. All dimensions are GTK logical pixels. */
		window.pinta-modern, window.pinta-modern.csd { border-radius: 0; }
		.pinta-modern .windows-titlebar {
		  background-image: linear-gradient(to bottom, {{titleTop}}, {{paletteHeader}});
		  color: {{headerText}}; min-height: 30px; padding: 0;
		  border-bottom: 1px solid mix({{Accent}}, black, .22);
		}
		.pinta-modern .windows-window-title { color: {{headerText}}; font-size: 12px; font-weight: normal; }
		.pinta-modern .windows-caption-button {
		  min-width: 43px; min-height: 29px; padding: 0; margin: 0;
		  border: 0; border-radius: 0; background: transparent; box-shadow: none;
		}
		.pinta-modern .windows-caption-button:hover { background-color: alpha(white, .18); }
		.pinta-modern .windows-caption-button:active { background-color: alpha(black, .16); }
		.pinta-modern .windows-close { background-color: #c94040; }
		.pinta-modern .windows-close:hover { background-color: #e81123; }
		.pinta-modern .windows-close:active { background-color: #bd1420; }
		.pinta-modern .editor-menubar {
		  background-color: {{Background}}; min-height: 23px; padding: 0 2px;
		  border-bottom: 1px solid alpha({{Border}}, .24);
		}
		.pinta-modern .editor-menubar > item { padding: 2px 7px; color: {{Foreground}}; border: 1px solid transparent; }
		.pinta-modern .editor-menubar > item:hover, .pinta-modern .editor-menubar > item:active {
		  border-color: alpha({{Accent}}, .6); background-color: {{Selection}};
		}
		.pinta-modern .command-deck {
		  padding: 0 4px; min-height: 62px;
		  background-image: linear-gradient(to bottom, {{Surface}}, {{Background}});
		  border-bottom: 1px solid {{Border}};
		}
		.pinta-modern #main_toolbar { padding: 1px 0; min-height: 28px; }
		.pinta-modern #tool_toolbar { background-image: linear-gradient(to bottom, {{Surface}}, {{Background}}); min-height: 28px; }
		.pinta-modern .workspace-toggles { padding: 0; }
		.pinta-modern button, .pinta-modern dropdown > button, .pinta-modern combobox > box > button {
		  background-image: linear-gradient(to bottom, {{Surface}}, mix({{Background}}, {{Border}}, .12));
		  border: 1px solid {{Border}}; color: {{Foreground}}; border-radius: 0;
		  box-shadow: inset 0 1px 0 {{tileHighlight}}; min-height: 20px;
		}
		.pinta-modern button:hover { border-color: {{Accent}}; background-color: {{Selection}}; }
		.pinta-modern button:active, .pinta-modern button:checked { background-image: none; background-color: {{Selection}}; }
		.pinta-modern entry, .pinta-modern spinbutton {
		  background-color: {{Surface}}; color: {{Foreground}}; border: 1px solid {{Border}};
		  border-radius: 0; min-height: 20px; padding: 0 3px; box-shadow: inset 0 1px 1px alpha(black, .06);
		}
		.pinta-modern entry:focus-within, .pinta-modern spinbutton:focus-within { border-color: {{Accent}}; outline: none; }
		.pinta-modern spinbutton entry { border: 0; box-shadow: none; min-width: 24px; padding: 0 3px; }
		.pinta-modern spinbutton button { min-width: 16px; min-height: 10px; padding: 0 2px; border-width: 0 0 0 1px; }
		.pinta-modern dropdown arrow, .pinta-modern combobox arrow { min-width: 11px; min-height: 11px; }
		.pinta-modern .editor-toolbar .editor-tile, .pinta-modern .workspace-toggles .editor-tile,
		.pinta-modern .tool-box-button.editor-tile {
		  background-image: linear-gradient(to bottom, alpha({{Surface}}, .6), alpha({{Background}}, .6));
		  background-color: transparent; border: 1px solid alpha({{Border}}, .22);
		  box-shadow: none; margin: 0; padding: 2px 3px;
		}
		.pinta-modern .editor-toolbar .editor-tile:hover, .pinta-modern .workspace-toggles .editor-tile:hover,
		.pinta-modern .tool-box-button.editor-tile:hover {
		  border-color: {{Accent}}; background-image: linear-gradient(to bottom, mix({{Surface}}, {{Selection}}, .5), {{Selection}});
		}
		.pinta-modern .editor-toolbar .editor-tile:checked, .pinta-modern .editor-toolbar .editor-tile:active,
		.pinta-modern .workspace-toggles .editor-tile:checked, .pinta-modern .tool-box-button.editor-tile:checked {
		  background-color: {{Selection}}; border-color: {{Accent}};
		  background-image: linear-gradient(to bottom, mix({{Surface}}, {{Selection}}, .2), mix({{Selection}}, {{Accent}}, .12));
		  box-shadow: inset 0 0 0 1px alpha({{Surface}}, .65);
		}
		.pinta-modern .tool-box-button.editor-tile { -gtk-icon-size: 22px; min-width: 24px; min-height: 24px; padding: 1px; }
		.pinta-modern .floating-palette {
		  background-image: linear-gradient(to bottom, {{Surface}}, {{Background}});
		  border: 1px solid {{Border}}; box-shadow: 1px 2px 4px alpha(black, .3);
		}
		.pinta-modern .palette-titlebar {
		  background-image: linear-gradient(to bottom, {{titleTop}}, {{paletteHeader}});
		  color: {{headerText}}; min-height: 23px; padding: 0 2px;
		  border-bottom: 1px solid mix({{Accent}}, black, .2);
		}
		.pinta-modern .palette-titlebar button {
		  background: transparent; color: {{headerText}}; border: 0; padding: 0;
		  min-width: 18px; min-height: 19px; margin: 1px 0; box-shadow: none;
		}
		.pinta-modern .palette-titlebar button:hover { background-color: #e81123; }
		.pinta-modern .palette-toolbar { background-image: linear-gradient(to bottom, {{Surface}}, {{Background}}); padding: 2px; }
		.pinta-modern .document-preview-button {
		  border-radius: 0; border: 1px solid {{Border}}; padding: 2px;
		  background-image: linear-gradient(to bottom, {{Surface}}, {{Background}});
		}
		.pinta-modern .document-preview-button:checked { border-color: {{Accent}}; background-image: none; background-color: {{Selection}}; }
		.pinta-modern .document-close, .pinta-modern .color-swap, .pinta-modern .palette-swatch { background-image: none; }
		.pinta-modern listview, .pinta-modern list { background-color: {{Surface}}; color: {{Foreground}}; }
		.pinta-modern listview row:selected { background-image: linear-gradient(to bottom, mix({{Surface}}, {{Selection}}, .3), {{Selection}}); }
		.pinta-modern checkbutton check { min-width: 12px; min-height: 12px; border: 1px solid {{Border}}; background-color: {{Surface}}; box-shadow: none; }
		.pinta-modern checkbutton check:checked { background-color: {{Accent}}; color: white; border-color: {{Accent}}; }
		.pinta-modern #statusbar { background-image: linear-gradient(to bottom, {{Surface}}, {{Background}}); min-height: 25px; }
		.pinta-modern #statusbar > separator { margin: 2px 5px; }
		.pinta-modern .status-zoom-slider trough { min-height: 3px; border: 1px solid {{Border}}; background-color: {{Surface}}; }
		.pinta-modern .status-zoom-slider highlight { background-color: {{Accent}}; border-radius: 0; }
		.pinta-modern popover.menu modelbutton { padding: 2px 8px; min-height: 21px; border-radius: 0; }
		.pinta-modern popover.menu modelbutton:hover { background-color: {{Selection}}; color: {{Foreground}}; }
		.pinta-modern popover.menu separator { margin: 2px 4px; min-height: 1px; }
		/* Caption buttons keep their solid blue/red Windows skin above general button rules. */
		.pinta-modern .windows-titlebar .windows-caption-button { color: {{headerText}}; background-image: none; background-color: transparent; border: 0; box-shadow: none; }
		.pinta-modern .windows-titlebar .windows-caption-button:hover { background-color: alpha(white, .18); }
		.pinta-modern .windows-titlebar .windows-close { color: white; background-color: #c94040; }
		.pinta-modern .windows-titlebar .windows-close:hover { background-color: #e81123; }


		""";
	}
	private static double Brightness (string color) =>
		.2126 * Convert.ToInt32 (color.Substring (1, 2), 16) +
		.7152 * Convert.ToInt32 (color.Substring (3, 2), 16) +
		.0722 * Convert.ToInt32 (color.Substring (5, 2), 16);

}
