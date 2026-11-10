using System;
using System.Collections.Generic;
using Cairo;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

/// <summary>Responsive preset gallery with vector previews, rendered sharply at the display scale.</summary>
public sealed class ThemePresetPicker
{
	public Gtk.FlowBox Widget { get; } = Gtk.FlowBox.New ();
	private readonly Dictionary<ThemePreset, Gtk.ToggleButton> buttons = [];
	public ThemePresetPicker (Action<ThemePalette> selected)
	{
		Widget.SelectionMode = Gtk.SelectionMode.None;
		Widget.Homogeneous = true;
		Widget.MinChildrenPerLine = 1; Widget.MaxChildrenPerLine = 3;
		Widget.RowSpacing = Widget.ColumnSpacing = 6;
		Widget.AddCssClass ("theme-preset-gallery");
		var group = Gtk.ToggleButton.New ();
		foreach (var preset in ThemePresets.All) {
			var button = Gtk.ToggleButton.New ();
			button.Group = group; button.Name = preset.Id;
			button.AddCssClass ("theme-preset-tile");
			button.TooltipText = Translations.GetString (preset.Description);
			var content = Gtk.Box.New (Gtk.Orientation.Vertical, 4);
			var preview = Gtk.DrawingArea.New ();
			preview.WidthRequest = 160; preview.HeightRequest = 96;
			preview.SetDrawFunc ((_, cr, width, height) => DrawPreview (cr, preset.Palette, width, height));
			var name = Gtk.Label.New (preset.Name);
			name.Wrap = true; name.MaxWidthChars = 24;
			content.AppendMultiple ([preview, name]);
			button.SetChild (content);
			button.OnClicked += (_, _) => { button.Active = true; selected (preset.Palette); };
			buttons.Add (preset, button);
			Widget.Insert (button, -1);
		}
	}

	public void SetSelected (ThemePalette palette)
	{
		foreach (var (preset, button) in buttons) button.Active = preset.Palette == palette;
	}

	public static void DrawPreview (Context cr, ThemePalette palette, int width, int height)
	{
		var bg = Color.FromHex (palette.Background)!.Value;
		var surface = Color.FromHex (palette.Surface)!.Value;
		var border = Color.FromHex (palette.Border)!.Value;
		var accent = Color.FromHex (palette.Accent)!.Value;
		var selection = Color.FromHex (palette.Selection)!.Value;
		bool dark = bg.R * .2126 + bg.G * .7152 + bg.B * .0722 < .5;
		Color Mix (Color a, Color b, double t) => new (a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t);
		var header = dark ? Mix (bg, accent, .18) : accent;
		void Fill (double x, double y, double w, double h, Color color) { cr.SetSourceColor (color); cr.Rectangle (x, y, w, h); cr.Fill (); }
		void Frame (double x, double y, double w, double h) { cr.SetSourceColor (border); cr.LineWidth = .7; cr.Rectangle (x + .35, y + .35, w - .7, h - .7); cr.Stroke (); }
		void Header (double x, double y, double w, double h)
		{
			if (palette.Style == ThemeStyle.Classic95) { Fill (x, y, w, h, header); return; }
			using var gradient = new LinearGradient (x, y, x, y + h);
			bool glossy = palette.Style is ThemeStyle.Aero or ThemeStyle.Frutiger or ThemeStyle.Office2007;
			gradient.AddColorStop (0, Mix (header, Color.White, glossy ? .5 : .18));
			if (glossy) { gradient.AddColorStop (.45, Mix (header, Color.White, .24)); gradient.AddColorStop (.49, header); }
			gradient.AddColorStop (1, header);
			cr.SetSource (gradient); cr.Rectangle (x, y, w, h); cr.Fill ();
		}
		void Panel (double x, double y, double w, double h) { Fill (x, y, w, h, surface); Frame (x, y, w, h); Header (x + 1, y + 1, w - 2, 5); }
		cr.Save (); cr.Scale (width / 160.0, height / 96.0);
		Fill (0, 0, 160, 96, bg); Header (0, 0, 160, 10);
		Fill (4, 3, 3, 4, Color.White); Fill (10, 4, 36, 1, Color.White);
		Fill (147, 2, 10, 6, palette.Style == ThemeStyle.Classic95 ? bg : new Color (.78, .24, .2));
		for (int i = 0; i < 6; i++) Fill (4 + i * 13, 13, 8, 1, border);
		for (int i = 0; i < 8; i++) { Fill (4 + i * 8, 18, 5, 5, i % 3 == 0 ? selection : accent); Frame (3 + i * 8, 17, 7, 7); }
		Fill (0, 27, 160, 64, Color.FromHex (palette.Canvas)!.Value);
		Fill (44, 34, 72, 50, Color.White); Frame (44, 34, 72, 50);
		Panel (3, 30, 18, 34); Panel (3, 68, 33, 20); Panel (125, 30, 32, 23); Panel (125, 61, 32, 27);
		for (int y = 0; y < 4; y++) for (int x = 0; x < 2; x++) Fill (6 + x * 7, 39 + y * 5, 3, 3, x == 0 ? accent : selection);
		cr.SetSourceColor (accent); cr.Arc (25, 80, 6, 0, Math.PI * 2); cr.Fill ();
		Fill (128, 39, 26, 4, selection); Fill (128, 70, 26, 8, selection);
		Fill (129, 71, 7, 6, Color.White); Fill (127, 84, 3, 2, accent);
		Fill (115, 93, 38, 1, border); Fill (138, 91, 3, 4, accent);
		Frame (0, 0, 160, 96); cr.Restore ();
	}
}
