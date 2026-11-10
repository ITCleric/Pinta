using System;

namespace Pinta.Core;

public static class FloatingPalettePosition
{
	public static (int X, int Y) Clamp (double x, double y, int workspaceWidth, int workspaceHeight, int paletteWidth, int paletteHeight) => (
		(int) Math.Clamp (x, 0, Math.Max (0, workspaceWidth - paletteWidth)),
		(int) Math.Clamp (y, 0, Math.Max (0, workspaceHeight - paletteHeight)));
}
