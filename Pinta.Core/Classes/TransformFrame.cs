using System;
using Cairo;

namespace Pinta.Core;

public enum TransformPart { None, Pivot, Rotate, NorthWest, North, NorthEast, East, SouthEast, South, SouthWest, West }

/// <summary>An oriented selection frame. Value semantics keep clone/undo state independent.</summary>
public readonly record struct TransformFrame (PointD Origin, PointD XAxis, PointD YAxis)
{
	public static TransformFrame FromRectangle (RectangleD r) => new (new (r.X, r.Y), new (r.Width, 0), new (0, r.Height));
	public PointD At (double u, double v) => Origin + XAxis.Scaled (u) + YAxis.Scaled (v);
	public PointD Center => At (.5, .5);
	public bool IsValid => double.IsFinite (Origin.X + Origin.Y + XAxis.X + XAxis.Y + YAxis.X + YAxis.Y) && Math.Abs (Determinant) > 1e-8;
	private double Determinant => XAxis.X * YAxis.Y - XAxis.Y * YAxis.X;
	public PointD ToLocal (PointD point)
	{
		if (!IsValid) return PointD.Zero;
		var d = point - Origin;
		return new ((d.X * YAxis.Y - d.Y * YAxis.X) / Determinant, (XAxis.X * d.Y - XAxis.Y * d.X) / Determinant);
	}
	public TransformFrame Transform (Matrix matrix)
	{
		var origin = matrix.TransformPoint (Origin);
		return new (origin, matrix.TransformPoint (At (1, 0)) - origin, matrix.TransformPoint (At (0, 1)) - origin);
	}
	public PointD[] Corners => [At (0, 0), At (1, 0), At (1, 1), At (0, 1)];
	public RectangleD Bounds {
		get {
			var corners = Corners;
			double x = corners[0].X, y = corners[0].Y, right = x, bottom = y;
			foreach (var p in corners) { x = Math.Min (x, p.X); y = Math.Min (y, p.Y); right = Math.Max (right, p.X); bottom = Math.Max (bottom, p.Y); }
			return new (x, y, right - x, bottom - y);
		}
	}
	public static PointD HandleCoordinate (TransformPart part) => part switch {
		TransformPart.NorthWest => new (0, 0), TransformPart.North => new (.5, 0), TransformPart.NorthEast => new (1, 0),
		TransformPart.East => new (1, .5), TransformPart.SouthEast => new (1, 1), TransformPart.South => new (.5, 1),
		TransformPart.SouthWest => new (0, 1), TransformPart.West => new (0, .5), _ => new (.5, .5),
	};
	public static bool IsResize (TransformPart part) => part >= TransformPart.NorthWest;
	public static readonly TransformPart[] ResizeParts = [TransformPart.NorthWest, TransformPart.NorthEast, TransformPart.SouthEast, TransformPart.SouthWest, TransformPart.North, TransformPart.East, TransformPart.South, TransformPart.West];

	/// <summary>Hit testing is in logical view pixels so grips stay usable at every image zoom.</summary>
	public TransformPart HitTest (PointD point, PointD pivot)
	{
		if (!IsValid) return TransformPart.None;
		double pivotDistance = Math.Sqrt ((point - pivot).DistanceSquared (PointD.Zero));
		if (pivotDistance <= 6) return TransformPart.Pivot;
		foreach (var part in ResizeParts) {
			var uv = HandleCoordinate (part); var handle = At (uv.X, uv.Y);
			if (Math.Abs (point.X - handle.X) <= 7 && Math.Abs (point.Y - handle.Y) <= 7) return part;
		}
		if (pivotDistance >= 9 && pivotDistance <= 17) return TransformPart.Rotate;
		var local = ToLocal (point);
		if (local.X >= 0 && local.X <= 1 && local.Y >= 0 && local.Y <= 1) return TransformPart.None;
		var corners = Corners;
		for (int i = 0; i < 4; i++) if (DistanceToSegment (point, corners[i], corners[(i + 1) % 4]) <= 20) return TransformPart.Rotate;
		return TransformPart.None;
	}
	private static double DistanceToSegment (PointD p, PointD a, PointD b)
	{
		var d = b - a; var v = p - a;
		double t = Math.Clamp ((v.X * d.X + v.Y * d.Y) / Math.Max (1e-10, d.X * d.X + d.Y * d.Y), 0, 1);
		return Math.Sqrt (p.DistanceSquared (a + d.Scaled (t)));
	}
	public double RotationDelta (PointD start, PointD end, PointD pivot, bool snap)
	{
		var a = start - pivot; var b = end - pivot;
		if (a.DistanceSquared (PointD.Zero) < 1e-12 || b.DistanceSquared (PointD.Zero) < 1e-12) return 0;
		double delta = Math.Atan2 (a.X * b.Y - a.Y * b.X, a.X * b.X + a.Y * b.Y);
		if (!snap) return delta;
		double original = Math.Atan2 (XAxis.Y, XAxis.X), step = Math.PI / 12;
		return Math.Round ((original + delta) / step) * step - original;
	}
	/// <summary>Resize in the frame's rotated basis, anchored to the opposite nub (or center).</summary>
	public Matrix Resize (TransformPart part, PointD start, PointD end, bool proportional, bool aboutCenter)
	{
		var uv = HandleCoordinate (part);
		var anchor = aboutCenter ? new PointD (.5, .5) : new PointD (1 - uv.X, 1 - uv.Y);
		return ScaleFromPointer (start, end, anchor, uv.X != .5, uv.Y != .5, proportional);
	}
	public Matrix ScaleFromPointer (PointD start, PointD end, PointD anchor, bool scaleX, bool scaleY, bool proportional)
	{
		if (!IsValid) return CairoExtensions.CreateIdentityMatrix ();
		var a = ToLocal (start) - anchor; var b = ToLocal (end) - anchor;
		double sx = scaleX && Math.Abs (a.X) > 1e-10 ? b.X / a.X : 1;
		double sy = scaleY && Math.Abs (a.Y) > 1e-10 ? b.Y / a.Y : 1;
		if (proportional) {
			double magnitude = scaleX && scaleY ? Math.Max (Math.Abs (sx), Math.Abs (sy)) : scaleX ? Math.Abs (sx) : Math.Abs (sy);
			sx = Math.CopySign (magnitude, sx); sy = Math.CopySign (magnitude, sy);
		}
		sx = Math.CopySign (Math.Max (Math.Abs (sx), 1 / Math.Max (1, Math.Sqrt (XAxis.DistanceSquared (PointD.Zero)))), sx);
		sy = Math.CopySign (Math.Max (Math.Abs (sy), 1 / Math.Max (1, Math.Sqrt (YAxis.DistanceSquared (PointD.Zero)))), sy);
		// B * scale * inverse(B), expressed explicitly to avoid matrix multiplication-order ambiguity.
		double det = Determinant;
		double xx = (XAxis.X * sx * YAxis.Y - YAxis.X * sy * XAxis.Y) / det;
		double xy = (XAxis.Y * YAxis.Y * (sx - sy)) / det;
		double yx = (XAxis.X * YAxis.X * (sy - sx)) / det;
		double yy = (YAxis.Y * sy * XAxis.X - XAxis.Y * sx * YAxis.X) / det;
		var fixedPoint = At (anchor.X, anchor.Y);
		// cairo_matrix_init arguments: xx, yx, xy, yy, x0, y0.
		return CairoExtensions.CreateMatrix (xx, xy, yx, yy, fixedPoint.X - xx * fixedPoint.X - yx * fixedPoint.Y, fixedPoint.Y - xy * fixedPoint.X - yy * fixedPoint.Y);
	}
}
