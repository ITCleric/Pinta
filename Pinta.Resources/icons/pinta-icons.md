## The icons at the following paths were created by Pinta contributors, and they can be used with the same license as the project itself:

- hicolor/16x16/actions/effects-color-dithering.png
- hicolor/16x16/actions/effects-render-cells.png
- hicolor/16x16/actions/effects-render-voronoidiagram.png
- hicolor/scalable/actions/join-bevel-symbolic.svg
- hicolor/scalable/actions/join-miter-symbolic.svg
- hicolor/scalable/actions/join-round-symbolic.svg
- hicolor/scalable/actions/tool-palette.svg
- hicolor/scalable/actions/tool-select-lasso-freeform-symbolic.svg
- hicolor/scalable/actions/tool-select-lasso-polygon-symbolic.svg
- hicolor/scalable/actions/text-bold-symbolic.svg
- hicolor/scalable/actions/text-extra-bold-symbolic.svg
- hicolor/scalable/actions/text-extra-light-symbolic.svg
- hicolor/scalable/actions/text-light-symbolic.svg
- hicolor/scalable/actions/text-normal-symbolic.svg
- hicolor/scalable/actions/text-variant-*.svg

## Original desktop tool set (Pinta Modern)

The 22 `hicolor/scalable/actions/pinta-tool-*.svg` icons are independently drawn for this fork under the project's MIT license. Rebuild them with `scripts/generate-tool-icons.py`. The previous symbolic assets remain available for compatibility; cursor artwork is unchanged.

## Original Studio & Play set (Pinta Modern)

The 60 `hicolor/scalable/actions/pinta-studio-*.svg` files and their separate `hicolor/64x64/actions/pinta-studio-*.png` exports are independently drawn for this fork under the project's MIT license. They extend the square colored desktop icon family. Regenerate them with `scripts/generate-studio-icons.py`; the generator reads the canonical catalog in `Pinta.Effects/Studio/catalog.json` and also builds `docs/assets/studio-spritesheet.{svg,png,json}`. No third-party plugin artwork is included.

## Original application identity (Pinta Modern)

`hicolor/scalable/apps/io.github.itcleric.pintamodern.svg` is an original framed landscape and brush icon under the project's MIT license. `scripts/generate-app-icon.py` exports matching GTK PNGs, `Pinta/Pinta.ico`, and `installer/macos/pinta-modern.icns` with 16px through 1024px representations. The fork has its own application and bundle identifier, separate from upstream Pinta.
