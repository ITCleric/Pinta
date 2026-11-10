using System.Collections.Generic;
using System.Linq;
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

[GObject.Subclass<Gtk.Grid>]
public sealed partial class ToolBoxWidget
{
	private ToolManager tools = null!; // NRT - set in factory method
					   // Stores the button corresponding to each tool.
	private readonly Dictionary<BaseTool, Gtk.ToggleButton> tool_buttons = new ();
	// Dummy ToggleButton to use for grouping together the tools' buttons.
	private readonly Gtk.ToggleButton toggle_group = Gtk.ToggleButton.New ();

	partial void Initialize ()
	{
		RowSpacing = 2;
		ColumnSpacing = 2;
	}

	public static ToolBoxWidget New (ToolManager tools)
	{
		ToolBoxWidget widget = NewWithProperties ([]);
		widget.Configure (tools);
		return widget;
	}

	private void Configure (ToolManager tools)
	{
		tools.ToolAdded += (_, e) => HandleToolAdded (e.Tool);
		tools.ToolRemoved += (_, e) => HandleToolRemoved (e.Tool);
		tools.ToolActivated += (_, e) => HandleToolActivated (e.Tool);

		this.tools = tools;
	}

	private static Gtk.ToggleButton CreateToolButton (BaseTool tool)
	{
		Gtk.ToggleButton button = Gtk.ToggleButton.New ();
		button.IconName = tool.Icon;
		button.Name = tool.Name;
		button.FocusOnClick = false;

		button.SetCssClasses ([Resources.Styles.ToolBoxButton, "editor-tile"]);

		string shortcutText = "";
		if (tool.ShortcutKey != Gdk.Key.Invalid) {
			string shortcutLabel = Translations.GetString ("Shortcut key");
			shortcutText = $"{shortcutLabel}: {tool.ShortcutKey.ToUpper ().Name ()}\n";
		}

		button.TooltipText = $"{tool.Name}\n{shortcutText}\n{tool.StatusBarText}";

		return button;
	}

	private void HandleToolAdded (BaseTool tool)
	{
		Gtk.ToggleButton toolButton = CreateToolButton (tool);
		toolButton.Group = toggle_group;
		toolButton.OnClicked += (_, _) => HandleToolButtonClicked (tool);
		tool_buttons[tool] = toolButton;

		foreach (var child in tool_buttons.Values)
			if (child.GetParent () is not null) Remove (child);
		int index = 0;
		foreach (var entry in tools) {
			Attach (tool_buttons[entry], index % 2, index / 2, 1, 1);
			index++;
		}
	}

	private void HandleToolButtonClicked (BaseTool tool)
	{
		tools.SetCurrentTool (tool);
	}

	/// <summary>
	/// If the tool was switched without clicking on the button (e.g. via shortcut key),
	/// ensure the tool's button is active. Note we don't need to deactivate the previous
	/// button since they're all in the same toggle button group.
	/// </summary>
	private void HandleToolActivated (BaseTool tool)
	{
		Gtk.ToggleButton toolButton = tool_buttons[tool];
		toolButton.Active = true;
	}

	private void HandleToolRemoved (BaseTool tool)
	{
		Gtk.ToggleButton toolButton = tool_buttons[tool];
		Remove (toolButton);
		tool_buttons.Remove (tool);
	}
}
