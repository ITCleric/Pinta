//
// EffectsActions.cs
//
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
//
// Copyright (c) 2010 Jonathan Pobst
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

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Pinta.Core;

public sealed class EffectsActions
{
	public Dictionary<string, Gio.Menu> Menus { get; } = [];
	public Collection<Command> Actions { get; } = [];
	public Dictionary<Command, string> Categories { get; } = [];
	public Command Browse { get; } = new ("browse-effects", Translations.GetString ("Effects Browser…"), null,
		Resources.Icons.EffectsDefault, shortcuts: ["<Primary><Alt>F"]);
	public Command RepeatLastEffect { get; } = new (
		"repeat-last-effect", Translations.GetString ("Repeat Last Effect"), null,
		Resources.Icons.EffectsDefault, shortcuts: ["<Primary>F"]);
	private bool repeat_registered;

	private readonly ChromeManager chrome;
	public EffectsActions (ChromeManager chrome)
	{
		this.chrome = chrome;
	}

	#region Initialization
	public void AddEffect (string category, Command action)
	{
		var effects_menu = chrome.EffectsMenu;
		if (!repeat_registered) {
			repeat_registered = true;
			var section = Gio.Menu.New ();
			section.AppendItem (RepeatLastEffect.CreateMenuItem ());
			section.AppendItem (Browse.CreateMenuItem ());
			effects_menu.AppendSection (null, section);
			chrome.Application.AddCommand (RepeatLastEffect);
			chrome.Application.AddCommand (Browse);
			RepeatLastEffect.Sensitive = false;
			RepeatLastEffect.Activated += (_, _) => PintaCore.LivePreview.RepeatLastEffect ();
			PintaCore.LivePreview.EffectApplied += (_, _) => RepeatLastEffect.Sensitive = true;
		}

		if (!Menus.ContainsKey (category)) {
			var category_menu = Gio.Menu.New ();
			effects_menu.AppendMenuItemSorted (Gio.MenuItem.NewSubmenu (category, category_menu));
			Menus.Add (category, category_menu);
		}

		Actions.Add (action);
		Categories[action] = category;

		Gio.Menu m = Menus[category];
		m.AppendMenuItemSorted (action.CreateMenuItem ());
	}

	// TODO: Remove menu category if empty
	internal void RemoveEffect (string category, Command action)
	{
		if (!Menus.ContainsKey (category))
			return;

		var menu = Menus[category];
		menu.Remove (action);
		Actions.Remove (action);
		Categories.Remove (action);
	}
	#endregion

	#region Public Methods
	public void ToggleActionsSensitive (bool sensitive)
	{
		RepeatLastEffect.Sensitive = sensitive && PintaCore.LivePreview.LastEffect is not null;
		foreach (Command a in Actions)
			a.Sensitive = sensitive;
	}
	#endregion
}
