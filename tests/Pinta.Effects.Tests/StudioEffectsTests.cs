using System;
using System.Linq;
using Cairo;
using NUnit.Framework;
using Pinta.Core;

namespace Pinta.Effects.Tests;

[TestFixture]
internal sealed class StudioEffectsTests
{
	[OneTimeSetUp] public void Initialize () => Cairo.Module.Initialize ();
	private static StudioKind[] Kinds => Enum.GetValues<StudioKind> ();
	private static ImageSurface Fixture (int width = 23, int height = 17)
	{
		var surface = CairoExtensions.CreateImageSurface (Format.Argb32, width, height);
		var pixels = surface.GetPixelData ();
		for (int i = 0; i < pixels.Length; i++) pixels[i] = ColorBgra.FromBgra ((byte) (i * 53), (byte) (i * 37), (byte) (i * 71), (byte) (i * 29)).ToPremultipliedAlpha ();
		return surface;
	}
	private static StudioEffect Effect (StudioKind kind) => StudioCatalog.CreateAll ().Single (e => e.Kind == kind);
	[Test] public void CatalogHasSixtyIndependentEffectsInSixEqualGroups ()
	{
		var effects = StudioCatalog.CreateAll ();
		Assert.That (effects.Length, Is.EqualTo (60));
		Assert.That (effects.Select (e => e.GetType ()).Distinct ().Count (), Is.EqualTo (60));
		Assert.That (effects.Select (e => e.Icon).Distinct ().Count (), Is.EqualTo (60));
		Assert.That (StudioCatalog.All.GroupBy (i => i.Category).Select (g => g.Count ()), Is.EqualTo (Enumerable.Repeat (10, 6)));
		Assert.That (effects.All (e => !string.IsNullOrWhiteSpace (e.Description)), Is.True);
	}
	[TestCaseSource (nameof (Kinds))] public void ZeroStrengthIsExactAndCloneOwnsSettings (StudioKind kind)
	{
		var effect = Effect (kind); effect.Data.Amount = 0;
		var clone = (StudioEffect) effect.Clone (); effect.Data.Amount = 100;
		Assert.That (clone.Data.Amount, Is.Zero);
		using var source = Fixture (); using var result = Fixture ();
		clone.Render (source, result, [source.GetBounds ()]);
		Assert.That (result.GetReadOnlyPixelData ().ToArray (), Is.EqualTo (source.GetReadOnlyPixelData ().ToArray ()));
	}
	[TestCaseSource (nameof (Kinds))] public void ParameterExtremesAreDeterministicAndInPlaceSafe (StudioKind kind)
	{
		var effect = Effect (kind);
		using var source = Fixture (); using var first = Fixture (); using var second = Fixture ();
		foreach (bool maximum in new[] { false, true }) {
			effect.Data.Amount = 100; effect.Data.Radius = maximum ? 64 : 1;
			effect.Data.Scale = maximum ? 128 : 2; effect.Data.Detail = maximum ? 100 : 0;
			effect.Data.Angle = maximum ? 180 : -180; effect.Data.Seed = maximum ? 65535 : 0;
			effect.Render (source, first, [source.GetBounds ()]);
			source.GetReadOnlyPixelData ().CopyTo (second.GetPixelData ());
			effect.Render (second, second, [second.GetBounds ()]);
			Assert.That (second.GetReadOnlyPixelData ().ToArray (), Is.EqualTo (first.GetReadOnlyPixelData ().ToArray ()));
			foreach (var p in first.GetReadOnlyPixelData ()) Assert.That (Math.Max (p.R, Math.Max (p.G, p.B)), Is.LessThanOrEqualTo (p.A));
		}
	}
	[TestCase (1, 1, 1)] [TestCase (7, 9, 2)] [TestCase (13, 6, 64)]
	public void MorphologyMatchesNaiveSquareReference (int width, int height, int radius)
	{
		using var source = Fixture (width, height); using var dest = Fixture (width, height);
		foreach (bool maximum in new[] { false, true }) {
			var effect = Effect (maximum ? StudioKind.AlphaDilate : StudioKind.AlphaErode); effect.Data.Amount = 100; effect.Data.Radius = radius;
			effect.Render (source, dest, [source.GetBounds ()]);
			var src = source.GetReadOnlyPixelData (); var dst = dest.GetReadOnlyPixelData ();
			for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) {
				int expected = maximum ? 0 : 255;
				for (int yy = y - radius; yy <= y + radius; yy++) for (int xx = x - radius; xx <= x + radius; xx++) {
					int a = xx < 0 || yy < 0 || xx >= width || yy >= height ? 0 : src[yy * width + xx].A;
					expected = maximum ? Math.Max (expected, a) : Math.Min (expected, a);
				}
				Assert.That (dst[y * width + x].A, Is.EqualTo (expected), $"{x},{y}; maximum={maximum}");
			}
		}
	}
	[Test] public void FlatNormalMapPointsOutOfSurface ()
	{
		using var src = Fixture (); using var dst = Fixture (); src.GetPixelData ().Fill (ColorBgra.White);
		var effect = Effect (StudioKind.NormalMap); effect.Data.Amount = 100; effect.Render (src, dst, [src.GetBounds ()]);
		Assert.That (dst.GetReadOnlyPixelData ().ToArray ().All (p => p == ColorBgra.FromBgra (255, 128, 128, 255)), Is.True);
	}
	[Test] public void SeamlessBlendMakesOppositeEdgesEqual ()
	{
		using var src = Fixture (); using var dst = Fixture ();
		var effect = Effect (StudioKind.SeamlessBlend); effect.Data.Amount = 100; effect.Render (src, dst, [src.GetBounds ()]);
		var p = dst.GetReadOnlyPixelData ();
		for (int y = 0; y < src.Height; y++) Assert.That (p[y * src.Width], Is.EqualTo (p[(y + 1) * src.Width - 1]));
		for (int x = 0; x < src.Width; x++) Assert.That (p[x], Is.EqualTo (p[(src.Height - 1) * src.Width + x]));
	}
	[Test] public void GlowAndShadowExtendAnObjectIntoTransparency ()
	{
		using var src = Fixture (31, 31); using var dst = Fixture (31, 31); src.GetPixelData ().Fill (ColorBgra.Zero);
		src.GetPixelData ()[15 * 31 + 15] = ColorBgra.White;
		foreach (var kind in new[] { StudioKind.OuterGlow, StudioKind.LongShadow }) {
			var effect = Effect (kind); effect.Data.Radius = 2; effect.Data.Amount = 100;
			effect.Render (src, dst, [src.GetBounds ()]);
			Assert.That (dst.GetReadOnlyPixelData ().ToArray ().Count (p => p.A > 0), Is.GreaterThan (1), kind.ToString ());
			Assert.That (dst.GetReadOnlyPixelData ()[15 * 31 + 15], Is.EqualTo (ColorBgra.White));
		}
	}
	[Test] public void PixelSortOrdersEligibleRunButKeepsDarkBoundary ()
	{
		using var src = Fixture (5, 1); using var dst = Fixture (5, 1);
		byte[] values = [200, 100, 150, 0, 220];
		for (int x = 0; x < 5; x++) src.GetPixelData ()[x] = ColorBgra.FromBgra (values[x], values[x], values[x], 255);
		var effect = Effect (StudioKind.PixelSort); effect.Data.Amount = 100; effect.Data.Detail = 20; effect.Data.Scale = 8;
		effect.Render (src, dst, [src.GetBounds ()]);
		Assert.That (dst.GetReadOnlyPixelData ().ToArray ().Select (p => p.R), Is.EqualTo (new byte[] { 100, 150, 200, 0, 220 }));
	}
}
