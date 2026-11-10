using NUnit.Framework;

namespace Pinta.Core.Tests;

[TestFixture]
internal sealed class FloatingPalettePositionTests
{
	[TestCase (-20, -30, 1000, 700, 220, 200, 0, 0)]
	[TestCase (990, 690, 1000, 700, 220, 200, 780, 500)]
	[TestCase (40, 90, 1000, 700, 220, 200, 40, 90)]
	[TestCase (400, 400, 180, 160, 220, 200, 0, 0)]
	[TestCase (70, 70, 0, 0, 220, 200, 0, 0)]
	[TestCase (5000, 5000, 640, 480, 208, 210, 432, 270)]
	public void KeepsDraggedAndRestoredPalettesReachable (double x, double y, int width, int height, int panelWidth, int panelHeight, int expectedX, int expectedY)
	{
		Assert.That (FloatingPalettePosition.Clamp (x, y, width, height, panelWidth, panelHeight), Is.EqualTo ((expectedX, expectedY)));
	}
}
