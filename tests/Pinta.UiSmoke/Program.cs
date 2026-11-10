// Widget-level integration checks. No windows are presented and no desktop input is used.
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Pinta.Core;
using Pinta.Docking;

GLib.Module.Initialize ();
Gio.Module.Initialize ();
Gtk.Module.Initialize ();
Adw.Module.Initialize ();
Gtk.Functions.Init ();
Adw.Functions.Init ();

void Check (bool condition, string message)
{
	if (!condition) throw new Exception (message);
	Console.WriteLine ($"PASS {message}");
}

foreach (var theme in new[] { ThemePalette.Light, ThemePalette.Dark, ThemePalette.Light with { Accent = "#bd3ee8" } }) {
	using var css = Gtk.CssProvider.New ();
	var errors = new List<string> ();
	css.OnParsingError += (_, e) => errors.Add ($"{e.Error}");
	css.LoadFromBytes (GLib.Bytes.New (Encoding.UTF8.GetBytes (theme.ToCss ())));
	Check (errors.Count == 0, $"Theme CSS parses: {string.Join ("; ", errors)}");
}

var iconTheme = Gtk.IconTheme.GetForDisplay (Gdk.Display.GetDefault ()!);
iconTheme.AddSearchPath (Path.Combine (AppContext.BaseDirectory, "icons"));
var toolIcons = typeof (Pinta.Resources.Icons).GetFields ()
	.Where (field => field.Name.StartsWith ("Tool", StringComparison.Ordinal))
	.Select (field => field.GetRawConstantValue () as string)
	.Where (name => name is not null && name.StartsWith ("pinta-tool-", StringComparison.Ordinal))
	.ToArray ();
Check (toolIcons.Length == 22 && toolIcons.All (name => iconTheme.HasIcon (name!)), "All 22 colored tool icons resolve in the packaged icon theme");
var toolbar = GtkExtensions.CreateToolBar ();
Check (toolbar.HasCssClass ("editor-toolbar") && toolbar.Spacing == 2, "Toolbar keeps separate, explicitly styled tiles");

var notebook = DockNotebook.New ();
var first = new MockItem ("One"); var second = new MockItem ("Two"); var third = new MockItem ("Three");
notebook.InsertTab (first, 0); notebook.InsertTab (second, 1); notebook.InsertTab (third, 2);
notebook.ActiveItem = second;
Check (notebook.ActiveItemIndex == 1 && notebook.ActiveItem == second, "Thumbnail selection selects its document");
var host = Gtk.Box.New (Gtk.Orientation.Horizontal, 0);
notebook.MoveTabStripTo (host);
Check (host.GetFirstChild () is Gtk.ScrolledWindow, "Thumbnail strip moves into the command deck");
notebook.EnableTabs = false;
Check (!host.GetFirstChild ()!.Visible, "Image Tabs toggle hides the external strip");
notebook.EnableTabs = true;
var view = (Adw.TabView) notebook.GetLastChild ()!;
EventHandler<TabClosedEventArgs> cancel = (_, e) => e.Cancel = true;
notebook.TabClosed += cancel;
view.ClosePage (view.GetPage (second.Widget));
Check (notebook.Items.Count () == 3, "Cancelled close retains document and thumbnail");
notebook.TabClosed -= cancel;
notebook.TabClosed += (_, e) => notebook.RemoveTab (e.Item); // Same reentrancy as the application close handler.
view.ClosePage (view.GetPage (second.Widget));
Check (notebook.Items.Count () == 2 && second.Subscribers == 0, "Reentrant close removes the thumbnail and subscriptions once");
notebook.RemoveTab (first);
Check (notebook.Items.Count () == 1 && first.Subscribers == 0, "Programmatic document close cleans up the strip");

var dock = Dock.New ();
var history = DockItem.New (Gtk.Label.New ("History content"), "History", "");
var layers = DockItem.New (Gtk.Label.New ("Layers content"), "Layers", "");
dock.AddItem (history, DockPlacement.Right); dock.AddItem (layers, DockPlacement.Right);
Check (history.GetParent () is Gtk.Overlay && layers.GetParent () == history.GetParent (), "Right palettes float independently over the canvas");
Check (history.Valign == Gtk.Align.Start && layers.Valign == Gtk.Align.End, "History and Layers anchor to opposite corners");
dock.RightPanel.SetItemVisible ("History", false);
dock.RightPanel.SetAllItemsVisible (false);
dock.RightPanel.SetAllItemsVisible (true);
Check (!history.Visible && layers.Visible, "Global palette toggle preserves individual visibility");
dock.RightPanel.SetItemVisible ("History", true);
Check (history.Visible, "Hidden palette can be restored");
Console.WriteLine ("All widget smoke checks passed; interactive visual review is separate.");

sealed class MockItem (string label) : IDockNotebookItem
{
	public Gtk.Widget Widget { get; } = Gtk.Box.New (Gtk.Orientation.Vertical, 0);
	public string Label => label;
	public int Subscribers { get; private set; }
	private EventHandler? changed;
	public event EventHandler LabelChanged {
		add { changed += value; Subscribers++; }
		remove { changed -= value; Subscribers--; }
	}
}
