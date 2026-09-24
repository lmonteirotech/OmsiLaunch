# Glosario es-ES — documentación de OmsiLaunch 0.1.0-beta3

Glosario obligatorio para todas las páginas traducidas a `es-ES`. La página inglesa es la única fuente de verdad; este glosario solo fija la terminología y el estilo. No existe traducción antigua para `es-ES`, así que la terminología se establece aquí.

## Tratamiento, registro y estilo

- **Registro**: documentación técnica profesional en español de España. Se prefieren las construcciones impersonales (“se inicia la sesión”, “hay que ejecutar…”); cuando sea necesario dirigirse al lector, se usa **“tú”** de forma coherente (“ejecuta”, “consulta”), nunca “usted” ni “vosotros”.
- **Variantes de España**: `ordenador` (no “computadora”), **archivo** (no “fichero”; Windows en es-ES usa “archivo”), **directorio** para rutas técnicas y **carpeta** solo al describir lo que ve el usuario en el Explorador, **configuración/ajuste**, **guardar**, **pantalla**, **predeterminado** (no “por defecto” salvo en citas), **iniciar sesión**, **eliminar/quitar** (no “borrar” para archivos del sistema de archivos cuando hay matiz técnico).
- **Mayúsculas**: títulos y encabezados en mayúscula solo en la primera palabra y en nombres propios (“Ciclo de vida de la sesión”, no “Ciclo De Vida De La Sesión”). Los nombres de producto (OmsiLaunch, OMSI, Windows, .NET, Steam, Explorer/Explorador) conservan su forma.
- **Puntuación**: signos de apertura `¿` y `¡` obligatorios. Comillas en prosa: comillas angulares « » (y “ ” dentro de ellas); los literales de UI y los identificadores van siempre en su span de código original, sin comillas añadidas. Coma decimal solo en números propios de la prosa española; los números, unidades y valores que proceden del original (`64 KiB`, `250 ms`, `2 s`, `0.1`, versiones, hashes) se copian **sin cambios**. Abreviaturas: “p. ej.”, “etc.”. Sin punto final en celdas de tabla que en inglés no lo llevan; con punto si el inglés lo lleva.
- **Género de anglicismos**: el runtime, el handle, el overlay, el snapshot, el flag, el timeout, el plugin, el hash, el thread, el shim, el host, el envelope, el lease, el handoff, el slot, el preset, el repaint, el tooltip (todos masculinos). Plural con -s: handles, overlays, flags, timeouts.
- **Literales**: todo lo que va entre acentos graves, los bloques de código, los tokens en mayúsculas (`STABLE_BETA`, `PARTIAL`, `UNAVAILABLE`, `RUNTIME_PASS`, `NEW_MAP`, `SAVED_SITUATION`, `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`…) y los identificadores de evidencia (`T01`, `CAM01`, `RV-002`, `BUG-05`, `DOC-01`, `S-18`…) no se traducen nunca.
- **UI de Windows**: el producto no tiene cadenas en español; en Windows en español se muestran las cadenas inglesas. Los literales de UI (`End session`, `Status`, `Session is running`, `OmsiLaunch is running`, `Cancel`, `Close`…) se mantienen en inglés y su significado se explica en la prosa (p. ej. «el elemento de menú `End session` (finalizar la sesión)»). Nunca se presenta una etiqueta traducida como si el producto la mostrara.

## Terminología

| English | es-ES | note |
| --- | --- | --- |
| session | sesión | |
| session owner / owner | propietario de la sesión / propietario | “owner mode” = modo propietario |
| client (mode) | cliente (modo cliente) | |
| owner process | proceso propietario | |
| launch (noun / verb) | lanzamiento / lanzar | “launch flags” = flags de lanzamiento; “a launch fails” = el lanzamiento falla |
| plan (noun / verb) | plan / planificar | planning = planificación; re-plan = volver a planificar |
| runnable / not runnable | ejecutable / no ejecutable | dicho del plan (“el plan no es ejecutable”); el archivo `.exe` es «el ejecutable» o «archivo ejecutable» |
| start (noun / verb) | inicio / iniciar | start phase = fase de inicio; startup = arranque (startup timeout = timeout de arranque) |
| stop (noun / verb) | detención / detener | stop request = solicitud de detención; no usar “parada” |
| end session | finalizar la sesión | el literal de UI `End session` se mantiene en inglés |
| canonical stop | detención canónica | la ruta única de detención del propietario |
| restore (noun / verb) | restauración / restaurar | “exact restore” = restauración exacta |
| recovery / recover | recuperación / recuperar | |
| pending (journal) | (diario) pendiente | |
| journal | diario | diario de la transacción; el archivo `journal.json` no se traduce |
| transaction | transacción | |
| overlay | overlay | se mantiene en inglés (masculino); no “superposición” |
| backup | copia de seguridad | plural “copias de seguridad”; `backup\<session>` no se traduce |
| snapshot | snapshot | se mantiene en inglés (masculino); también para «la ventana de estado es un snapshot» |
| installation | instalación | |
| installation root | raíz de la instalación | “OMSI root” = raíz de OMSI |
| package | paquete | |
| release package | paquete de la versión | ZIP de distribución publicado |
| manifest | manifiesto | `release-manifest.json` no se traduce |
| permanent plugin | plugin permanente | “plugin closure” = conjunto de archivos del plugin |
| native bridge | puente nativo | |
| runtime | runtime | se mantiene en inglés (masculino); “.NET runtime” = runtime de .NET |
| runtime operation | operación de runtime | |
| runtime command | comando de runtime | “runtime command channel” = canal de comandos de runtime |
| runtime slot / mailbox | slot de runtime / buzón de runtime | “telemetry slot” = slot de telemetría; “latest-value slot” = slot de último valor |
| control plane | plano de control | |
| local control | control local | |
| named pipe | canalización con nombre | término de Windows en es-ES; “pipe” suelto = canalización; “pipe stop” = detención por la canalización |
| frame (protocol) | trama | |
| envelope (JSON) | envelope | se mantiene en inglés (masculino): «el envelope `version`» |
| handle | handle | se mantiene en inglés (masculino) |
| stale handle | handle obsoleto | |
| capability | capacidad | |
| capability registry | registro de capacidades | `PublicCapabilityRegistry` no se traduce |
| bounded list | lista acotada | |
| truncated | truncado / truncada | «la lista está truncada»; `truncated=true` no se traduce |
| row | fila | |
| evidence | evidencia | «evidencia de runtime»; los ids (`T01`…) no cambian |
| runtime-validated | validado en runtime | `RUNTIME_VALIDATED` no se traduce; “not runtime validated” = no validado en runtime |
| statically validated | validado estáticamente | `STATICALLY_VALIDATED` no se traduce |
| offline test | prueba offline | “offline only” = solo offline (sin ejecutar OMSI) |
| gate (documentation gate) | control de documentación | “the gate fails the build” = el control hace fallar la compilación |
| tray icon | icono de la bandeja | “tray” = bandeja; “tray indicator” = indicador de la bandeja |
| notification area | área de notificación | término de Windows en es-ES |
| status window | ventana de estado | |
| confirmation dialog | cuadro de diálogo de confirmación | |
| message box | cuadro de mensaje | |
| failure dialog | cuadro de diálogo de error | |
| tooltip | tooltip | se mantiene en inglés (masculino); en Windows es-ES equivale a «información sobre herramientas» |
| context menu | menú contextual | “menu item” = elemento de menú |
| Explorer restart | reinicio del Explorador | Explorador de Windows; `explorer.exe` no se traduce |
| entry point | punto de entrada | “entrypoint index” = índice del punto de entrada |
| new map | mapa nuevo | `NEW_MAP` no se traduce; “New session” (valor de UI) se mantiene como literal |
| saved situation | situación guardada | `SAVED_SITUATION` no se traduce |
| map | mapa | |
| splash screen | pantalla de presentación (splash) | “managed splash” = splash gestionado |
| Internet Textures | texturas de Internet | |
| session profile | perfil de sesión | |
| preset | preset | se mantiene en inglés (masculino); “weather preset” = preset meteorológico |
| setting | ajuste | “semantic setting” = ajuste semántico; “settings” en general = configuración |
| player vehicle | vehículo del jugador | |
| road vehicle | vehículo de carretera | `RoadVehicle` no se traduce |
| human (pedestrian/passenger object) | humano (objeto peatón/pasajero) | `Human` no se traduce |
| timetable | horario | |
| track entry | entrada de trayecto | |
| tour entry | entrada de tour | “tour” se mantiene (término de OMSI) |
| ticket | billete | |
| driver | conductor | “driver profile” = perfil de conductor |
| fleet number | número de flota | |
| registration (plate) | matrícula | |
| repaint | repaint | se mantiene (término de la comunidad OMSI, masculino) |
| spawn | generar | “spawned vehicles” = vehículos generados |
| camera lock | bloqueo de cámara | `camera.lock` no se traduce |
| device reset | reset del dispositivo | D3D; “lost device” = dispositivo perdido |
| render thread | thread de renderizado | “UI thread” = thread de la UI |
| texture | textura | |
| exit code | código de salida | |
| error code | código de error | |
| diagnostic | diagnóstico | |
| diagnostics directory | directorio de diagnósticos | |
| timeout | timeout | se mantiene en inglés (masculino) |
| placeholder | marcador de posición | |
| flag | flag | se mantiene en inglés (masculino): «el flag `/silent`» |
| route | ruta | “hierarchical route” = ruta jerárquica; una ruta de archivo se llama «ruta de archivo» cuando pueda confundirse |
| command word | palabra de comando | |
| integrator | integrador | |
| caller | llamador | «el código llamador» cuando convenga |
| known limitation | limitación conocida | |
| accepted risk | riesgo aceptado | |
| stable beta | beta estable | `STABLE_BETA` no se traduce |
| experimental | experimental | `EXPERIMENTAL` no se traduce |
| partial | parcial | `PARTIAL` no se traduce |
| unavailable | no disponible | `UNAVAILABLE` no se traduce; “not implemented” = no implementado |
| deprecated / legacy | obsoleto / heredado | |
| not normative | no normativo | “normative” = normativo |
| source of truth | fuente de verdad | |

## Términos adicionales

| English | es-ES | note |
| --- | --- | --- |
| installation lease | lease de la instalación | se mantiene “lease” (masculino) |
| handoff | handoff | se mantiene (masculino) |
| host / shim / bootstrapper | host / shim / bootstrapper | se mantienen |
| supervisor | supervisor | |
| session deletion | eliminación de sesión | |
| gameplay | fase de juego | “reach gameplay” = llegar a la fase de juego |
| telemetry | telemetría | |
| forced termination | terminación forzada | |
| cooperative shutdown | cierre cooperativo | |
| logoff | cierre de sesión de Windows | para no confundir con la sesión de OmsiLaunch |
| build (OMSI build / software build) | build / compilación | build de OMSI = build; build del software = compilación |
| hash | hash | |
| release | versión / publicación | |
