using System;
using Cairo;
using Pinta.Core;
using Pinta.Docking;
using Pinta.Gui.Widgets;

namespace Pinta;

/// <summary>Persistent color wheel, swatches, value and alpha controls.</summary>
internal sealed class ColorsPad
{
	private readonly IPaletteService palette;
	private readonly Gtk.DrawingArea wheel = Gtk.DrawingArea.New ();
	private readonly Gtk.Entry hex = Gtk.Entry.New ();
	private readonly Gtk.Scale value = Gtk.Scale.NewWithRange (Gtk.Orientation.Horizontal, 0, 1, .01);
	private readonly Gtk.Scale alpha = Gtk.Scale.NewWithRange (Gtk.Orientation.Horizontal, 0, 1, .01);
	private readonly PintaColorButton primary = PintaColorButton.New ();
	private readonly PintaColorButton secondary = PintaColorButton.New ();
	private bool primary_selected = true;
	private bool updating;
	private readonly ImageSurface wheel_surface;
	public DockItem Widget { get; }
	private Color Active => primary_selected ? palette.PrimaryColor : palette.SecondaryColor;

	public ColorsPad (IPaletteService palette)
	{
		this.palette = palette;
		const int side = 156;
		wheel_surface = CairoExtensions.CreateImageSurface (Format.Argb32, side, side);
		var pixels = wheel_surface.GetPixelData ();
		for (int y = 0; y < side; y++) for (int x = 0; x < side; x++) {
			double dx = x + .5 - side / 2.0, dy = side / 2.0 - y - .5;
			double r = Math.Sqrt (dx * dx + dy * dy) / (side / 2.0 - 2);
			double hue = (Math.Atan2 (dy, dx) * 180 / Math.PI + 360) % 360;
			pixels[y * side + x] = r > 1 ? ColorBgra.Zero : Color.FromHsv (hue, r, 1).ToColorBgra ();
		}
		wheel_surface.MarkDirty ();
		wheel.WidthRequest = side; wheel.HeightRequest = side;
		wheel.Halign = Gtk.Align.Center;
		wheel.TooltipText = Translations.GetString ("Left: primary color • Right: secondary color • Ctrl: hue • Alt: saturation • Shift: snap hue");
		wheel.SetDrawFunc ((_, cr, w, h) => {
			cr.SetSourceSurface (wheel_surface, 0, 0); cr.Paint ();
			var hsv = Active.ToHsv ();
			double angle = hsv.Hue * Math.PI / 180;
			cr.Arc (side / 2.0 + Math.Cos (angle) * hsv.Sat * 76, side / 2.0 - Math.Sin (angle) * hsv.Sat * 76, 4, 0, Math.PI * 2);
			cr.SetSourceRgb (0, 0, 0); cr.LineWidth = 2; cr.Stroke ();
		});
		var drag = Gtk.GestureDrag.New (); drag.SetButton (0);
		double startX = 0, startY = 0;
		drag.OnDragBegin += (_, e) => { startX = e.StartX; startY = e.StartY; Pick (startX, startY, drag); };
		drag.OnDragUpdate += (_, e) => Pick (startX + e.OffsetX, startY + e.OffsetY, drag);
		wheel.AddController (drag);

		var content = Gtk.Box.New (Gtk.Orientation.Vertical, 4);
		content.MarginStart = content.MarginEnd = 8;
		content.MarginTop = content.MarginBottom = 6;
		var swatches = Gtk.Box.New (Gtk.Orientation.Horizontal, 4);
		primary.WidthRequest = secondary.WidthRequest = 46;
		primary.TooltipText = Translations.GetString ("Primary Color");
		secondary.TooltipText = Translations.GetString ("Secondary Color");
		primary.OnClicked += (_, _) => { primary_selected = true; Refresh (); };
		secondary.OnClicked += (_, _) => { primary_selected = false; Refresh (); };
		var swap = Gtk.Button.NewFromIconName (Resources.StandardIcons.EditUndo);
		swap.TooltipText = Translations.GetString ("Swap Colors (X)");
		swap.OnClicked += (_, _) => SwapColors ();
		swatches.AppendMultiple ([primary, secondary, swap]);
		content.Append (swatches); content.Append (wheel);
		value.SetValue (1); alpha.SetValue (1);
		value.DrawValue = alpha.DrawValue = false;
		value.TooltipText = Translations.GetString ("Brightness");
		alpha.TooltipText = Translations.GetString ("Opacity");
		value.OnValueChanged += (_, _) => { if (!updating) SetActive (Active.CopyHsv (value: value.GetValue ())); };
		alpha.OnValueChanged += (_, _) => { if (!updating) { var c = Active; SetActive (new Color (c.R, c.G, c.B, alpha.GetValue ())); } };
		content.Append (value); content.Append (alpha);
		hex.TooltipText = Translations.GetString ("Hex Color (RRGGBB or RRGGBBAA)");
		hex.OnActivate += (_, _) => { if (Color.FromHex (hex.GetText ().TrimStart ('#')) is Color c) SetActive (c); };
		content.Append (hex);
		var swatch_grid = Gtk.Grid.New ();
		int i = 0;
		foreach (Color c in palette.CurrentPalette.Colors) {
			var swatch = PintaColorButton.New (); swatch.DisplayColor = c;
			swatch.WidthRequest = 16; swatch.HeightRequest = 16;
			swatch.AddCssClass ("palette-swatch");
			swatch.OnClicked += (_, _) => SetActive (c);
			swatch_grid.Attach (swatch, i % 12, i / 12, 1, 1); i++;
		}
		content.Append (swatch_grid);
		Widget = DockItem.New (content, "Colors", Resources.Icons.ToolColorPicker);
		Widget.Label = Translations.GetString ("Colors");
		Widget.AddCssClass ("floating-palette");
		Widget.Halign = Gtk.Align.Start; Widget.Valign = Gtk.Align.End;
		Widget.MarginStart = Widget.MarginBottom = 8;
		palette.PrimaryColorChanged += (_, _) => Refresh ();
		palette.SecondaryColorChanged += (_, _) => Refresh ();
		Refresh ();
	}

	public void SwitchActiveColor () { primary_selected = !primary_selected; Refresh (); }

	private void SwapColors ()
	{
		var c = palette.PrimaryColor;
		palette.SetColor (true, palette.SecondaryColor, false);
		palette.SetColor (false, c, false);
	}

	private void SetActive (Color color) => palette.SetColor (primary_selected, color);

	private void Pick (double x, double y, Gtk.GestureDrag drag)
	{
		primary_selected = drag.GetCurrentButton () != 3;
		double dx = x - 78, dy = 78 - y;
		var current = Active.ToHsv ();
		var state = drag.GetCurrentEventState ();
		double hue = (Math.Atan2 (dy, dx) * 180 / Math.PI + 360) % 360;
		double saturation = Math.Clamp (Math.Sqrt (dx * dx + dy * dy) / 76, 0, 1);
		if (state.HasFlag (Gdk.ModifierType.ControlMask)) saturation = current.Sat;
		if (state.HasFlag (Gdk.ModifierType.AltMask)) hue = current.Hue;
		if (state.HasFlag (Gdk.ModifierType.ShiftMask)) hue = Math.Round (hue / 15) * 15 % 360;
		SetActive (Color.FromHsv (hue, saturation, current.Val == 0 ? 1 : current.Val, Active.A));
	}

	private void Refresh ()
	{
		updating = true;
		primary.DisplayColor = palette.PrimaryColor; secondary.DisplayColor = palette.SecondaryColor;
		primary.SetCssClasses (primary_selected ? ["suggested-action"] : []);
		secondary.SetCssClasses (primary_selected ? [] : ["suggested-action"]);
		value.SetValue (Active.ToHsv ().Val); alpha.SetValue (Active.A);
		hex.SetText (Active.ToHex ());
		wheel.QueueDraw ();
		updating = false;
	}
}
