using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Pinta.Core;

[JsonConverter (typeof (JsonStringEnumConverter<ThemeStyle>))]
public enum ThemeStyle { Windows10, Classic95, Luna, Office2003, Office2007, Aero, Frutiger }

public sealed record ThemePreset (string Id, string Name, string Description, ThemePalette Palette);

/// <summary>Original palettes and desktop skins inspired by familiar eras. No OS assets are bundled.</summary>
public static class ThemePresets
{
	public static IReadOnlyList<ThemePreset> All { get; } = Array.AsReadOnly<ThemePreset> ([
		new ("windows-95", "Windows 95", "Warm gray, navy captions and raised classic borders.", new ("#c0c0c0", "#ffffff", "#000000", "#000080", "#808080", "#808080", "#c4c4dd", ThemeStyle.Classic95)),
		new ("xp-luna", "Windows XP — Luna", "Cream controls, vivid blue captions and an orange close button.", new ("#ece9d8", "#ffffff", "#17223c", "#0054e3", "#7f9db9", "#a3a8b2", "#c1d2ee", ThemeStyle.Luna)),
		new ("xp-olive", "Windows XP — Olive", "Olive captions with pale beige desktop controls.", new ("#ece9d8", "#fffef8", "#25311c", "#587135", "#88946a", "#a6ac99", "#cbd4b4", ThemeStyle.Luna)),
		new ("xp-silver", "Windows XP — Silver", "Silver-lilac chrome with restrained violet selection.", new ("#e5e5ec", "#ffffff", "#292836", "#5d5d86", "#9898ad", "#aaaab7", "#d0cfe5", ThemeStyle.Luna)),
		new ("office-2003", "Office 2003", "Blue command rows with warm orange hover and selection.", new ("#dbe8f8", "#ffffff", "#172943", "#3b6caf", "#859bb8", "#a4b3c5", "#ffd99a", ThemeStyle.Office2003)),
		new ("office-2007", "Office 2007", "Icy blue glossy bars with golden highlights and selection.", new ("#dbe7f5", "#f9fcff", "#20354e", "#426c9e", "#8ca6c5", "#a7b6c7", "#ffe5ad", ThemeStyle.Office2007)),
		new ("vista-2007", "Windows Vista 2007", "Smoky blue glass-style captions with a pronounced gloss band.", new ("#e9edf1", "#ffffff", "#1e2a35", "#315e7d", "#8b9fac", "#aab4bd", "#b4d8ee", ThemeStyle.Aero)),
		new ("windows-7", "Windows 7 — Aero", "Clear sky-blue chrome, light command rows and gentle highlights.", new ("#edf3f8", "#ffffff", "#233548", "#277fb8", "#88a5bc", "#b3c4d1", "#c4e5fa", ThemeStyle.Aero)),
		new ("frutiger-aero", "Frutiger Aero", "Glossy aqua-blue captions with fresh cyan surfaces.", new ("#e2f6fa", "#f9ffff", "#123c50", "#087db4", "#75adbc", "#a5d0d7", "#b8eddf", ThemeStyle.Frutiger)),
		new ("frutiger-eco", "Frutiger Eco", "Leaf-green gloss, pale mint surfaces and warm natural accents.", new ("#e9f4df", "#fdfff8", "#294224", "#3f7f35", "#90ab7c", "#bacdaf", "#d5ef9b", ThemeStyle.Frutiger)),
		new ("midnight", "Midnight", "Deep navy surfaces with cool blue focus and selection.", new ("#172332", "#223247", "#edf4ff", "#63b9f4", "#486078", "#101a27", "#294f70", ThemeStyle.Windows10)),
		new ("graphite", "Graphite", "Neutral charcoal surfaces with a crisp turquoise accent.", new ("#292d30", "#343a3f", "#f1f4f5", "#5fc7c7", "#59666d", "#1d2226", "#36575d", ThemeStyle.Windows10)),
	]);
}
