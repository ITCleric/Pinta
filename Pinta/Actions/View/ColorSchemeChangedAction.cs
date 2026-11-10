using System.Text;
using Gio;
using Pinta.Core;

namespace Pinta.Actions;

internal sealed class ColorSchemeChangedAction : IActionHandler
{
	private readonly ISettingsService settings;
	private Gtk.CssProvider? provider;
	internal ColorSchemeChangedAction (ISettingsService settings)
	{
		this.settings = settings;
	}

	void IActionHandler.Initialize ()
	{
		settings.SettingChanged += OnSettingChanged;

		// Load the initial color scheme setting.
		OnSettingChanged (null, new (SettingNames.COLOR_SCHEME));
	}

	void IActionHandler.Uninitialize ()
	{
		settings.SettingChanged -= OnSettingChanged;
		if (provider is not null && Gdk.Display.GetDefault () is Gdk.Display display) {
			Gtk.StyleContext.RemoveProviderForDisplay (display, provider);
			provider.Dispose ();
			provider = null;
		}
	}

	private void OnSettingChanged (object? sender, SettingChangedEventArgs e)
	{
		if (e.Key != SettingNames.COLOR_SCHEME && e.Key != "custom-theme")
			return;

		int schemeIndex = PintaCore.Settings.GetSetting (SettingNames.COLOR_SCHEME, 1);
		Adw.ColorScheme scheme = schemeIndex switch {
			1 => Adw.ColorScheme.ForceLight,
			2 => Adw.ColorScheme.ForceDark,
			_ => Adw.ColorScheme.Default,
		};

		ThemePalette theme = schemeIndex == 2 || (schemeIndex == 0 && Adw.StyleManager.GetDefault ().Dark)
			? ThemePalette.Dark : ThemePalette.Light;
		if (schemeIndex == 3) {
			try { theme = ThemePalette.FromJson (settings.GetSetting ("custom-theme", ThemePalette.Light.ToJson ())); }
			catch (System.Exception) { theme = ThemePalette.Light; }
		}
		if (schemeIndex == 3) {
			int red = System.Convert.ToInt32 (theme.Background.Substring (1, 2), 16);
			int green = System.Convert.ToInt32 (theme.Background.Substring (3, 2), 16);
			int blue = System.Convert.ToInt32 (theme.Background.Substring (5, 2), 16);
			scheme = .2126 * red + .7152 * green + .0722 * blue < 128
				? Adw.ColorScheme.ForceDark : Adw.ColorScheme.ForceLight;
		}
		Adw.StyleManager.GetDefault ().SetColorScheme (scheme);
		Gdk.Display? display = Gdk.Display.GetDefault ();
		if (display is null) return;
		if (provider is not null) {
			Gtk.StyleContext.RemoveProviderForDisplay (display, provider);
			provider.Dispose ();
		}
		provider = Gtk.CssProvider.New ();
		provider.LoadFromBytes (GLib.Bytes.New (Encoding.UTF8.GetBytes (theme.ToCss ())));
		Gtk.StyleContext.AddProviderForDisplay (display, provider, Gtk.Constants.STYLE_PROVIDER_PRIORITY_APPLICATION + 1);
	}
}

