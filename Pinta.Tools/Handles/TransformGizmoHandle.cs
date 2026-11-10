using Pinta.Core;

namespace Pinta.Tools;

/// <summary>Selection box, resize nubs and a central rotation dial; all sizes are view pixels.</summary>
public sealed class TransformGizmoHandle (IWorkspaceService workspace) : IOverlayToolHandle
{
	public bool Active { get; set; }
	public TransformFrame Frame { get; set; }
	public PointD Pivot { get; set; }
	public PointD? GuidePoint { get; set; }
	public TransformPart Highlight { get; set; }
	public TransformFrame ViewFrame {
		get {
			var origin = workspace.CanvasPointToView (Frame.Origin);
			return new (origin, workspace.CanvasPointToView (Frame.At (1, 0)) - origin, workspace.CanvasPointToView (Frame.At (0, 1)) - origin);
		}
	}
	public TransformPart HitTest (PointD viewPoint) => Active ? ViewFrame.HitTest (viewPoint, workspace.CanvasPointToView (Pivot)) : TransformPart.None;
	public bool ContainsPoint (PointD windowPoint) => HitTest (windowPoint) != TransformPart.None;

	private static readonly Gdk.RGBA white = new () { Red = 1, Green = 1, Blue = 1, Alpha = 1 };
	private static readonly Gdk.RGBA blue = new () { Red = .04f, Green = .35f, Blue = .75f, Alpha = 1 };
	private static readonly Gdk.RGBA orange = new () { Red = 1, Green = .6f, Blue = .1f, Alpha = 1 };

	public void Draw (Gtk.Snapshot snapshot)
	{
		if (!Active || !Frame.IsValid) return;
		var frame = ViewFrame;
		var outline = Gsk.PathBuilder.New ();
		var corners = frame.Corners;
		outline.MoveTo ((float) corners[0].X, (float) corners[0].Y);
		for (int i = 1; i <= 4; i++) outline.LineTo ((float) corners[i % 4].X, (float) corners[i % 4].Y);
		var path = outline.ToPath ();
		snapshot.AppendStroke (path, Gsk.Stroke.New (3), white);
		var stroke = Gsk.Stroke.New (1); stroke.SetDash ([4, 3]);
		snapshot.AppendStroke (path, stroke, blue);
		foreach (var part in TransformFrame.ResizeParts) {
			var uv = TransformFrame.HandleCoordinate (part); var p = frame.At (uv.X, uv.Y);
			float x = (float) p.X, y = (float) p.Y;
			var nub = Gsk.PathBuilder.New ();
			nub.MoveTo (x - 3.5f, y - 3.5f); nub.LineTo (x + 3.5f, y - 3.5f); nub.LineTo (x + 3.5f, y + 3.5f); nub.LineTo (x - 3.5f, y + 3.5f); nub.LineTo (x - 3.5f, y - 3.5f);
			var nubPath = nub.ToPath ();
			snapshot.AppendFill (nubPath, Gsk.FillRule.EvenOdd, Highlight == part ? orange : white);
			snapshot.AppendStroke (nubPath, Gsk.Stroke.New (1), blue);
		}
		var pivot = workspace.CanvasPointToView (Pivot);
		if (GuidePoint is PointD guide) {
			var viewGuide = workspace.CanvasPointToView (guide); var line = Gsk.PathBuilder.New ();
			line.MoveTo ((float) pivot.X, (float) pivot.Y); line.LineTo ((float) viewGuide.X, (float) viewGuide.Y);
			snapshot.AppendStroke (line.ToPath (), stroke, blue);
		}
		var ring = Gsk.PathBuilder.New (); ring.AddCircle (pivot.ToGraphenePoint (), 13);
		var ringPath = ring.ToPath ();
		snapshot.AppendStroke (ringPath, Gsk.Stroke.New (4), white);
		snapshot.AppendStroke (ringPath, Gsk.Stroke.New (2), Highlight == TransformPart.Rotate ? orange : blue);
		var center = Gsk.PathBuilder.New (); center.AddCircle (pivot.ToGraphenePoint (), 5);
		var centerPath = center.ToPath ();
		snapshot.AppendFill (centerPath, Gsk.FillRule.EvenOdd, Highlight == TransformPart.Pivot ? orange : white);
		snapshot.AppendStroke (centerPath, Gsk.Stroke.New (1), blue);
		var cross = Gsk.PathBuilder.New ();
		cross.MoveTo ((float) pivot.X - 3, (float) pivot.Y); cross.LineTo ((float) pivot.X + 3, (float) pivot.Y);
		cross.MoveTo ((float) pivot.X, (float) pivot.Y - 3); cross.LineTo ((float) pivot.X, (float) pivot.Y + 3);
		// Arrowhead on the dial distinguishes it from a resize nub or move pivot.
		cross.MoveTo ((float) pivot.X + 11, (float) pivot.Y - 7); cross.LineTo ((float) pivot.X + 15, (float) pivot.Y - 5); cross.LineTo ((float) pivot.X + 14, (float) pivot.Y - 10);
		snapshot.AppendStroke (cross.ToPath (), Gsk.Stroke.New (1.5f), blue);
	}
}
