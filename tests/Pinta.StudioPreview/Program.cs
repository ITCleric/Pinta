// Export actual kernel renders without presenting a window or using desktop input.
using System;
using System.IO;
using Path = System.IO.Path;
using System.Diagnostics;
using Cairo;
using Pinta.Core;
using Pinta.Effects;

Gio.Module.Initialize (); GdkPixbuf.Module.Initialize (); Cairo.Module.Initialize (); Gdk.Module.Initialize ();
string directory = Path.GetFullPath (args.Length > 0 ? args[0] : "artifacts/studio-previews");
Directory.CreateDirectory (directory);
using var source = CairoExtensions.CreateImageSurface (Format.Argb32, 360, 240);
var pixels = source.GetPixelData ();
for (int y = 0; y < source.Height; y++) for (int x = 0; x < source.Width; x++) {
	double u = x / 359.0, v = y / 239.0;
	byte r = (byte) (35 + u * 170), g = (byte) (50 + (1 - v) * 150), b = (byte) (80 + (1 - u) * 150);
	if (y > 138 + 24 * Math.Sin (x * .018)) { r = (byte) (35 + v * 30); g = (byte) (65 + u * 70); b = (byte) (80 + v * 60); }
	if (Math.Pow (x - 275, 2) + Math.Pow (y - 52, 2) < 23 * 23) { r = 255; g = 240; b = 182; }
	if (x > 62 && x < 143 && y > 98 && y < 177) { r = 225; g = (byte) (50 + (x + y) % 32); b = 75; }
	if (Math.Pow (x - 207, 2) + Math.Pow (y - 158, 2) < 32 * 32) { r = 242; g = 185; b = 45; }
	if (x > 25 && x < 145 && y > 193 && y < 215) { int band = (x - 25) / 20; r = (byte) (band % 3 == 0 ? 255 : 30); g = (byte) (band % 3 == 1 ? 255 : 30); b = (byte) (band % 3 == 2 ? 255 : 30); }
	byte alpha = x < 10 || y < 10 || x >= 350 || y >= 230 ? (byte) 0 : (byte) 255;
	pixels[y * 360 + x] = ColorBgra.FromBgra (b, g, r, alpha).ToPremultipliedAlpha ();
}
void Save (ImageSurface image, string name) { using var pixbuf = image.ToPixbuf (); pixbuf.Savev (Path.Combine (directory, name + ".png"), "png", [], []); }
Save (source, "source");
var timer = Stopwatch.StartNew ();
foreach (var effect in StudioCatalog.CreateAll ()) {
	using var result = CairoExtensions.CreateImageSurface (Format.Argb32, source.Width, source.Height);
	effect.Render (source, result, [source.GetBounds ()]); Save (result, effect.Info.Slug);
	Console.WriteLine ($"{effect.Info.Name}: {timer.ElapsedMilliseconds}ms cumulative");
}
Console.WriteLine ($"Exported source + 60 real effect renders to {directory}");
