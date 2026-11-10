// 
// BaseTransformTool.cs
//  
// Author:
//       Volodymyr <${AuthorEmail}>
// 
// Copyright (c) 2012 Volodymyr
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using Cairo;
using Pinta.Core;

namespace Pinta.Tools;

public abstract class BaseTransformTool : BaseTool
{
	private readonly IWorkspaceService workspace;
	private readonly ChromeManager? chrome;
	private readonly TransformGizmoHandle gizmo;
	private readonly Gdk.Cursor rotate_cursor;
	private readonly Matrix transform = CairoExtensions.CreateIdentityMatrix ();
	private TransformFrame start_frame;
	private PointD start_pivot, original_point;
	private TransformPart drag_part;
	private bool is_dragging, is_rotating, is_scaling, using_mouse, moving_pivot, started;
	private string? last_hint;
	private readonly Dictionary<string, Gdk.Cursor> cursors = [];
	private bool IsActive => is_dragging || is_rotating || is_scaling || moving_pivot;

	public BaseTransformTool (IServiceProvider services) : base (services)
	{
		workspace = services.GetService<IWorkspaceService> ();
		chrome = services.GetOptionalService<IChromeService> () as ChromeManager;
		gizmo = new (workspace);
		rotate_cursor = Gdk.Cursor.NewFromTexture (Resources.GetIcon (Pinta.Resources.Icons.TransformRotate, 32), 16, 16, null);
	}
	public override IEnumerable<IToolHandle> Handles {
		get {
			gizmo.Active = IsActiveTool () && workspace.HasOpenDocuments;
			if (gizmo.Active) SynchronizeGizmo (workspace.ActiveDocument);
			return [gizmo];
		}
	}
	private void SynchronizeGizmo (Document document)
	{
		gizmo.Frame = document.Selection.TransformBounds ?? TransformFrame.FromRectangle (GetSourceRectangle (document));
		gizmo.Pivot = document.Selection.RotationPivot ?? gizmo.Frame.Center;
		gizmo.Active = gizmo.Frame.IsValid && (!IsSelectionTool || document.Selection.Visible);
	}
	protected override void OnBuildToolBar (Gtk.Box toolbar)
	{
		base.OnBuildToolBar (toolbar);
		var reset = Gtk.Button.NewWithLabel (Translations.GetString ("Reset Pivot"));
		reset.AddCssClass ("editor-tile");
		reset.TooltipText = Translations.GetString ("Return the rotation point to the center of the selection.");
		reset.OnClicked += (_, _) => {
			if (!workspace.HasOpenDocuments) return;
			if (IsActive) FinishGesture (workspace.ActiveDocument);
			workspace.ActiveDocument.Selection.RotationPivot = null;
			workspace.Invalidate ();
		};
		toolbar.Append (reset);
	}

	protected override void OnActivated (Document? document)
	{
		base.OnActivated (document);
		if (document is not null) { SynchronizeGizmo (document); document.Workspace.Invalidate (); }
	}
	protected override void OnMouseDown (Document document, ToolMouseEventArgs e)
	{
		if (IsActive || (e.MouseButton != MouseButton.Left && e.MouseButton != MouseButton.Right)) return;
		SynchronizeGizmo (document);
		if (!gizmo.Active) return;
		drag_part = gizmo.HitTest (e.WindowPoint);
		if (!document.Workspace.PointInCanvas (e.PointDouble) && drag_part == TransformPart.None && e.MouseButton != MouseButton.Right) return;
		original_point = e.PointDouble;
		start_frame = gizmo.Frame; start_pivot = gizmo.Pivot;
		is_rotating = e.MouseButton == MouseButton.Right || drag_part == TransformPart.Rotate;
		moving_pivot = !is_rotating && drag_part == TransformPart.Pivot;
		is_scaling = !is_rotating && !moving_pivot && (TransformFrame.IsResize (drag_part) || e.IsControlPressed);
		is_dragging = !is_rotating && !moving_pivot && !is_scaling;
		using_mouse = true; started = false; transform.InitIdentity ();
		gizmo.Highlight = is_rotating ? TransformPart.Rotate : drag_part;
		SetCursor (is_rotating ? rotate_cursor : moving_pivot ? NamedCursor (Pinta.Resources.StandardCursors.Move) : TransformFrame.IsResize (drag_part) ? ResizeCursor (drag_part) : DefaultCursor);
		document.Workspace.Invalidate ();
	}
	protected override void OnMouseMove (Document document, ToolMouseEventArgs e)
	{
		if (!IsActive || !using_mouse) {
			SynchronizeGizmo (document);
			var hit = gizmo.HitTest (e.WindowPoint);
			if (gizmo.Highlight != hit) { gizmo.Highlight = hit; document.Workspace.Invalidate (); }
			SetCursor (hit == TransformPart.Rotate ? rotate_cursor : hit == TransformPart.Pivot ? NamedCursor (Pinta.Resources.StandardCursors.Move) : TransformFrame.IsResize (hit) ? ResizeCursor (hit) : DefaultCursor);
			SetHint (hit == TransformPart.Rotate ? Translations.GetString ("Drag to rotate. Hold Shift for 15° steps.") : hit == TransformPart.Pivot ? Translations.GetString ("Drag the cross to move the pivot; drag its outer ring to rotate.") : StatusBarText);
			return;
		}
		if (moving_pivot) {
			document.Selection.RotationPivot = start_pivot + (e.PointDouble - original_point);
			document.Workspace.Invalidate ();
			return;
		}
		if (!started) {
			if (workspace.CanvasPointToView (original_point).DistanceSquared (e.WindowPoint) < .25) return;
			OnStartTransform (document); started = true;
		}
		if (is_scaling) {
			var scale = TransformFrame.IsResize (drag_part)
				? start_frame.Resize (drag_part, original_point, e.PointDouble, e.IsShiftPressed, e.IsAltPressed)
				: start_frame.ScaleFromPointer (original_point, e.PointDouble, new (.5, .5), true, true, e.IsShiftPressed);
			transform.InitMatrix (scale);
		} else if (is_rotating) {
			double delta = start_frame.RotationDelta (original_point, e.PointDouble, start_pivot, e.IsShiftPressed);
			transform.InitIdentity (); transform.Translate (start_pivot.X, start_pivot.Y); transform.Rotate (delta); transform.Translate (-start_pivot.X, -start_pivot.Y);
			gizmo.GuidePoint = e.PointDouble;
			SetCursor (rotate_cursor);
			double degrees = (Math.Atan2 (start_frame.XAxis.Y, start_frame.XAxis.X) + delta) * 180 / Math.PI;
			SetHint ($"{Translations.GetString ("Rotation")}: {degrees:0.0}° — {Translations.GetString ("Shift: 15° steps")}");
		} else {
			transform.InitIdentity ();
			transform.Translate (Math.Floor (e.PointDouble.X - original_point.X), Math.Floor (e.PointDouble.Y - original_point.Y));
		}
		OnUpdateTransform (document, transform);
	}
	protected override void OnMouseUp (Document document, ToolMouseEventArgs e)
	{
		if (!IsActive || !using_mouse) return;
		OnMouseMove (document, e);
		FinishGesture (document);
	}
	private void FinishGesture (Document document)
	{
		if (started) OnFinishTransform (document, transform);
		ResetGesture ();
		SetHint (StatusBarText); document.Workspace.Invalidate ();
	}
	private void ResetGesture ()
	{
		is_dragging = is_rotating = is_scaling = using_mouse = moving_pivot = started = false;
		gizmo.GuidePoint = null; gizmo.Highlight = TransformPart.None;
	}
	private void SetHint (string text)
	{
		if (last_hint == text) return;
		last_hint = text; chrome?.SetStatusBarText ($"{Name}: {text}");
	}
	private Gdk.Cursor NamedCursor (string name)
	{
		if (!cursors.TryGetValue (name, out var cursor)) cursors[name] = cursor = GdkExtensions.CursorFromName (name);
		return cursor;
	}
	private Gdk.Cursor ResizeCursor (TransformPart part)
	{
		var uv = TransformFrame.HandleCoordinate (part); var d = gizmo.Frame.At (uv.X, uv.Y) - gizmo.Frame.Center;
		string[] names = ["e-resize", "se-resize", "s-resize", "sw-resize", "w-resize", "nw-resize", "n-resize", "ne-resize"];
		int index = ((int) Math.Round (Math.Atan2 (d.Y, d.X) / (Math.PI / 4)) + 8) % 8;
		return NamedCursor (names[index]);
	}
	protected override bool OnKeyDown (Document document, ToolKeyEventArgs e)
	{
		if (using_mouse) return base.OnKeyDown (document, e);
		double dx = 0, dy = 0, amount = e.IsControlPressed ? 10 : 1;
		switch (e.Key.Value) {
			case Gdk.Constants.KEY_Left: dx = -amount; break;
			case Gdk.Constants.KEY_Right: dx = amount; break;
			case Gdk.Constants.KEY_Up: dy = -amount; break;
			case Gdk.Constants.KEY_Down: dy = amount; break;
			default: return base.OnKeyDown (document, e);
		}
		if (!IsActive) {
			SynchronizeGizmo (document); if (!gizmo.Active) return false;
			start_frame = gizmo.Frame; start_pivot = gizmo.Pivot;
			is_dragging = true; OnStartTransform (document); started = true;
		}
		transform.Translate (dx, dy); OnUpdateTransform (document, transform);
		return true;
	}
	protected override bool OnKeyUp (Document document, ToolKeyEventArgs e)
	{
		if (IsActive && !using_mouse) FinishGesture (document);
		return base.OnKeyUp (document, e);
	}
	protected abstract RectangleD GetSourceRectangle (Document document);
	protected virtual void OnStartTransform (Document document)
	{
		document.Selection.TransformBounds = start_frame;
		document.Selection.RotationPivot = start_pivot;
		transform.InitIdentity ();
	}
	protected virtual void OnUpdateTransform (Document document, Matrix transform) { }
	protected virtual void OnFinishTransform (Document document, Matrix transform) { }
	protected override bool OnHandleUndo (Document document)
	{
		if (IsActive) FinishGesture (document);
		return base.OnHandleUndo (document);
	}
	protected override bool OnHandleRedo (Document document)
	{
		if (IsActive) FinishGesture (document);
		return base.OnHandleRedo (document);
	}
	protected override void OnCommit (Document? document)
	{
		if (document is not null && IsActive) FinishGesture (document);
		base.OnCommit (document);
	}
	protected override void OnDeactivated (Document? document, BaseTool? newTool)
	{
		if (document is not null && IsActive) FinishGesture (document);
		ResetGesture (); gizmo.Active = false;
		base.OnDeactivated (document, newTool);
	}
}
