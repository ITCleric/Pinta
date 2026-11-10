// 
// DocumentViewContent.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2015 Jonathan Pobst
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
using Cairo;
using Pinta.Core;
using Pinta.Docking;

namespace Pinta;

internal sealed class DocumentViewContent : IDockNotebookPreview, IDisposable
{
	private readonly CanvasWindow canvas_window;
	private ImageSurface? preview_surface;
	public event EventHandler? PreviewChanged;

	public Document Document { get; }

	public DocumentViewContent (Document document, CanvasWindow canvasWindow)
	{
		Document = document;
		canvas_window = canvasWindow;

		document.IsDirtyChanged += OnLabelChanged;
		document.Renamed += OnLabelChanged;
		document.History.HistoryItemAdded += InvalidatePreview;
		document.History.ActionUndone += InvalidatePreview;
		document.History.ActionRedone += InvalidatePreview;
		document.Layers.LayerPropertyChanged += InvalidatePreview;
	}

	private void OnLabelChanged (object? sender, EventArgs args) => LabelChanged?.Invoke (this, EventArgs.Empty);

	private void InvalidatePreview (object? sender, EventArgs args)
	{
		preview_surface?.Dispose (); preview_surface = null;
		PreviewChanged?.Invoke (this, EventArgs.Empty);
	}

	public void DrawPreview (Context context, int width, int height)
	{
		if (preview_surface is null || preview_surface.Width != width || preview_surface.Height != height) {
			preview_surface?.Dispose ();
			preview_surface = CairoExtensions.CreateImageSurface (Format.Argb32, width, height);
			using var cr = new Context (preview_surface);
			double scale = Math.Min (width / (double) Document.ImageSize.Width, height / (double) Document.ImageSize.Height);
			cr.Translate ((width - Document.ImageSize.Width * scale) / 2, (height - Document.ImageSize.Height * scale) / 2);
			cr.Scale (scale, scale);
			foreach (var layer in Document.Layers.GetLayersToPaint ()) {
				cr.SetBlendMode (layer.BlendMode);
				cr.SetSourceSurface (layer.Surface, 0, 0);
				cr.PaintWithAlpha (layer.Opacity);
			}
		}
		context.SetSourceSurface (preview_surface, 0, 0);
		context.Paint ();
	}

	public void Dispose ()
	{
		Document.IsDirtyChanged -= OnLabelChanged;
		Document.Renamed -= OnLabelChanged;
		Document.History.HistoryItemAdded -= InvalidatePreview;
		Document.History.ActionUndone -= InvalidatePreview;
		Document.History.ActionRedone -= InvalidatePreview;
		Document.Layers.LayerPropertyChanged -= InvalidatePreview;
		preview_surface?.Dispose (); preview_surface = null;
	}

	public event EventHandler? LabelChanged;

	public string Label => Document.DisplayName + (Document.IsDirty ? "*" : string.Empty);

	public Gtk.Widget Widget => canvas_window;
}
