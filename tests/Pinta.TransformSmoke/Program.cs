// Actual tools/documents/history through the mouse dispatcher. No windows or desktop input are presented.
using System;
using System.IO;
using System.Linq;
using Cairo;
using Pinta;
using Pinta.Core;
using Pinta.Tools;
using Path = System.IO.Path;

Environment.SetEnvironmentVariable ("PINTA_SETTINGS_DIRECTORY", Path.GetFullPath ("artifacts/verification/transform-profile"));
GLib.Module.Initialize (); Gio.Module.Initialize (); Cairo.Module.Initialize (); GdkPixbuf.Module.Initialize ();
Gtk.Module.Initialize (); Adw.Module.Initialize (); Gsk.Module.Initialize (); Gtk.Functions.Init (); Adw.Functions.Init ();
Gtk.IconTheme.GetForDisplay (Gdk.Display.GetDefault ()!).AddSearchPath (Path.Combine (AppContext.BaseDirectory, "icons"));
using var root = Gtk.Window.New ();
var documents = Gtk.Box.New (Gtk.Orientation.Vertical, 0); root.SetChild (documents);
using var application = Gtk.Application.New ("io.github.itcleric.transformtests", Gio.ApplicationFlags.NonUnique);
application.Register (null);
PintaCore.Chrome.InitializeApplication (application);
PintaCore.Actions.Window.RegisterActions (application, Gio.Menu.New ());
PintaCore.Chrome.InitializeWindowShell (root);
PintaCore.Chrome.InitializeToolToolBar (Gtk.Box.New (Gtk.Orientation.Horizontal, 0));
PintaCore.Workspace.DocumentActivated += (_, e) => {
	var canvas = CanvasWindow.New (PintaCore.Chrome, PintaCore.Tools, e.Document, PintaCore.CanvasGrid, PintaCore.Settings);
	e.Document.Workspace.CanvasWindow = canvas; e.Document.Workspace.Canvas = canvas.Canvas; documents.Append (canvas);
};
var move = new MoveSelectedTool (PintaCore.Services);
var outline = new MoveSelectionTool (PintaCore.Services);
PintaCore.Tools.AddTool (move); PintaCore.Tools.AddTool (outline);
int checks = 0;
void Check (bool value, string text) { if (!value) throw new Exception (text); checks++; Console.WriteLine ("PASS " + text); }
void Near (PointD actual, PointD expected, string text) => Check (actual.DistanceSquared (expected) < 1e-10, text);
Document NewDocument (bool selected = true)
{
	var doc = PintaCore.Workspace.NewDocument (new (120, 100), new Color (0, 0, 0, 0));
	if (selected) { doc.Selection.CreateRectangleSelection (new (30, 25, 60, 40)); doc.Selection.Visible = true; }
	var data = doc.Layers.CurrentUserLayer.Surface.GetPixelData ();
	for (int y = 25; y < 65; y++) for (int x = 30; x < 90; x++) data[y * 120 + x] = ColorBgra.FromBgra ((byte) x, (byte) y, (byte) (x + y), 255);
	doc.Layers.CurrentUserLayer.Surface.MarkDirty ();
	PintaCore.Tools.SetCurrentTool (move);
	return doc;
}
ToolMouseEventArgs Mouse (Document doc, PointD point, MouseButton button = MouseButton.Left, Gdk.ModifierType modifiers = 0) => new () {
	PointDouble = point, WindowPoint = doc.Workspace.CanvasPointToView (point), RootPoint = doc.Workspace.CanvasPointToView (point), MouseButton = button, State = modifiers,
};
void Drag (Document doc, PointD start, PointD end, MouseButton button = MouseButton.Left, Gdk.ModifierType modifiers = 0)
{
	PintaCore.Tools.DoMouseDown (doc, Mouse (doc, start, button, modifiers));
	PintaCore.Tools.DoMouseMove (doc, Mouse (doc, end, button, modifiers));
	PintaCore.Tools.DoMouseUp (doc, Mouse (doc, end, button, modifiers));
}
var doc = NewDocument ();
var original = doc.Layers.CurrentUserLayer.Surface.GetReadOnlyPixelData ().ToArray ();
int history = doc.History.Items.Count ();
Drag (doc, new (40, 45), new (40, 45));
Check (doc.History.Items.Count () == history && !doc.Layers.ShowSelectionLayer, "A click does not lift pixels or create history");
var gizmo = (TransformGizmoHandle) move.Handles.Single ();
Check (gizmo.Active && gizmo.Frame.IsValid, "Move tool exposes an active transform frame");
Drag (doc, new (18, 13), new (92, 3));
Check (doc.History.Items.Count () == history + 1, "One corner-corridor drag produces one undo item");
Near (doc.Selection.TransformBounds!.Value.XAxis, new (0, 60), "Corner corridor rotates the oriented box clockwise by 90 degrees");
Near (doc.Layers.SelectionLayer.Transform.TransformPoint (new PointD (40.5, 30.5)), new (74.5, 25.5), "Pixel transform matches the selection frame");
doc.FinishSelection ();
var result = doc.Layers.CurrentUserLayer.Surface.GetReadOnlyPixelData ();
bool pixelsMatch = true; int mismatches = 0;
for (int y = 25; y < 65; y++) for (int x = 30; x < 90; x++) {
	var actual = result[(x - 15) * 120 + 104 - y]; var expected = original[y * 120 + x];
	if (actual != expected) { pixelsMatch = false; if (mismatches++ < 6) Console.WriteLine ($"Pixel diff source {x},{y}: expected {expected}, got {actual}"); }
}
Check (pixelsMatch, "Committed 90-degree rotation preserves every source pixel exactly");
doc.History.Undo (); doc.History.Undo ();
Check (doc.Layers.CurrentUserLayer.Surface.GetReadOnlyPixelData ().ToArray ().SequenceEqual (original), "Undo restores the original pixels");
Near (doc.Selection.TransformBounds!.Value.XAxis, new (60, 0), "Undo restores handle orientation");
doc.History.Redo (); doc.History.Redo ();
Near (doc.Selection.TransformBounds!.Value.XAxis, new (0, 60), "Redo restores handle orientation");

doc = NewDocument ();
original = doc.Layers.CurrentUserLayer.Surface.GetReadOnlyPixelData ().ToArray (); history = doc.History.Items.Count ();
Drag (doc, new (60, 45), new (15, 10));
Near (doc.Selection.RotationPivot!.Value, new (15, 10), "Dragging the center cross moves the pivot");
Check (doc.History.Items.Count () == history && doc.Layers.CurrentUserLayer.Surface.GetReadOnlyPixelData ().ToArray ().SequenceEqual (original), "Pivot placement changes no pixels or image history");
Drag (doc, new (28, 10), new (15, 23));
Near (doc.Selection.RotationPivot!.Value, new (15, 10), "Center-ring rotation keeps the relocated pivot fixed");
Near (doc.Layers.SelectionLayer.Transform.TransformPoint (new PointD (20, 10)), new (15, 15), "Dragging the center ring rotates pixels around the custom pivot");
// Resize after rotation: its opposite corner must stay fixed, and pixels/frame must agree.
var before = doc.Selection.TransformBounds!.Value;
var anchor = before.Origin;
var oldTransform = doc.Layers.SelectionLayer.Transform.Clone ();
Drag (doc, before.At (1, 1), before.At (1.5, 1.5));
Near (doc.Selection.TransformBounds!.Value.Origin, anchor, "Resize after rotation preserves the opposite corner");
var expectedDelta = before.Resize (TransformPart.SouthEast, before.At (1, 1), before.At (1.5, 1.5), false, false);
Near (doc.Layers.SelectionLayer.Transform.TransformPoint (new PointD (40, 30)), expectedDelta.TransformPoint (oldTransform.TransformPoint (new PointD (40, 30))), "Consecutive transform composition matches the geometry");

doc = NewDocument (false);
Drag (doc, new (-12, -12), new (132, -12));
Check (doc.Selection.TransformBounds is not null && doc.Layers.ShowSelectionLayer, "Full-image corner rotation works from outside the canvas without a prior selection");
doc = NewDocument ();
PintaCore.Tools.SetCurrentTool (outline); original = doc.Layers.CurrentUserLayer.Surface.GetReadOnlyPixelData ().ToArray ();
Drag (doc, new (73, 45), new (60, 58));
Check (doc.Layers.CurrentUserLayer.Surface.GetReadOnlyPixelData ().ToArray ().SequenceEqual (original), "Move Selection rotates only the outline");
Near (doc.Selection.TransformBounds!.Value.XAxis, new (0, 60), "Outline tool has the same center-ring rotation");

doc = NewDocument ();
Drag (doc, new (90, 45), new (60 + 30 * Math.Cos (38 * Math.PI / 180), 45 + 30 * Math.Sin (38 * Math.PI / 180)), MouseButton.Right, Gdk.ModifierType.ShiftMask);
var axis = doc.Selection.TransformBounds!.Value.XAxis;
Check (Math.Abs (Math.Atan2 (axis.Y, axis.X) * 180 / Math.PI - 45) < 1e-8, "Right-button rotation and Shift snap to 45° from a 38° drag");
doc = NewDocument ();
var primary = SystemManager.GetOperatingSystem () == OS.Mac ? Gdk.ModifierType.MetaMask : Gdk.ModifierType.ControlMask;
Drag (doc, new (40, 45), new (30, 45), MouseButton.Left, primary);
Check (doc.Selection.TransformBounds!.Value.IsValid, "Legacy primary-key scaling on the horizontal centerline remains finite");
Near (doc.Selection.TransformBounds.Value.XAxis, new (90, 0), "Legacy scaling still changes width");
doc = NewDocument ();
PintaCore.Tools.DoKeyDown (doc, new ToolKeyEventArgs { Key = new Gdk.Key (Gdk.Constants.KEY_Right) });
PintaCore.Tools.DoKeyUp (doc, new ToolKeyEventArgs { Key = new Gdk.Key (Gdk.Constants.KEY_Right) });
Near (doc.Selection.TransformBounds!.Value.Origin, new (31, 25), "Arrow-key nudging still moves exactly one pixel");
doc = NewDocument ();
Drag (doc, new (60, 45), new (-20, -30));
Drag (doc, new (-7, -30), new (-20, -17));
Near (doc.Selection.RotationPivot!.Value, new (-20, -30), "Rotation dial works with an off-canvas pivot");
doc = NewDocument ();
PintaCore.Tools.DoMouseDown (doc, Mouse (doc, new (73, 45)));
PintaCore.Tools.DoMouseUp (doc, Mouse (doc, new (60, 58)));
Near (doc.Selection.TransformBounds!.Value.XAxis, new (0, 60), "Fast drags retain the final mouse-up position");
doc = NewDocument ();
PintaCore.Tools.DoMouseDown (doc, Mouse (doc, new (73, 45)));
PintaCore.Tools.DoMouseMove (doc, Mouse (doc, new (60, 58)));
PintaCore.Tools.SetCurrentTool (outline);
Check (!doc.Layers.ShowSelectionLayer && doc.Layers.CurrentUserLayer.Surface.GetReadOnlyPixelData ().ToArray ().Any (p => p.A > 0), "Switching tools during a drag commits the visible pixels safely");

doc = NewDocument ();
original = doc.Layers.CurrentUserLayer.Surface.GetReadOnlyPixelData ().ToArray ();
PintaCore.Tools.DoMouseDown (doc, Mouse (doc, new (73, 45)));
for (int i = 1; i <= 120; i++) {
	double angle = i * Math.PI / 240;
	PintaCore.Tools.DoMouseMove (doc, Mouse (doc, new (60 + 13 * Math.Cos (angle), 45 + 13 * Math.Sin (angle))));
}
PintaCore.Tools.DoMouseUp (doc, Mouse (doc, new (60, 58)));
Check (doc.Layers.SelectionLayer.Surface.GetReadOnlyPixelData ().ToArray ().SequenceEqual (original), "Live rotation always samples the original pixels without cumulative resampling");

doc = NewDocument ();
foreach (double scale in new[] { .5, 1, 4 }) {
	doc.Workspace.Scale = scale;
	gizmo = (TransformGizmoHandle) move.Handles.Single ();
	var origin = doc.Workspace.CanvasPointToView (gizmo.Frame.Origin);
	Check (gizmo.HitTest (origin + new PointD (-12, -12)) == TransformPart.Rotate, $"Corner rotation hit area stays constant at zoom {scale}");
}
doc.Workspace.Scale = 1;
var snapshot = Gtk.Snapshot.New ();
gizmo = (TransformGizmoHandle) move.Handles.Single (); gizmo.Draw (snapshot);
var node = snapshot.ToNode ();
Check (node is not null, "Native GSK renderer builds the visible transform controls");
Directory.CreateDirectory ("artifacts/verification/transforms");
using (var preview = CairoExtensions.CreateImageSurface (Format.Argb32, 720, 480)) {
	using var cr = new Context (preview);
	cr.SetSourceRgb (.75, .76, .78); cr.Paint ();
	cr.Scale (4, 4); cr.Translate (30, 10);
	cr.SetSourceRgb (1, 1, 1); cr.Rectangle (0, 0, 120, 100); cr.Fill ();
	cr.SetSourceSurface (doc.Layers.CurrentUserLayer.Surface, 0, 0); cr.Paint ();
	node!.Draw (cr);
	using var png = preview.ToPixbuf (); png.Savev ("artifacts/verification/transforms/gizmo.png", "png", [], []);
}
Console.WriteLine ($"All {checks} transform integration checks passed.");
root.Destroy ();
