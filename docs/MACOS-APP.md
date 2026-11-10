# Pinta Modern als macOS-app

De fork heeft een eigen app-identiteit: `io.github.itcleric.pintamodern`. De native app heet **Pinta Modern**. De oorspronkelijke canvas-en-penseeltekening wordt gebruikt voor de titelbalk, GTK-appiconen, Windows ICO en het Finder/Dock-icoon. De macOS ICNS bevat standaard- en Retina-representaties tot 1024 pixels.

## Bouwen en installeren

Vereist bij het bouwen: macOS, .NET 10 (of de lokale `.dotnet` SDK), Python 3, Xcode Command Line Tools, Homebrew GTK/libadwaita en de in het bestaande `bundle_gtk.py` genoemde native bibliotheken. De geïnstalleerde app heeft deze ontwikkelomgeving niet nodig: .NET en de GTK-bibliotheken worden meegeleverd.

```sh
python3 scripts/generate-app-icon.py
python3 installer/macos/build_modern_app.py --install
open -a '/Applications/Pinta Modern.app'
```

Zonder `--install` staat het resultaat in `artifacts/macos/Pinta Modern.app`. Op Apple Silicon gebruikt de builder `osx-arm64`; op Intel `osx-x64`. Voor hergebruik van een bestaande self-contained publish:

```sh
python3 installer/macos/build_modern_app.py --runtime osx-arm64 --publish-dir artifacts/macos-publish --install
```

De builder gebruikt de bestaande GTK-dependencycollector, verplaatst iconen/vertalingen naar `Contents/Resources/share`, configureert native library-rpaths, ondertekent lokaal ad-hoc en controleert de handtekening. `ditto` bewaart de extended-attribute-handtekeningen van managed .NET-assemblies bij installatie. Alleen een bestaande app met dezelfde bundle-ID wordt vervangen; de vorige bundel gaat naar `artifacts/macos-backups/`.

De bundel is bedoeld voor lokale installatie. Ad-hoc ondertekening is geen Developer ID-notarisatie voor distributie aan andere Macs. Een publiek uitgebrachte download vraagt een afzonderlijk Developer ID-signing- en notarisationproces.

Het originele icoon staat in `Pinta.Resources/icons/hicolor/scalable/apps/io.github.itcleric.pintamodern.svg`; het is zelfstandig getekend onder de MIT-licentie van deze fork. De oude upstream-appidentiteit en het oude macOS-installerpad blijven beschikbaar voor upstream-verwijzingen.
