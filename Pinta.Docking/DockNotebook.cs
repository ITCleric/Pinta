//
// Author:
//       Cameron White <cameronwhite91@gmail.com>
//
// Copyright (c) 2020 Cameron White
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Pinta.Core;

namespace Pinta.Docking;

public sealed class TabClosedEventArgs (IDockNotebookItem item) : CancelEventArgs
{
	public IDockNotebookItem Item { get; } = item;
}

public sealed class TabEventArgs (IDockNotebookItem? item) : EventArgs
{
	public IDockNotebookItem? Item { get; } = item;
}

[GObject.Subclass<Gtk.Box>]
public sealed partial class DockNotebook
{
	private Adw.TabView tab_view;
	private Gtk.ScrolledWindow tab_strip;
	private Gtk.Box thumbnails;
	private readonly List<IDockNotebookItem> items = [];
	private readonly Dictionary<IDockNotebookItem, TabPresentation> presentations = [];
	private readonly HashSet<IDockNotebookItem> closing = [];
	private readonly HashSet<IDockNotebookItem> programmatic_removal = [];

	private sealed record TabPresentation (Gtk.Overlay Root, Gtk.ToggleButton Button, EventHandler LabelHandler, EventHandler PreviewHandler);

	[MemberNotNull (nameof (tab_view), nameof (tab_strip), nameof (thumbnails))]
	partial void Initialize ()
	{
		tab_view = Adw.TabView.New ();
		tab_view.Vexpand = true;
		tab_view.OnClosePage += TabView_OnClosePage;
		thumbnails = Gtk.Box.New (Gtk.Orientation.Horizontal, 3);
		thumbnails.AddCssClass ("document-thumbnails");
		tab_strip = Gtk.ScrolledWindow.New ();
		tab_strip.HscrollbarPolicy = Gtk.PolicyType.Automatic;
		tab_strip.VscrollbarPolicy = Gtk.PolicyType.Never;
		tab_strip.HeightRequest = 72;
		tab_strip.Hexpand = true;
		tab_strip.Child = thumbnails;
		SetOrientation (Gtk.Orientation.Vertical);
		Append (tab_strip);
		Append (tab_view);
		Adw.TabView.SelectedPagePropertyDefinition.Notify (tab_view, TabView_TabChanged);
	}

	public static DockNotebook New () => NewWithProperties ([]);

	public void MoveTabStripTo (Gtk.Box host)
	{
		Remove (tab_strip);
		host.Append (tab_strip);
	}

	private void TabView_TabChanged (GObject.Object _, NotifySignalArgs __)
	{
		foreach (var (item, presentation) in presentations)
			presentation.Button.Active = item.Widget == tab_view.SelectedPage?.Child;
		ActiveTabChanged?.Invoke (this, new TabEventArgs (ActiveItem));
	}

	private bool TabView_OnClosePage (Adw.TabView _, Adw.TabView.ClosePageSignalArgs args)
	{
		var item = FindItemForPage (args.Page);
		if (item is null) { tab_view.ClosePageFinish (args.Page, true); return Gdk.Constants.EVENT_STOP; }
		if (programmatic_removal.Remove (item)) {
			tab_view.ClosePageFinish (args.Page, true);
			RemovePresentation (item);
			return Gdk.Constants.EVENT_STOP;
		}
		if (!closing.Add (item)) return Gdk.Constants.EVENT_STOP;
		var close = new TabClosedEventArgs (item);
		TabClosed?.Invoke (this, close);
		tab_view.ClosePageFinish (args.Page, !close.Cancel);
		if (!close.Cancel) RemovePresentation (item);
		closing.Remove (item);
		return Gdk.Constants.EVENT_STOP;
	}

	public event EventHandler<TabClosedEventArgs>? TabClosed;
	public event EventHandler<TabEventArgs>? ActiveTabChanged;
	public IEnumerable<IDockNotebookItem> Items => items;

	public bool EnableTabs { get => tab_strip.Visible; set => tab_strip.Visible = value; }
	public IDockNotebookItem? ActiveItem {
		get => FindItemForPage (tab_view.SelectedPage);
		set { if (value is not null) tab_view.SelectedPage = tab_view.GetPage (value.Widget); }
	}
	public int ActiveItemIndex => tab_view.SelectedPage is { } page ? tab_view.GetPagePosition (page) : -1;

	public void InsertTab (IDockNotebookItem item, int position)
	{
		position = Math.Clamp (position, 0, items.Count);
		items.Insert (position, item);
		var page = tab_view.Insert (item.Widget, position);
		page.Title = item.Label;
		var root = Gtk.Overlay.New ();
		root.AddCssClass ("document-thumbnail");
		root.Valign = Gtk.Align.Center;
		var button = Gtk.ToggleButton.New ();
		button.FocusOnClick = false;
		button.AddCssClass ("document-preview-button");
		var image = Gtk.DrawingArea.New ();
		image.WidthRequest = 82; image.HeightRequest = 56;
		image.SetDrawFunc ((_, context, width, height) => {
			if (item is IDockNotebookPreview preview) preview.DrawPreview (context, width, height);
		});
		button.SetChild (image);
		button.TooltipText = item.Label;
		button.OnClicked += (_, _) => {
			tab_view.SelectedPage = page;
			// Clicking the already selected toggle must leave its document visibly selected.
			button.Active = true;
		};
		root.Child = button;
		var dirty = Gtk.Label.New ("•");
		dirty.AddCssClass ("document-dirty");
		dirty.Halign = Gtk.Align.Start; dirty.Valign = Gtk.Align.Start;
		dirty.CanTarget = false;
		dirty.Visible = item.Label.EndsWith ('*');
		root.AddOverlay (dirty);
		var close = Gtk.Button.NewFromIconName (Resources.StandardIcons.WindowClose);
		close.AddCssClass ("document-close");
		close.FocusOnClick = false;
		close.Halign = Gtk.Align.End; close.Valign = Gtk.Align.Start;
		close.TooltipText = Translations.GetString ("Close Image");
		close.OnClicked += (_, _) => tab_view.ClosePage (page);
		root.AddOverlay (close);
		EventHandler labelHandler = (_, _) => { page.Title = item.Label; button.TooltipText = item.Label; dirty.Visible = item.Label.EndsWith ('*'); };
		EventHandler previewHandler = (_, _) => image.QueueDraw ();
		item.LabelChanged += labelHandler;
		if (item is IDockNotebookPreview previewItem) previewItem.PreviewChanged += previewHandler;
		presentations[item] = new (root, button, labelHandler, previewHandler);
		Gtk.Widget? previous = position > 0 ? presentations[items[position - 1]].Root : null;
		thumbnails.InsertChildAfter (root, previous);
		button.Active = tab_view.SelectedPage == page;
	}

	public void RemoveTab (IDockNotebookItem item)
	{
		// The document-close handler can be called during the native close callback.
		if (closing.Contains (item) || !presentations.ContainsKey (item)) return;
		programmatic_removal.Add (item);
		tab_view.ClosePage (tab_view.GetPage (item.Widget));
	}

	private void RemovePresentation (IDockNotebookItem item)
	{
		if (!presentations.Remove (item, out var presentation)) return;
		item.LabelChanged -= presentation.LabelHandler;
		if (item is IDockNotebookPreview preview) preview.PreviewChanged -= presentation.PreviewHandler;
		thumbnails.Remove (presentation.Root);
		items.Remove (item);
	}

	private IDockNotebookItem? FindItemForPage (Adw.TabPage? page) => items.FirstOrDefault (i => i.Widget == page?.Child);
}
