using System;
using Cairo;
using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class TransformFrameTests
{
	[OneTimeSetUp] public void Initialize () => Cairo.Module.Initialize ();
	private static void Point (PointD actual, PointD expected)
	{
		Assert.That (actual.X, Is.EqualTo (expected.X).Within (1e-7));
		Assert.That (actual.Y, Is.EqualTo (expected.Y).Within (1e-7));
	}
	private static Matrix Rotation (double degrees)
	{
		var matrix = CairoExtensions.CreateIdentityMatrix (); matrix.Rotate (degrees * Math.PI / 180); return matrix;
	}
	[Test]
	public void RotatedFrameResizeUsesItsOwnAxes ()
	{
		var frame = TransformFrame.FromRectangle (new (10, 20, 80, 40)).Transform (Rotation (31));
		var scale = frame.Resize (TransformPart.SouthEast, frame.At (1, 1), frame.At (2, 3), false, false);
		var result = frame.Transform (scale);
		Point (result.Origin, frame.Origin);
		Point (result.XAxis, frame.XAxis.Scaled (2)); Point (result.YAxis, frame.YAxis.Scaled (3));
	}
	[TestCase (TransformPart.NorthWest)] [TestCase (TransformPart.North)] [TestCase (TransformPart.NorthEast)]
	[TestCase (TransformPart.East)] [TestCase (TransformPart.SouthEast)] [TestCase (TransformPart.South)]
	[TestCase (TransformPart.SouthWest)] [TestCase (TransformPart.West)]
	public void ResizeKeepsOppositeNubFixed (TransformPart part)
	{
		var frame = TransformFrame.FromRectangle (new (10, 20, 80, 40)).Transform (Rotation (-17));
		var uv = TransformFrame.HandleCoordinate (part);
		var fixedPoint = frame.At (1 - uv.X, 1 - uv.Y);
		var start = frame.At (uv.X, uv.Y);
		var end = fixedPoint + (start - fixedPoint).Scaled (1.5);
		var matrix = frame.Resize (part, start, end, false, false);
		Point (matrix.TransformPoint (fixedPoint), fixedPoint); Point (matrix.TransformPoint (start), end);
	}
	[Test] public void ShiftKeepsAspectAndAltKeepsCenter ()
	{
		var frame = TransformFrame.FromRectangle (new (10, 20, 80, 40)).Transform (Rotation (41));
		var matrix = frame.Resize (TransformPart.SouthEast, frame.At (1, 1), frame.At (2, 1.5), true, true);
		var result = frame.Transform (matrix);
		Point (result.Center, frame.Center);
		Assert.That (result.XAxis.DistanceSquared (PointD.Zero) / result.YAxis.DistanceSquared (PointD.Zero), Is.EqualTo (4).Within (1e-8));
	}
	[Test] public void CrossingOppositeHandleMirrorsWithoutSingularMatrix ()
	{
		var frame = TransformFrame.FromRectangle (new (0, 0, 100, 80));
		var mirrored = frame.Transform (frame.Resize (TransformPart.SouthEast, frame.At (1, 1), frame.At (-1, -.5), false, false));
		Point (mirrored.At (1, 1), new (-100, -40)); Assert.That (mirrored.IsValid, Is.True);
		var collapsed = frame.Transform (frame.Resize (TransformPart.SouthEast, frame.At (1, 1), frame.Origin, false, false));
		Assert.That (collapsed.IsValid, Is.True);
		Assert.That (Math.Sqrt (collapsed.XAxis.DistanceSquared (PointD.Zero)), Is.EqualTo (1));
	}
	[Test] public void LegacyScaleAtCenterCannotDivideByZero ()
	{
		var frame = TransformFrame.FromRectangle (new (0, 0, 100, 80));
		var matrix = frame.ScaleFromPointer (frame.Center, new (80, 70), new (.5, .5), true, true, false);
		Point (matrix.TransformPoint (frame.Origin), frame.Origin);
	}
	[Test] public void ShiftRotationSnapsAbsoluteAngleInFifteenDegreeSteps ()
	{
		var frame = TransformFrame.FromRectangle (new (0, 0, 100, 80)).Transform (Rotation (20));
		var pivot = new PointD (30, -25);
		var start = pivot + new PointD (40, 0);
		var end = pivot + new PointD (40 * Math.Cos (22 * Math.PI / 180), 40 * Math.Sin (22 * Math.PI / 180));
		double delta = frame.RotationDelta (start, end, pivot, true);
		Assert.That (delta * 180 / Math.PI, Is.EqualTo (25).Within (1e-8));
	}
	[TestCase (.1)] [TestCase (1)] [TestCase (8)]
	public void HandleHitToleranceStaysInViewPixels (double zoom)
	{
		var frame = TransformFrame.FromRectangle (new (100, 100, 1000, 800));
		var matrix = CairoExtensions.CreateIdentityMatrix (); matrix.Scale (zoom, zoom); frame = frame.Transform (matrix);
		Assert.That (frame.HitTest (frame.Origin + new PointD (-12, -12), frame.Center), Is.EqualTo (TransformPart.Rotate));
		Assert.That (frame.HitTest (frame.Origin + new PointD (3, 3), frame.Center), Is.EqualTo (TransformPart.NorthWest));
		Assert.That (frame.HitTest (frame.Center, frame.Center), Is.EqualTo (TransformPart.Pivot));
		Assert.That (frame.HitTest (frame.Center + new PointD (13, 0), frame.Center), Is.EqualTo (TransformPart.Rotate));
	}
	[Test] public void SelectionClonesKeepOrientationAndOffCanvasPivot ()
	{
		var selection = new DocumentSelection ();
		selection.CreateRectangleSelection (new (10, 20, 40, 30));
		selection.TransformBounds = TransformFrame.FromRectangle (selection.GetBounds ());
		selection.RotationPivot = new (-15, -20);
		var clone = selection.Clone ();
		var rotation = Rotation (45); var transformed = clone.Transform (rotation);
		Point (transformed.RotationPivot!.Value, rotation.TransformPoint (new PointD (-15, -20)));
		Assert.That (transformed.TransformBounds!.Value.IsValid, Is.True);
		Assert.That (transformed.HandleBounds.Width, Is.GreaterThan (40));
		Point (selection.RotationPivot!.Value, new (-15, -20));
		clone.MarkDirty ();
		Assert.That (clone.TransformBounds, Is.Null); Assert.That (selection.TransformBounds, Is.Not.Null);
	}
}
