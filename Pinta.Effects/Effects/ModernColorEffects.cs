// Independently implemented effects, licensed under the project's MIT license.
// No Paint.NET or third-party plugin binaries or source are used here.
using System;
using Cairo;
using Pinta.Core;

namespace Pinta.Effects;

public abstract class ModernPixelEffect : ConfigurableEffect
{
	public override bool IsTileable => true;
	protected abstract ColorBgra Transform (ColorBgra straight);
	protected override ColorBgra Render (in ColorBgra color)
	{
		ColorBgra straight = color.ToStraightAlpha ();
		ColorBgra result = Transform (straight);
		// Preserve exact premultiplied values for neutral adjustments.
		return result == straight ? color : result.ToPremultipliedAlpha ();
	}
	protected static byte Byte (double value) => (byte) Math.Clamp ((int) Math.Round (value), 0, 255);
}

public sealed class TransparencyEffect : ModernPixelEffect
{
	public override string Name => Translations.GetString ("Transparency");
	public override string Icon => "pinta-modern-transparency";
	public TransparencyData Data => (TransparencyData) EffectData!;
	public TransparencyEffect () { EffectData = new TransparencyData (); }
	protected override ColorBgra Transform (ColorBgra p) => ColorBgra.FromBgra (p.B, p.G, p.R, Byte (p.A * Data.Opacity / 100.0));
}
public sealed class TransparencyData : EffectData
{
	[Caption ("Opacity (%)"), MinimumValue (0), MaximumValue (100)]
	public int Opacity { get; set; } = 75;
}

public sealed class InvertAlphaEffect : BaseEffect
{
	public override string Name => Translations.GetString ("Invert Alpha");
	public override string Icon => "pinta-modern-invert-alpha";
	public override bool IsTileable => true;
	public override string AdjustmentMenuKey => "I";
	public override string AdjustmentMenuKeyModifiers => "<Primary><Alt>";
	protected override ColorBgra Render (in ColorBgra color) => color.NewAlpha ((byte) (255 - color.A));
}

public sealed class ColorBalanceEffect : ModernPixelEffect
{
	public override string Name => Translations.GetString ("Color Balance");
	public override string Icon => "pinta-modern-color-balance";
	public ColorBalanceData Data => (ColorBalanceData) EffectData!;
	public ColorBalanceEffect () { EffectData = new ColorBalanceData (); }
	protected override ColorBgra Transform (ColorBgra p) => ColorBgra.FromBgra (
		Byte (p.B + Data.Blue), Byte (p.G + Data.Green), Byte (p.R + Data.Red), p.A);
}
public sealed class ColorBalanceData : EffectData
{
	[Caption ("Cyan / Red"), MinimumValue (-100), MaximumValue (100)]
	public int Red { get; set; }
	[Caption ("Magenta / Green"), MinimumValue (-100), MaximumValue (100)]
	public int Green { get; set; }
	[Caption ("Yellow / Blue"), MinimumValue (-100), MaximumValue (100)]
	public int Blue { get; set; }
}

public sealed class BlackAndWhitePlusEffect : ModernPixelEffect
{
	public override string Name => Translations.GetString ("Black and White Plus");
	public override string Icon => "pinta-modern-monochrome";
	public ChannelMixerData Data => (ChannelMixerData) EffectData!;
	public BlackAndWhitePlusEffect () { EffectData = new ChannelMixerData (); }
	protected override ColorBgra Transform (ColorBgra p)
	{
		byte gray = Byte ((p.R * Data.Red + p.G * Data.Green + p.B * Data.Blue) / 100.0);
		return ColorBgra.FromBgra (gray, gray, gray, p.A);
	}
}
public sealed class ChannelMixerData : EffectData
{
	[Caption ("Red Weight (%)"), MinimumValue (-200), MaximumValue (200)]
	public int Red { get; set; } = 30;
	[Caption ("Green Weight (%)"), MinimumValue (-200), MaximumValue (200)]
	public int Green { get; set; } = 59;
	[Caption ("Blue Weight (%)"), MinimumValue (-200), MaximumValue (200)]
	public int Blue { get; set; } = 11;
}

public sealed class AlphaToGrayEffect : BaseEffect
{
	public override string Name => Translations.GetString ("Alpha to Gray");
	public override string Icon => "pinta-modern-alpha-gray";
	public override bool IsTileable => true;
	public override string EffectMenuCategory => Translations.GetString ("Object");
	protected override ColorBgra Render (in ColorBgra p) => ColorBgra.FromBgra (p.A, p.A, p.A, 255);
}

public sealed class GrayToAlphaEffect : BaseEffect
{
	public override string Name => Translations.GetString ("Gray to Alpha");
	public override string Icon => "pinta-modern-gray-alpha";
	public override bool IsTileable => true;
	public override string EffectMenuCategory => Translations.GetString ("Object");
	protected override ColorBgra Render (in ColorBgra color)
	{
		var p = color.ToStraightAlpha ();
		byte alpha = (byte) ((p.R * 299 + p.G * 587 + p.B * 114) / 1000 * p.A / 255);
		return ColorBgra.FromBgra (255, 255, 255, alpha).ToPremultipliedAlpha ();
	}
}

public sealed class ComplementaryColorsEffect : BaseEffect
{
	public override string Name => Translations.GetString ("Complementary Colors");
	public override string Icon => "pinta-modern-complementary";
	public override bool IsTileable => true;
	public override string EffectMenuCategory => Translations.GetString ("Color");
	protected override ColorBgra Render (in ColorBgra color)
	{
		var p = color.ToStraightAlpha ();
		int sum = Math.Max (p.R, Math.Max (p.G, p.B)) + Math.Min (p.R, Math.Min (p.G, p.B));
		return ColorBgra.FromBgra ((byte) (sum - p.B), (byte) (sum - p.G), (byte) (sum - p.R), p.A).ToPremultipliedAlpha ();
	}
}

public sealed class CombinedAdjustmentsEffect : ModernPixelEffect
{
	public override string Name => Translations.GetString ("Combined Adjustments");
	public override string Icon => "pinta-modern-combined";
	public CombinedAdjustmentsData Data => (CombinedAdjustmentsData) EffectData!;
	public CombinedAdjustmentsEffect () { EffectData = new CombinedAdjustmentsData (); }
	protected override ColorBgra Transform (ColorBgra p)
	{
		double gray = p.R * .2126 + p.G * .7152 + p.B * .0722;
		double saturation = Data.Saturation / 100.0;
		double exposure = Math.Pow (2, Data.Exposure);
		double gamma = 1 / Math.Max (.1, Data.Gamma);
		byte Channel (byte c) => Byte (255 * Math.Pow (Math.Clamp ((gray + (c - gray) * saturation) * exposure / 255, 0, 1), gamma));
		return ColorBgra.FromBgra (Channel (p.B), Channel (p.G), Channel (p.R), p.A);
	}
}
public sealed class CombinedAdjustmentsData : EffectData
{
	[Caption ("Exposure (stops)"), MinimumValue (-5), MaximumValue (5)]
	public double Exposure { get; set; }
	[Caption ("Saturation (%)"), MinimumValue (0), MaximumValue (200)]
	public int Saturation { get; set; } = 100;
	[Caption ("Gamma"), MinimumValue (0), MaximumValue (5)]
	public double Gamma { get; set; } = 1;
}

/// <summary>RGB channel controls inspired by the wider plugin ecosystem.</summary>
public sealed class ChannelSwizzleEffect : ModernPixelEffect
{
	public override string Name => Translations.GetString ("Channel Mixer");
	public override string Icon => "pinta-modern-channels";
	public override string EffectMenuCategory => Translations.GetString ("Color");
	public ChannelSwizzleData Data => (ChannelSwizzleData) EffectData!;
	public ChannelSwizzleEffect () { EffectData = new ChannelSwizzleData (); }
	protected override ColorBgra Transform (ColorBgra p)
	{
		byte Channel (int i) => i switch { 0 => p.R, 1 => p.G, 2 => p.B, 3 => p.A, _ => 0 };
		return ColorBgra.FromBgra (Channel (Data.BlueSource), Channel (Data.GreenSource), Channel (Data.RedSource), p.A);
	}
}
public sealed class ChannelSwizzleData : EffectData
{
	[Caption ("Red Source (0=R, 1=G, 2=B, 3=A)"), MinimumValue (0), MaximumValue (3)]
	public int RedSource { get; set; }
	[Caption ("Green Source (0=R, 1=G, 2=B, 3=A)"), MinimumValue (0), MaximumValue (3)]
	public int GreenSource { get; set; } = 1;
	[Caption ("Blue Source (0=R, 1=G, 2=B, 3=A)"), MinimumValue (0), MaximumValue (3)]
	public int BlueSource { get; set; } = 2;
}
