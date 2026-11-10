using System;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

/// <summary>Compact desktop titlebar. Native GTK window actions retain close/unsaved handling.</summary>
public static class ClassicWindowChrome
{
	public static Gtk.Widget Create (Gtk.Window window)
	{
		var handle = Gtk.WindowHandle.New ();
		handle.AddCssClass ("windows-titlebar");
		var row = Gtk.Box.New (Gtk.Orientation.Horizontal, 0);
		var icon = Gtk.Image.NewFromIconName (Resources.Icons.Pinta);
		icon.PixelSize = 16; icon.MarginStart = 8; icon.MarginEnd = 7;
		row.Append (icon);
		var title = Gtk.Label.New (window.Title);
		title.Hexpand = true; title.Xalign = 0;
		title.Ellipsize = Pango.EllipsizeMode.End;
		title.AddCssClass ("windows-window-title");
		Gtk.Window.TitlePropertyDefinition.Notify (window, (_, _) => title.SetText (window.Title ?? string.Empty));
		row.Append (title);
		var minimize = Button ("windows-minimize", Translations.GetString ("Minimize"), 0);
		minimize.OnClicked += (_, _) => window.Minimize ();
		var maximize = Button ("windows-maximize", Translations.GetString ("Maximize / Restore"), 1, () => window.IsMaximized ());
		maximize.OnClicked += (_, _) => { if (window.IsMaximized ()) window.Unmaximize (); else window.Maximize (); };
		var close = Button ("windows-close", Translations.GetString ("Close"), 2);
		close.OnClicked += (_, _) => window.Close ();
		row.AppendMultiple ([minimize, maximize, close]);
		handle.SetChild (row);
		return handle;
	}

	private static Gtk.Button Button (string cssClass, string label, int glyph, Func<bool>? isMaximized = null)
	{
		var button = Gtk.Button.New ();
		button.AddCssClass ("windows-caption-button"); button.AddCssClass (cssClass);
		button.TooltipText = label; button.FocusOnClick = false;
		var image = Gtk.DrawingArea.New ();
		image.WidthRequest = 12; image.HeightRequest = 12;
		image.SetDrawFunc ((_, cr, width, height) => {
			image.GetStyleContext ().GetColor (out Gdk.RGBA color);
			cr.SetSourceRgba (color.Red, color.Green, color.Blue, color.Alpha); cr.LineWidth = 1;
			double x = (width - 10) * .5 + .5, y = (height - 10) * .5 + .5;
			if (glyph == 0) { cr.MoveTo (x, y + 7); cr.LineTo (x + 10, y + 7); }
			else if (glyph == 1) {
				if (isMaximized?.Invoke () == true) {
					cr.Rectangle (x + 1, y + 3, 6, 6);
					cr.MoveTo (x + 3, y + 2); cr.LineTo (x + 3, y + 1); cr.LineTo (x + 9, y + 1); cr.LineTo (x + 9, y + 7); cr.LineTo (x + 8, y + 7);
				} else cr.Rectangle (x + 1, y + 1, 8, 8);
			}
			else { cr.MoveTo (x + 1, y + 1); cr.LineTo (x + 9, y + 9); cr.MoveTo (x + 9, y + 1); cr.LineTo (x + 1, y + 9); }
			cr.Stroke ();
		});
		button.SetChild (image);
		return button;
	}
}
