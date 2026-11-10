using System.Collections.Generic;
using System.Linq;
using Pinta.Core;

namespace Pinta.Docking;

/// <summary>Independent overlay palettes, leaving the whole workspace available to the canvas.</summary>
[GObject.Subclass<Gtk.Box>]
public sealed partial class DockPanel
{
	internal sealed class DockPanelItem (DockItem item)
	{
		public DockItem Item { get; } = item;
		public bool IsMinimized { get; set; }
	}

	private readonly List<DockPanelItem> items = [];
	private Gtk.Overlay overlay = null!;
	private bool all_visible = true;

	partial void Initialize () { }
	public static DockPanel New () => NewWithProperties ([]);
	internal void SetOverlay (Gtk.Overlay value) => overlay = value;

	public void AddItem (DockItem item)
	{
		var entry = new DockPanelItem (item);
		items.Add (entry);
		item.WidthRequest = 208;
		item.HeightRequest = item.UniqueName == "History" ? 190 : 210;
		item.EnableFloating (overlay, Gtk.Align.End,
			item.UniqueName == "History" ? Gtk.Align.Start : Gtk.Align.End, 8, 8);
		item.MinimizeClicked += (_, _) => { entry.IsMinimized = true; item.Visible = false; };
		item.MaximizeClicked += (_, _) => { entry.IsMinimized = false; item.Visible = all_visible; };
		overlay.AddOverlay (item);
	}

	public void SetItemVisible (string name, bool visible)
	{
		var entry = items.FirstOrDefault (x => x.Item.UniqueName == name);
		if (entry is null) return;
		entry.IsMinimized = !visible;
		if (visible) entry.Item.Maximize ();
		entry.Item.Visible = visible && all_visible;
	}

	public void SetAllItemsVisible (bool visible)
	{
		all_visible = visible;
		foreach (var entry in items) entry.Item.Visible = visible && !entry.IsMinimized;
	}

	public void SaveSettings (ISettingsService settings)
	{
		foreach (var entry in items) {
			settings.PutSetting (SettingNames.MinimizeKey (entry), entry.IsMinimized);
			entry.Item.SaveFloatingPosition (settings);
		}
	}

	public void LoadSettings (ISettingsService settings)
	{
		foreach (var entry in items) {
			entry.Item.LoadFloatingPosition (settings);
			SetItemVisible (entry.Item.UniqueName, !settings.GetSetting (SettingNames.MinimizeKey (entry), false));
		}
	}
}
