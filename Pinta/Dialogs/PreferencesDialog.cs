using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GObject;
using Object = GObject.Object;

namespace Pinta;

using Pinta.Core;
using Pinta.Gui.Widgets;

[GObject.Subclass<Adw.PreferencesDialog> (qualifiedName: nameof (PreferencesDialog))]
[Gtk.Template<Gtk.AssemblyResource> ("PreferencesDialog.ui")]
internal sealed partial class PreferencesDialog
{
	private ISettingsService settings = null!; // NRT - set by factory method
	private IChromeService chrome = null!;
	private IPaletteService palette = null!;
	private List<string> language_codes = [];
	[Gtk.Connect ("theme_group")]
	private Adw.PreferencesGroup theme_group;
	private readonly List<Adw.EntryRow> theme_rows = [];
	private bool loading_theme;
	private ThemeStyle theme_style = ThemeStyle.Windows10;
	private ThemePresetPicker? theme_presets;

	[Gtk.Connect ("language_comborow")]
	private Adw.ComboRow language_row;

	[Gtk.Connect ("color_scheme_comborow")]
	private Adw.ComboRow color_scheme_row;

	[Gtk.Connect ("menubar_switchrow")]
	private Adw.SwitchRow menubar_row;

	[Gtk.Connect ("selection_anim_switchrow")]
	private Adw.SwitchRow selection_anim_row;

	[Gtk.Connect ("startup_width_spinrow")]
	private Adw.SpinRow startup_width_row;

	[Gtk.Connect ("startup_height_spinrow")]
	private Adw.SpinRow startup_height_row;

	[Gtk.Connect ("startup_background_comborow")]
	private Adw.ComboRow startup_background_row;

	[Gtk.Connect ("startup_color_row")]
	private Adw.ActionRow startup_color_row;

	private PintaColorButton startup_color_button = null!;

	public static PreferencesDialog New (IChromeService chrome, IPaletteService palette, ISettingsService settings)
	{
		PreferencesDialog dialog = NewWithProperties ([]);
		dialog.AddCssClass ("pinta-modern");
		dialog.chrome = chrome;
		dialog.palette = palette;
		dialog.LoadSettings (settings);
		dialog.BuildThemeEditor ();
		return dialog;
	}

	partial void Initialize ()
	{
		language_codes.Add (string.Empty); // For 'Default'.

		// Build a map from language label to language code.
		Dictionary<string, string> langMap = Translations.GetAvailableLanguages ()
			.ToDictionary (Translations.GetLanguageDisplayName, lang => lang);

		// Add the available languages to the combobox, ordered by label.
		Gtk.StringList langModel = (Gtk.StringList) language_row.Model;
		foreach (string lang in langMap.Keys.Order ()) {
			langModel.Append (lang);
			language_codes.Add (langMap[lang]);
		}

		Adw.ComboRow.SelectedPropertyDefinition.Notify (language_row, OnLanguageChanged);
		Adw.ComboRow.SelectedPropertyDefinition.Notify (color_scheme_row, OnColorSchemeChanged);
		Adw.SwitchRow.ActivePropertyDefinition.Notify (menubar_row, OnMenuBarChanged);
		Adw.SwitchRow.ActivePropertyDefinition.Notify (selection_anim_row, OnSelectionAnimChanged);
		Adw.SpinRow.ValuePropertyDefinition.Notify (startup_width_row, OnStartupWidthChanged);
		Adw.SpinRow.ValuePropertyDefinition.Notify (startup_height_row, OnStartupHeightChanged);
		Adw.ComboRow.SelectedPropertyDefinition.Notify (startup_background_row, OnStartupBackgroundChanged);

		startup_color_button = PintaColorButton.New ();
		startup_color_button.Valign = Gtk.Align.Center;
		startup_color_button.Hexpand = false;
		startup_color_button.WidthRequest = 80;
		startup_color_button.OnClicked += async (_, _) => await ChooseStartupColor ();
		startup_color_row.AddSuffix (startup_color_button);
		startup_color_row.ActivatableWidget = startup_color_button;
	}

	/// <summary>
	/// Initialize the UI widgets from the existing settings.
	/// </summary>
	private void LoadSettings (ISettingsService settingsService)
	{
		settings = settingsService;

		int langIndex = language_codes.IndexOf (settings.GetSetting (SettingNames.LANGUAGE, SettingDefaults.LANGUAGE));
		if (langIndex >= 0 && langIndex < language_codes.Count)
			language_row.SetSelected ((uint) langIndex);

		int schemeIndex = settings.GetSetting (SettingNames.COLOR_SCHEME, 1);
		color_scheme_row.SetSelected ((uint) schemeIndex);

		bool menuBarShown = settings.GetSetting (SettingNames.MENUBAR_SHOWN, SettingDefaults.MenuBarShown ());
		menubar_row.Active = menuBarShown;

		bool selectionAnimated = settings.GetSetting (
			Pinta.Core.SettingNames.CANVAS_SELECTION_ANIMATED,
			Pinta.Core.SettingDefaults.CANVAS_SELECTION_ANIMATED);
		selection_anim_row.Active = selectionAnimated;

		startup_width_row.Value = settings.GetSetting (SettingNames.STARTUP_IMAGE_WIDTH, 800);
		startup_height_row.Value = settings.GetSetting (SettingNames.STARTUP_IMAGE_HEIGHT, 600);
		startup_background_row.SetSelected ((uint) settings.GetSetting (SettingNames.STARTUP_IMAGE_BACKGROUND, (int) BackgroundType.White));

		string colorHex = settings.GetSetting (SettingNames.STARTUP_IMAGE_BACKGROUND_COLOR, Cairo.Color.Black.ToHex ());
		startup_color_button.DisplayColor = Cairo.Color.FromHex (colorHex) ?? Cairo.Color.Black;
		startup_color_row.Visible = startup_background_row.Selected == (uint) BackgroundType.SecondaryColor;
	}

	private void BuildThemeEditor ()
	{
		theme_presets = new ThemePresetPicker (preset => { SetThemeRows (preset); SaveTheme (); });
		theme_group.Add (theme_presets.Widget);
		string[] labels = ["Background", "Surface", "Foreground", "Accent", "Border", "Canvas", "Selection"];
		foreach (string label in labels) {
			var row = Adw.EntryRow.New ();
			row.Title = Translations.GetString (label);
			theme_group.Add (row);
			theme_rows.Add (row);
			row.OnChanged += (_, _) => SaveTheme ();
		}
		ThemePalette theme;
		try { theme = ThemePalette.FromJson (settings.GetSetting ("custom-theme", ThemePalette.Light.ToJson ())); }
		catch (Exception) { theme = ThemePalette.Light; }
		SetThemeRows (theme);
		var buttons = Gtk.Box.New (Gtk.Orientation.Horizontal, 8);
		foreach (var (label, preset) in new[] { ("Light Preset", ThemePalette.Light), ("Dark Preset", ThemePalette.Dark) }) {
			var button = Gtk.Button.NewWithLabel (Translations.GetString (label));
			button.OnClicked += (_, _) => { SetThemeRows (preset); SaveTheme (); };
			buttons.Append (button);
		}
		var import = Gtk.Button.NewWithLabel (Translations.GetString ("Import…"));
		import.OnClicked += async (_, _) => await TransferTheme (false);
		buttons.Append (import);
		var export = Gtk.Button.NewWithLabel (Translations.GetString ("Export…"));
		export.OnClicked += async (_, _) => await TransferTheme (true);
		buttons.Append (export);
		theme_group.Add (buttons);
	}

	private void SetThemeRows (ThemePalette theme)
	{
		loading_theme = true;
		theme_style = theme.Style;
		theme_presets?.SetSelected (theme);
		string[] values = [theme.Background, theme.Surface, theme.Foreground, theme.Accent, theme.Border, theme.Canvas, theme.Selection];
		for (int i = 0; i < values.Length; i++) theme_rows[i].SetText (values[i]);
		loading_theme = false;
	}

	private ThemePalette ReadTheme () => new (
		theme_rows[0].GetText (), theme_rows[1].GetText (), theme_rows[2].GetText (),
		theme_rows[3].GetText (), theme_rows[4].GetText (), theme_rows[5].GetText (), theme_rows[6].GetText (), theme_style);

	private void SaveTheme ()
	{
		if (loading_theme) return;
		foreach (var row in theme_rows) {
			if (ThemePalette.IsColor (row.GetText ())) row.RemoveCssClass ("error");
			else row.AddCssClass ("error");
		}
		var theme = ReadTheme ();
		if (!theme.IsValid) return;
		theme_presets?.SetSelected (theme);
		settings.PutSetting ("custom-theme", theme.ToJson ());
		color_scheme_row.SetSelected (3);
	}

	private async Task TransferTheme (bool export)
	{
		try {
			using var dialog = Gtk.FileDialog.New ();
			dialog.Title = Translations.GetString (export ? "Export Theme" : "Import Theme");
			Gio.File? file = export ? await dialog.SaveAsync (chrome.MainWindow) : await dialog.OpenAsync (chrome.MainWindow);
			string? path = file?.GetPath ();
			if (path is null) return;
			if (export) {
				var theme = ReadTheme ();
				if (!theme.IsValid) throw new FormatException ("Theme colors must use #RRGGBB.");
				await File.WriteAllTextAsync (path, theme.ToJson ());
			} else {
				var theme = ThemePalette.FromJson (await File.ReadAllTextAsync (path));
				SetThemeRows (theme);
				SaveTheme ();
			}
		} catch (Exception ex) {
			AddToast (Adw.Toast.New (ex.Message));
		}
	}

	private void OnLanguageChanged (Object sender, NotifySignalArgs args)
	{
		int langIndex = (int) language_row.Selected;
		string langCode = language_codes[langIndex];

		// Don't trigger the restart message when the setting is loaded on startup.
		string currentLang = settings.GetSetting (SettingNames.LANGUAGE, SettingDefaults.LANGUAGE);
		if (langCode == currentLang)
			return;

		settings.PutSetting (SettingNames.LANGUAGE, langCode);

		// Changing the language requires restarting Pinta.
		ShowRestartMessage ();
	}

	private void OnColorSchemeChanged (Object sender, NotifySignalArgs args)
	{
		int schemeIndex = (int) color_scheme_row.Selected;
		settings.PutSetting (SettingNames.COLOR_SCHEME, schemeIndex);
	}

	private void OnMenuBarChanged (Object sender, NotifySignalArgs args)
	{
		// Don't trigger the restart message when the setting is loaded on startup.
		if (menubar_row.Active == settings.GetSetting (SettingNames.MENUBAR_SHOWN, SettingDefaults.MenuBarShown ()))
			return;

		settings.PutSetting (SettingNames.MENUBAR_SHOWN, menubar_row.Active);

		// Changing the setting requires a restart since the application window is
		// constructed differently (see WindowShell).
		ShowRestartMessage ();
	}

	private void OnSelectionAnimChanged (Object sender, NotifySignalArgs args)
	{
		settings.PutSetting (Pinta.Core.SettingNames.CANVAS_SELECTION_ANIMATED, selection_anim_row.Active);
	}

	private void OnStartupWidthChanged (Object sender, NotifySignalArgs args)
	{
		settings.PutSetting (SettingNames.STARTUP_IMAGE_WIDTH, (int) startup_width_row.Value);
	}

	private void OnStartupHeightChanged (Object sender, NotifySignalArgs args)
	{
		settings.PutSetting (SettingNames.STARTUP_IMAGE_HEIGHT, (int) startup_height_row.Value);
	}

	private void OnStartupBackgroundChanged (Object sender, NotifySignalArgs args)
	{
		settings.PutSetting (SettingNames.STARTUP_IMAGE_BACKGROUND, (int) startup_background_row.Selected);
		startup_color_row.Visible = startup_background_row.Selected == (uint) BackgroundType.SecondaryColor;
	}

	private async Task ChooseStartupColor ()
	{
		using ColorPickerDialog dialog = ColorPickerDialog.New (
			chrome.MainWindow,
			palette,
			new SingleColor (startup_color_button.DisplayColor),
			primarySelected: true,
			false,
			Translations.GetString ("Choose Color"));

		// The picker needs this to receive input from Preferences on macOS
		dialog.Modal = true;

		try {
			Gtk.ResponseType response = await dialog.RunAsync ();

			if (response != Gtk.ResponseType.Ok)
				return;

			Cairo.Color color = ((SingleColor) dialog.Colors).Color;
			startup_color_button.DisplayColor = color;
			settings.PutSetting (SettingNames.STARTUP_IMAGE_BACKGROUND_COLOR, color.ToHex ());
		} finally {
			dialog.Destroy ();
		}
	}

	private void ShowRestartMessage ()
	{
		Adw.Toast toast = Adw.Toast.New (Translations.GetString ("Please restart Pinta for the changes to take effect."));
		toast.Timeout = 2;

		AddToast (toast);
	}
}
