using System;
using System.Collections.Generic;
using System.Linq;
using Cairo;
using NUnit.Framework;
using Pinta.Core;

namespace Pinta.Effects.Tests;

[TestFixture]
internal sealed class ModernEffectsTests
{
	[OneTimeSetUp]
	public void InitializeCairo () => Cairo.Module.Initialize ();

	private static IEnumerable<Type> EffectTypes () => typeof (TransparencyEffect).Assembly.GetTypes ()
		.Where (t => t.IsSealed && typeof (BaseEffect).IsAssignableFrom (t) && t.GetConstructor (Type.EmptyTypes) is not null);

	private static ImageSurface Source (int width = 19, int height = 13)
	{
		var image = CairoExtensions.CreateImageSurface (Format.Argb32, width, height);
		var data = image.GetPixelData ();
		for (int i = 0; i < data.Length; i++) data[i] = ColorBgra.FromBgra (
			(byte) (i * 37 % 256), (byte) (i * 53 % 256), (byte) (i * 29 % 256), (byte) (i * 71 % 256)).ToPremultipliedAlpha ();
		return image;
	}

	[TestCaseSource (nameof (EffectTypes))]
	public void TiledRenderMatchesWholeAndPreservesSource (Type type)
	{
		var effect = (BaseEffect) Activator.CreateInstance (type)!;
		using var source = Source ();
		var original = source.GetReadOnlyPixelData ().ToArray ();
		using var whole = CairoExtensions.CreateImageSurface (Format.Argb32, source.Width, source.Height);
		using var tiled = CairoExtensions.CreateImageSurface (Format.Argb32, source.Width, source.Height);
		effect.Render (source, whole, [source.GetBounds ()]);
		for (int y = 0; y < source.Height; y += 4) for (int x = 0; x < source.Width; x += 5)
			effect.Render (source, tiled, [new RectangleI (x, y, Math.Min (5, source.Width - x), Math.Min (4, source.Height - y))]);
		Assert.That (tiled.GetReadOnlyPixelData ().ToArray (), Is.EqualTo (whole.GetReadOnlyPixelData ().ToArray ()));
		Assert.That (source.GetReadOnlyPixelData ().ToArray (), Is.EqualTo (original));
		foreach (var c in whole.GetReadOnlyPixelData ())
			Assert.That (Math.Max (c.R, Math.Max (c.G, c.B)), Is.LessThanOrEqualTo (c.A), "Cairo pixels must remain premultiplied.");
	}

	[TestCaseSource (nameof (EffectTypes))]
	public void HandlesOnePixelAndDoesNotWriteOutsideRoi (Type type)
	{
		var effect = (BaseEffect) Activator.CreateInstance (type)!;
		using var one = Source (1, 1);
		using var oneResult = Source (1, 1);
		effect.Render (one, oneResult, [one.GetBounds ()]);
		using var source = Source ();
		using var dest = Source ();
		dest.GetPixelData ().Fill (ColorBgra.Magenta);
		effect.Render (source, dest, [new RectangleI (3, 4, 2, 2)]);
		var pixels = dest.GetReadOnlyPixelData ();
		for (int y = 0; y < source.Height; y++) for (int x = 0; x < source.Width; x++)
			if (x < 3 || x > 4 || y < 4 || y > 5)
				Assert.That (pixels[y * source.Width + x], Is.EqualTo (ColorBgra.Magenta));
	}

	[Test]
	public void TransparencyScalesColorAndAlphaTogether ()
	{
		using var src = Source (1, 1); using var dst = Source (1, 1);
		src.GetPixelData ()[0] = ColorBgra.FromBgra (64, 32, 16, 128);
		var effect = new TransparencyEffect (); effect.Data.Opacity = 50;
		effect.Render (src, dst, [src.GetBounds ()]);
		Assert.That (dst.GetReadOnlyPixelData ()[0], Is.EqualTo (ColorBgra.FromBgra (31, 15, 7, 64)));
		effect.Data.Opacity = 0;
		effect.Render (src, dst, [src.GetBounds ()]);
		Assert.That (dst.GetReadOnlyPixelData ()[0], Is.EqualTo (ColorBgra.Zero));
	}

	[Test]
	public void AlphaMaskUsesLuminanceAndExistingOpacity ()
	{
		using var src = Source (1, 1); using var dst = Source (1, 1);
		src.GetPixelData ()[0] = ColorBgra.FromBgra (64, 64, 64, 128);
		new GrayToAlphaEffect ().Render (src, dst, [src.GetBounds ()]);
		Assert.That (dst.GetReadOnlyPixelData ()[0].A, Is.EqualTo (63));
	}

	[Test]
	public void ChromaKeyRemovesKeyAndRetainsUnrelatedColors ()
	{
		using var src = Source (2, 1); using var dst = Source (2, 1);
		src.GetPixelData ()[0] = ColorBgra.FromBgra (0, 255, 0, 255); src.GetPixelData ()[1] = ColorBgra.Red;
		new ChromaKeyEffect ().Render (src, dst, [src.GetBounds ()]);
		Assert.That (dst.GetReadOnlyPixelData ()[0].A, Is.Zero);
		Assert.That (dst.GetReadOnlyPixelData ()[1], Is.EqualTo (ColorBgra.Red));
	}

	[Test]
	public void NeutralAdjustmentsAreBitExactWithPartialAlpha ()
	{
		using var src = Source (); using var dst = Source ();
		var opacity = new TransparencyEffect (); opacity.Data.Opacity = 100;
		BaseEffect[] effects = [new ColorBalanceEffect (), new CombinedAdjustmentsEffect (), new ChannelSwizzleEffect (), opacity];
		foreach (var effect in effects) {
			effect.Render (src, dst, [src.GetBounds ()]);
			Assert.That (dst.GetReadOnlyPixelData ().ToArray (), Is.EqualTo (src.GetReadOnlyPixelData ().ToArray ()), effect.GetType ().Name);
		}
	}

	[Test]
	public void CloneOwnsIndependentSettings ()
	{
		var effect = new ColorBalanceEffect (); effect.Data.Red = 42;
		var copy = (ColorBalanceEffect) effect.Clone ();
		effect.Data.Red = -80;
		Assert.That (copy.Data.Red, Is.EqualTo (42));
	}
}
