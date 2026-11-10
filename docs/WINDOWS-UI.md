# Klassieke Windows-presentatie

De aangeleverde Paint.NET-mockup is de visuele referentie voor de hoofdinterface. Dit blijft de bestaande Pinta-editor; de canvasrenderer, tools, effectkernels, documentmodellen, undo en sneltoetsafhandeling zijn behouden.

## Architectuur en wijzigingen

| Onderdeel | Bestaande component | Presentatie |
| --- | --- | --- |
| Hoofdvenster | `WindowShell`, `Gtk.ApplicationWindow` | Blauwe compacte titelbalk met Windows-achtige bediening via `ClassicWindowChrome`; verplaatsen/dubbelklikken via `Gtk.WindowHandle`, vensteracties via GTK. Sluiten loopt door de bestaande afhandeling van niet-opgeslagen documenten. |
| Menu en opdrachten | `Gtk.PopoverMenuBar`, `ActionManager` | Menubalk op een eigen volledige rij, daaronder iconenwerkbalk met dezelfde opdrachtvolgorde op alle platforms. Layers toont bestaande laagcommando’s. |
| Afbeeldingstabbladen | `DockNotebook` | Kleinere voorbeelden naast de iconenwerkbalk, met dezelfde selectie-, sluiten- en gewijzigd-indicatoren. Horizontaal scrollbaar. |
| Toolinstellingen | Bestaande tool-toolbar | Eigen horizontale rij onder de opdrachten; horizontaal scrollbaar als een tool meer ruimte nodig heeft. |
| Tools | `ToolBoxWidget`, `DockItem` | Twee compacte kolommen, eigen kleurrijke vectoriconen en subtiele rechthoekige knopvlakken. |
| Colors | `ColorsPad`, `DockItem` | Blauwe titelbalk, overlappende kleurstalen en klokwijzergericht HSV-wiel. Wielcache op daadwerkelijke schermschaal; kleurkeuze, modifiers en extra velden blijven beschikbaar. |
| History en Layers | Bestaande lijstwidgets en `DockPanel` | Lichtgrijze vlakken, blauwe verloopbalken, smalle randen, subtiele schaduw en geselecteerde rijen met blauw verloop. Bestaande laagopties blijven in het menu achter de eigenschappenknop. |
| Werkruimte | Bestaande viewport en `PintaCanvas` | Neutraal grijs met centraal papier, een visuele outline en lichte schaduw zonder extra layoutborder of gewijzigde pixelcoördinaten. |
| Status | Bestaande status-toolbar | Toolhint, afbeeldingsafmetingen, coördinaten, selectie-informatie en rechthoekige blauwe zoomslider. |

`ThemePalette.ToCss()` bevat de stijlregels; `themes/aero-light.json` is het lichte preset. Nieuwe profielen beginnen licht. Bestaande expliciete thema- en kleurvoorkeuren blijven geldig; het donkere preset en custom kleuren zijn beschikbaar. De alternatieve compacte menumodus blijft bestaan.

De panelen behouden hun bestaande titelbalkdrag, dubbelklik om naar de hoek terug te keren, zichtbaarheidscommando’s en opgeslagen posities. Afmetingen zijn GTK-logische pixels; GTK schaalt tekst, SVG-iconen en titelbalkglyphs. De documentstrip en toolinstellingen kunnen horizontaal scrollen op smallere vensters. De vensterdecoratie wordt binnen de applicatie getekend; systeemeigen bestandsdialogen volgen nog het platform.

## Verificatie

- Solutionbuild en releasepublish zonder waarschuwingen of fouten.
- Core- en effecttests controleren de bestaande editorwerking.
- `tests/Pinta.UiSmoke` controleert licht/donker/custom CSS, titelupdates, Windows-knoppen, compacte titelbalk/slider, alle 60 effectdialogen, iconen, documenttabs en paneelgedrag. Ook uitgevoerd met `GDK_SCALE=2`.
- Interactieve visuele acceptatie van het volledige appvenster op macOS staat nog open; widgetchecks zijn geen pixelvergelijking met de referentie.

Start lokaal met `./scripts/run-macos.sh`. Een al geopende instantie blijft zijn eerder geladen interface gebruiken totdat je die opnieuw start.
