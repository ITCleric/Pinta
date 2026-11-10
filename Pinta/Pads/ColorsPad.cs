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
	private Gtk.ComboBoxText active_selector = null!;
	private ImageSurface? wheel_surface;
	private int wheel_scale;
	public DockItem Widget { get; }
	private Color Active => primary_selected ? palette.PrimaryColor : palette.SecondaryColor;

	public ColorsPad (IPaletteService palette)
	{
		this.palette = palette;
		const int side = 144;
		wheel.WidthRequest = side; wheel.HeightRequest = side;
		wheel.Halign = Gtk.Align.Center;
		wheel.TooltipText = Translations.GetString ("Left: primary color • Right: secondary color • Ctrl: hue • Alt: saturation • Shift: snap hue");
		wheel.SetDrawFunc ((_, cr, w, h) => {
			EnsureWheelSurface (wheel.GetScaleFactor ());
			cr.Save (); cr.Scale (1.0 / wheel_scale, 1.0 / wheel_scale);
			cr.SetSourceSurface (wheel_surface!, 0, 0); cr.Paint (); cr.Restore ();
			var hsv = Active.ToHsv ();
			double angle = hsv.Hue * Math.PI / 180;
			cr.Arc (side / 2.0 + Math.Cos (angle) * hsv.Sat * 70, side / 2.0 + Math.Sin (angle) * hsv.Sat * 70, 4, 0, Math.PI * 2);
			cr.SetSourceRgb (0, 0, 0); cr.LineWidth = 2; cr.Stroke ();
		});
		var drag = Gtk.GestureDrag.New (); drag.SetButton (0);
		double startX = 0, startY = 0;
		drag.OnDragBegin += (_, e) => { startX = e.StartX; startY = e.StartY; Pick (startX, startY, drag); };
		drag.OnDragUpdate += (_, e) => Pick (startX + e.OffsetX, startY + e.OffsetY, drag);
		wheel.AddController (drag);

		var content = Gtk.Box.New (Gtk.Orientation.Vertical, 3);
		content.MarginStart = content.MarginEnd = 6;
		content.MarginTop = content.MarginBottom = 5;
		var controls = Gtk.Box.New (Gtk.Orientation.Horizontal, 4);
		active_selector = Gtk.ComboBoxText.New ();
		active_selector.AppendText (Translations.GetString ("Primary"));
		active_selector.AppendText (Translations.GetString ("Secondary"));
		active_selector.Active = 0;
		active_selector.Hexpand = true;
		active_selector.OnChanged += (_, _) => { if (!updating) { primary_selected = active_selector.Active == 0; Refresh (); } };
		var more = Gtk.ToggleButton.NewWithLabel (Translations.GetString ("More »"));
		more.FocusOnClick = false;
		controls.AppendMultiple ([active_selector, more]);
		content.Append (controls);
		var wheel_row = Gtk.Box.New (Gtk.Orientation.Horizontal, 4);
		var swatches = Gtk.Fixed.New ();
		swatches.WidthRequest = 52; swatches.HeightRequest = 56;
		swatches.Valign = Gtk.Align.Start;
		primary.WidthRequest = primary.HeightRequest = 30;
		secondary.WidthRequest = secondary.HeightRequest = 30;
		primary.AddCssClass ("active-color-swatch"); secondary.AddCssClass ("active-color-swatch");
		primary.TooltipText = Translations.GetString ("Primary Color");
		secondary.TooltipText = Translations.GetString ("Secondary Color");
		primary.OnClicked += (_, _) => { primary_selected = true; Refresh (); };
		secondary.OnClicked += (_, _) => { primary_selected = false; Refresh (); };
		var swap = Gtk.Button.NewFromIconName (Resources.StandardIcons.EditUndo);
		swap.AddCssClass ("color-swap");
		swap.TooltipText = Translations.GetString ("Swap Colors (X)");
		swap.OnClicked += (_, _) => SwapColors ();
		swatches.Put (secondary, 16, 18); swatches.Put (primary, 0, 3); swatches.Put (swap, 31, 0);
		wheel_row.AppendMultiple ([swatches, wheel]);
		content.Append (wheel_row);
		var details = Gtk.Box.New (Gtk.Orientation.Vertical, 2);
		details.Visible = false;
		more.OnToggled += (_, _) => {
			details.Visible = more.Active;
			more.Label = Translations.GetString (more.Active ? "Less «" : "More »");
		};
		value.SetValue (1); alpha.SetValue (1);
		value.DrawValue = alpha.DrawValue = false;
		value.TooltipText = Translations.GetString ("Brightness");
		alpha.TooltipText = Translations.GetString ("Opacity");
		value.OnValueChanged += (_, _) => { if (!updating) SetActive (Active.CopyHsv (value: value.GetValue ())); };
		alpha.OnValueChanged += (_, _) => { if (!updating) { var c = Active; SetActive (new Color (c.R, c.G, c.B, alpha.GetValue ())); } };
		details.Append (value); details.Append (alpha);
		hex.TooltipText = Translations.GetString ("Hex Color (RRGGBB or RRGGBBAA)");
		hex.OnActivate += (_, _) => { if (Color.FromHex (hex.GetText ().TrimStart ('#')) is Color c) SetActive (c); };
		details.Append (hex);
		content.Append (details);
		var swatch_grid = Gtk.Grid.New ();
		int i = 0;
		foreach (Color c in palette.CurrentPalette.Colors) {
			var swatch = PintaColorButton.New (); swatch.DisplayColor = c;
			swatch.WidthRequest = 8; swatch.HeightRequest = 8;
			swatch.AddCssClass ("palette-swatch");
			swatch.OnClicked += (_, _) => SetActive (c);
			swatch_grid.Attach (swatch, i % 24, i / 24, 1, 1); i++;
		}
		content.Append (swatch_grid);
		Widget = DockItem.New (content, "Colors", Resources.Icons.ToolColorPicker);
		Widget.Label = Translations.GetString ("Colors");
		Widget.AddCssClass ("floating-palette");
		Widget.WidthRequest = 220;
		Widget.Halign = Gtk.Align.Start; Widget.Valign = Gtk.Align.End;
		Widget.MarginStart = Widget.MarginBottom = 8;
		palette.PrimaryColorChanged += (_, _) => Refresh ();
		palette.SecondaryColorChanged += (_, _) => Refresh ();
		Refresh ();
	}

	// Cache at the monitor's actual pixel density; pointer coordinates remain logical pixels.
	private void EnsureWheelSurface (int scale)
	{
		scale = Math.Max (1, scale);
		if (wheel_surface is not null && wheel_scale == scale) return;
		wheel_surface?.Dispose ();
		wheel_scale = scale;
		int size = 144 * scale;
		wheel_surface = CairoExtensions.CreateImageSurface (Format.Argb32, size, size);
		var pixels = wheel_surface.GetPixelData ();
		for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) {
			double dx = (x + .5) / scale - 72, dy = (y + .5) / scale - 72;
			double radius = Math.Sqrt (dx * dx + dy * dy);
			double coverage = Math.Clamp ((70 - radius) * scale + .5, 0, 1);
			double hue = (Math.Atan2 (dy, dx) * 180 / Math.PI + 360) % 360;
			pixels[y * size + x] = Color.FromHsv (hue, Math.Min (1, radius / 70), 1, coverage).ToColorBgra ();
		}
		wheel_surface.MarkDirty ();
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
		double dx = x - 72, dy = y - 72;
		var current = Active.ToHsv ();
		var state = drag.GetCurrentEventState ();
		double hue = (Math.Atan2 (dy, dx) * 180 / Math.PI + 360) % 360;
		double saturation = Math.Clamp (Math.Sqrt (dx * dx + dy * dy) / 70, 0, 1);
		if (state.HasFlag (Gdk.ModifierType.ControlMask)) saturation = current.Sat;
		if (state.HasFlag (Gdk.ModifierType.AltMask)) hue = current.Hue;
		if (state.HasFlag (Gdk.ModifierType.ShiftMask)) hue = Math.Round (hue / 15) * 15 % 360;
		SetActive (Color.FromHsv (hue, saturation, current.Val == 0 ? 1 : current.Val, Active.A));
	}

	private void Refresh ()
	{
		updating = true;
		active_selector.Active = primary_selected ? 0 : 1;
		primary.DisplayColor = palette.PrimaryColor; secondary.DisplayColor = palette.SecondaryColor;
		if (primary_selected) primary.AddCssClass ("selected-color"); else primary.RemoveCssClass ("selected-color");
		if (!primary_selected) secondary.AddCssClass ("selected-color"); else secondary.RemoveCssClass ("selected-color");
		value.SetValue (Active.ToHsv ().Val); alpha.SetValue (Active.A);
		hex.SetText (Active.ToHex ());
		wheel.QueueDraw ();
		updating = false;
	}
}
