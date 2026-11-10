using System;
using System.Collections.Generic;
using System.Linq;
using Pinta.Core;

namespace Pinta.Actions;

/// <summary>A searchable list of registered effects, including add-ins.</summary>
internal sealed class EffectBrowserAction (ActionManager actions, ChromeManager chrome) : IActionHandler
{
	public void Initialize () => actions.Effects.Browse.Activated += Activated;
	public void Uninitialize () => actions.Effects.Browse.Activated -= Activated;

	private void Activated (object sender, EventArgs args)
	{
		var dialog = Adw.Dialog.New ();
		dialog.Title = Translations.GetString ("Effects Browser");
		dialog.ContentWidth = 580; dialog.ContentHeight = 620;
		var content = Gtk.Box.New (Gtk.Orientation.Vertical, 8);
		var header = Adw.HeaderBar.New ();
		header.TitleWidget = Gtk.Label.New (dialog.Title);
		content.Append (header);
		var search = Gtk.SearchEntry.New ();
		search.MarginStart = search.MarginEnd = 12;
		search.PlaceholderText = Translations.GetString ("Search effects and adjustments…");
		content.Append (search);
		var list = Gtk.Box.New (Gtk.Orientation.Vertical, 4);
		list.MarginStart = list.MarginEnd = list.MarginBottom = 12;
		List<(Gtk.Widget Row, string Text)> rows = [];
		var entries = actions.Adjustments.Actions.Select (a => (Action: a, Category: Translations.GetString ("Adjustments")))
			.Concat (actions.Effects.Actions.Select (a => (Action: a, Category: actions.Effects.Categories.GetValueOrDefault (a, "Effects"))))
			.OrderBy (entry => entry.Category).ThenBy (entry => entry.Action.Label);
		foreach (var (action, category) in entries) {
			var button = Gtk.Button.New ();
			button.AddCssClass ("flat");
			button.Sensitive = action.Sensitive;
			var box = Gtk.Box.New (Gtk.Orientation.Horizontal, 12);
			var icon = Gtk.Image.NewFromIconName (action.IconName ?? Resources.Icons.EffectsDefault);
			icon.PixelSize = 32;
			box.Append (icon);
			var labels = Gtk.Box.New (Gtk.Orientation.Vertical, 2);
			var title = Gtk.Label.New (action.Label.TrimEnd ('.'));
			title.Halign = Gtk.Align.Start;
			var subtitle = Gtk.Label.New (category);
			subtitle.Halign = Gtk.Align.Start; subtitle.AddCssClass ("dim-label");
			labels.AppendMultiple ([title, subtitle]); box.Append (labels);
			button.SetChild (box);
			button.OnClicked += (_, _) => { dialog.Close (); action.Activate (); };
			list.Append (button);
			rows.Add ((button, action.Label + " " + category));
		}
		var empty = Gtk.Label.New (Translations.GetString ("No matching effects"));
		empty.Visible = false; list.Append (empty);
		search.OnSearchChanged += (_, _) => {
			string query = search.GetText ().Trim ();
			int matches = 0;
			foreach (var (row, text) in rows) {
				row.Visible = text.Contains (query, StringComparison.CurrentCultureIgnoreCase);
				if (row.Visible) matches++;
			}
			empty.Visible = matches == 0;
		};
		var scroll = Gtk.ScrolledWindow.New ();
		scroll.Vexpand = true; scroll.HscrollbarPolicy = Gtk.PolicyType.Never;
		scroll.Child = list; content.Append (scroll);
		dialog.Child = content;
		dialog.Present (chrome.MainWindow);
		search.GrabFocus ();
	}
}
