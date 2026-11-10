namespace Pinta.Core;

/// <summary>Keep macOS system shortcuts free, while retaining the Paint.NET key letters.</summary>
public static class ShortcutPolicy
{
	public static string ForPlatform (string shortcut) => ForPlatform (shortcut, SystemManager.GetOperatingSystem () == OS.Mac);

	public static string ForPlatform (string shortcut, bool isMac) => !isMac ? shortcut : shortcut switch {
		"<Primary>H" => "<Primary><Alt>H", // Cmd+H hides the app.
		"<Primary>M" => "<Primary><Alt>M", // Cmd+M minimizes the window.
		"<Primary>Y" => "<Primary><Shift>Z", // Native redo.
		"<Primary><Shift>Z" => "<Primary><Alt>Z", // Keep rotate/zoom distinct from redo.
		_ => shortcut,
	};
}
