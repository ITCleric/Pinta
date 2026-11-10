using System;
using Pinta.Core;

namespace Pinta.Effects;

internal static class StudioGeometry
{
	public static ColorBgra Process (StudioKind kind, StudioSettings s, ref StudioFrame f, ColorBgra p, int x, int y)
	{
		double cx = (f.Width - 1) * .5, cy = (f.Height - 1) * .5;
		double dx = x - cx, dy = y - cy, r = Math.Sqrt (dx * dx + dy * dy);
		double maxRadius = Math.Max (1, Math.Min (f.Width, f.Height) * .5);
		double angle = Math.Atan2 (dy, dx), sx, sy;
		switch (kind) {
			case StudioKind.Kaleidoscope:
				double sector = Math.PI * 2 / s.Scale;
				double folded = Math.Abs (StudioMath.Fract ((angle + s.Angle) / sector) * sector - sector * .5);
				sx = cx + Math.Cos (folded) * r; sy = cy + Math.Sin (folded) * r;
				break;
			case StudioKind.MirrorQuadrants:
				sx = cx + Math.Abs (dx); sy = cy + Math.Abs (dy);
				break;
			case StudioKind.TinyPlanet:
				double u = StudioMath.Fract ((angle + s.Angle) / (Math.PI * 2));
				double latitude = 2 * Math.Atan (r / (maxRadius * s.Scale / 64.0)) / Math.PI;
				sx = u * Math.Max (0, f.Width - 1); sy = (1 - latitude) * Math.Max (0, f.Height - 1);
				break;
			case StudioKind.BarrelLens:
				double factor = 1 + (r * r / (maxRadius * maxRadius)) * s.Detail * .65;
				sx = cx + dx * factor; sy = cy + dy * factor;
				break;
			case StudioKind.PerspectiveSkew:
				double perspective = Math.Max (.25, 1 + dy / Math.Max (1, cy) * s.Detail * .7);
				sx = cx + dx / perspective + Math.Sin (s.Angle) * dy * .5;
				sy = cy + dy / perspective;
				break;
			case StudioKind.SpiralTunnel:
				double logRadius = Math.Log (1 + r / maxRadius * 8);
				double rr = StudioMath.Fract (logRadius * s.Scale / 24.0) * maxRadius;
				double spiral = angle + logRadius * s.Angle * 2;
				sx = cx + Math.Cos (spiral) * rr; sy = cy + Math.Sin (spiral) * rr;
				break;
			case StudioKind.RippleRings:
				double movedRadius = r + Math.Sin (r * Math.PI * 2 / s.Scale) * s.Radius;
				sx = cx + Math.Cos (angle) * movedRadius; sy = cy + Math.Sin (angle) * movedRadius;
				break;
			case StudioKind.SineWeave:
				double xx = (dx * Math.Cos (s.Angle) + dy * Math.Sin (s.Angle)) / s.Scale;
				double yy = (-dx * Math.Sin (s.Angle) + dy * Math.Cos (s.Angle)) / s.Scale;
				sx = x + Math.Sin (yy * Math.PI * 2) * Math.Cos (xx * Math.PI) * s.Radius;
				sy = y + Math.Sin (xx * Math.PI * 2) * Math.Cos (yy * Math.PI) * s.Radius;
				break;
			case StudioKind.PixelSort: return f.Sorted (x, y, s.Scale, s.Detail);
			case StudioKind.PrismTiles:
				int tileX = x / s.Scale * s.Scale, tileY = y / s.Scale * s.Scale;
				double localX = x - tileX, localY = y - tileY;
				if (localX + localY >= s.Scale) { localX = s.Scale - 1 - localX; localY = s.Scale - 1 - localY; }
				sx = tileX + localX; sy = tileY + localY;
				break;
			default: throw new ArgumentOutOfRangeException (nameof (kind));
		}
		return f.Sample (sx, sy);
	}
}
