# Glosario es-LATAM (OmsiLaunch 0.1.0-beta3)

## Tratamiento y registro

- Español técnico latinoamericano neutro. Usar formas impersonales ("se ejecuta", "hay que indicar") y, cuando haga falta dirigirse al lector, "usted" implícito en tercera persona (imperativos: "ejecute", "consulte", "abra"). Nunca "tú"/"vosotros" ni formas de voseo.
- Vocabulario prohibido (España): ordenador, fichero, coger, vale, móvil, pinchar, guardar como "salvar", "vosotros". Usar: computadora (o "equipo"), archivo, tomar/obtener, hacer clic, "de acuerdo".
- Tono de documentación de referencia: preciso, sobrio, sin coloquialismos. No suavizar ni reforzar afirmaciones.

## Puntuación y tipografía

- Signos de apertura obligatorios: `¿…?`, `¡…!`.
- Comillas: comillas inglesas "…" (uso habitual en documentación técnica latinoamericana); no usar « ».
- Títulos y encabezados: mayúscula solo en la primera palabra y en nombres propios ("Ciclo de vida de la sesión", no "Ciclo De Vida De La Sesión").
- Números, unidades, versiones, hashes e ids quedan exactamente como en inglés (`64 KiB`, `250 ms`, `2 s`, `0.1.0-beta3`); no cambiar el punto decimal ni agregar separadores de miles.
- Fechas y formatos entre backticks no se tocan (`DD/MM/YYYY`).
- Raya/guion: mantener la puntuación del original; para incisos se puede usar coma o paréntesis.
- Nombres de producto, clases, estados y tokens en mayúsculas (`STABLE_BETA`, `PARTIAL`, `UNAVAILABLE`, `T01`, `BUG-05`, `RV-002`, `DOC-01`) nunca se traducen.
- La interfaz de Windows del producto NO está localizada al español: los literales de UI en inglés (`End session`, `Session is running`, `Status`, `Cancel`…) quedan en backticks y su significado se explica en la prosa ("el elemento de menú `End session` (finalizar sesión)"); nunca presentar una traducción como si fuera texto mostrado por el producto.
- Género de los anglicismos: el runtime, el plugin, el handle, el snapshot, el overlay, el journal, el timeout, el flag, el preset, el slot, el thread, el tooltip, el repaint, el hash, el build, el pipe.

## Términos

| English | es-LATAM | note |
| --- | --- | --- |
| session | sesión | |
| session owner / owner | propietario de la sesión / propietario | "El propietario" = el proceso de la CLI que posee la sesión. |
| client (mode) | (modo) cliente | |
| owner process | proceso propietario | |
| launch (noun / verb) | inicio / iniciar; "launch plan" = plan de inicio | "lanzar" solo si el contexto lo exige; preferir "iniciar". "Launcher" = lanzador. |
| plan (noun / verb) | plan / planificar | "planned mutations" = mutaciones planificadas; "re-plan" = volver a planificar. |
| runnable / not runnable | ejecutable / no ejecutable | Referido a un plan: "el plan no es ejecutable". Para el archivo .exe decir "archivo ejecutable". |
| start | iniciar / inicio | "startup" = arranque (p. ej., "timeout de arranque"). |
| stop | detener / detención | |
| end session | finalizar la sesión | El literal de UI `End session` queda en inglés. |
| canonical stop | detención canónica | "canonical stop path" = ruta de detención canónica. |
| restore | restaurar / restauración | "exact restore" = restauración exacta. |
| recovery | recuperación | |
| pending (journal) | (journal) pendiente | |
| journal | journal | Se mantiene en inglés (archivo `journal.json`); en la primera mención se puede aclarar "journal (registro de la transacción)". |
| transaction | transacción | |
| overlay | overlay | Se mantiene en inglés; plural "overlays". |
| backup | copia de seguridad | "backups" en plural = copias de seguridad; "backed up" = respaldado. |
| snapshot | snapshot | Se mantiene en inglés (masculino). En la ventana de estado: "snapshot de solo lectura". |
| installation | instalación | |
| installation root | raíz de la instalación | |
| package | paquete | |
| release package | paquete de release | "release" se mantiene en inglés (femenino: "la release") o "versión publicada" en prosa general. |
| manifest | manifiesto | Nombres de archivo como `release-manifest.json` no cambian. |
| permanent plugin | plugin permanente | |
| native bridge | puente nativo | |
| runtime | runtime | Se mantiene en inglés (masculino): "el runtime", "en runtime". |
| runtime operation | operación de runtime | |
| runtime command | comando de runtime | |
| runtime slot / mailbox | slot de runtime / buzón de runtime | "telemetry slot" = slot de telemetría; "latest-value slot" = slot de último valor. |
| control plane | plano de control | "local control plane" = plano de control local. |
| local control | control local | |
| named pipe | named pipe | Se mantiene en inglés; en la primera mención puede aclararse "(canalización con nombre)". "pipe stop" = detención por pipe. |
| frame (protocol) | trama | |
| envelope (JSON) | envoltorio (JSON) | |
| handle | handle | Se mantiene en inglés (masculino). |
| stale handle | handle obsoleto | |
| capability | capacidad | Ids como `camera.lock` no se traducen. |
| capability registry | registro de capacidades | |
| bounded list | lista acotada | |
| truncated | truncado/truncada | `truncated=true` queda literal; en prosa: "la lista se truncó". |
| row | fila | |
| evidence | evidencia | "runtime evidence" = evidencia de runtime. |
| runtime-validated | validado en runtime | "not runtime validated" = no validado en runtime (misma fuerza). |
| statically validated | validado estáticamente | |
| offline test | prueba offline | "offline only" = solo offline. Significa sin OMSI en ejecución, no "sin conexión a Internet". |
| gate (documentation gate) | gate (gate de documentación) | Se mantiene "gate"; puede aclararse "control de calidad de la documentación". |
| tray icon | ícono de la bandeja | Con tilde ("ícono"), uso latinoamericano. "tray" solo = bandeja del sistema. |
| notification area | área de notificación | Término de Windows en español. |
| status window | ventana de estado | |
| confirmation dialog | diálogo de confirmación | |
| message box | cuadro de mensaje | |
| failure dialog | diálogo de error | |
| tooltip | tooltip | Se mantiene en inglés; opcionalmente "(texto emergente)" en la primera mención. |
| context menu | menú contextual | |
| Explorer restart | reinicio del Explorador de Windows | "Explorer (the shell)" = el Explorador (el shell). `explorer.exe` literal. |
| entry point | punto de entrada | |
| new map | mapa nuevo | Token `NEW_MAP` sin traducir. |
| saved situation | situación guardada | Token `SAVED_SITUATION` sin traducir. |
| map | mapa | |
| splash screen | pantalla de presentación (splash) | "splash" solo puede quedar en inglés en contexto técnico ("assets de splash"). |
| Internet Textures | Internet Textures | Nombre de la función de OMSI; se mantiene en inglés. En prosa genérica: "texturas de Internet". |
| session profile | perfil de sesión | |
| preset | preset | Se mantiene en inglés (masculino). |
| setting | ajuste | "configuration" = configuración; "semantic setting" = ajuste semántico. |
| player vehicle | vehículo del jugador | |
| road vehicle | vehículo de tráfico (`RoadVehicle`) | Vehículo de IA en la calle. |
| human (pedestrian/passenger object) | humano (objeto `Human`: peatón o pasajero) | |
| timetable | horario | |
| track entry | entrada de trayecto | |
| tour entry | entrada de servicio (tour) | |
| ticket | boleto | Uso latinoamericano (no "billete"). |
| driver | conductor | |
| fleet number | número de flota | |
| registration (plate) | placa (matrícula) | Preferir "placa"; "matrícula" como aclaración. |
| repaint | repaint | Se mantiene en inglés (masculino). |
| spawn | generar / generación (spawn) | "spawned vehicle" = vehículo generado. |
| camera lock | bloqueo de cámara | |
| device reset | reinicio del dispositivo (D3D) | |
| render thread | thread de renderizado | "thread" se mantiene en inglés; "UI thread" = thread de la interfaz. |
| texture | textura | |
| exit code | código de salida | |
| error code | código de error | |
| diagnostic | diagnóstico | |
| diagnostics directory | directorio de diagnósticos | |
| timeout | timeout | Se mantiene en inglés (masculino); "startup timeout" = timeout de arranque. |
| placeholder | marcador de posición | |
| flag | flag | Se mantiene en inglés (opción de línea de comandos), masculino. |
| route | ruta | |
| command word | palabra de comando | |
| integrator | integrador | |
| caller | llamador | "caller thread" = thread del llamador. |
| known limitation | limitación conocida | |
| accepted risk | riesgo aceptado | |
| stable beta | beta estable | Token `STABLE_BETA` sin traducir. |
| experimental | experimental | Token `EXPERIMENTAL` sin traducir. |
| partial | parcial | Token `PARTIAL` sin traducir. |
| unavailable | no disponible | Token `UNAVAILABLE` sin traducir. |
| deprecated / legacy | obsoleto / heredado | |
| not normative | no normativo | |
| source of truth | fuente de verdad | |
| lease (installation lease) | lease (lease de la instalación) | Se mantiene "lease"; puede aclararse "bloqueo exclusivo". |
| handoff | handoff | Se mantiene en inglés. |
| supervisor | supervisor | |
| accepted for compatibility / no effect | aceptado por compatibilidad / sin efecto actualmente | El token `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT` sin traducir. |
