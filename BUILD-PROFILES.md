# Build Profiles

Status: NORMATIVE

`OmsiBuildProfile` is independent of `RuntimePlatform`. It owns OMSI executable
fingerprint, globals, forms, fields, methods, callsites, and native byte guards.
It never encodes host Windows-generation compatibility.

`Omsi23004_692EBFBF` accepts only these known LAA SHA-256 values:

- `692EBFBF2CD32FAB05A8B934E52C2BE14594E939882F3DBF2BA4E2B66CCC6243`
  (`ALTERNATE_LAA`, runtime-validated);
- `7DAB063D1F62E73B3A2C7A6AC1921D7EDF5E5DB0FBC731481D117EEC8DE7D759`
  (`STEAM_LAA`, statically reconciled; Beta runtime field validation pending).

No version string, executable name, file size, or generic LAA marker is an
acceptance criterion. Any other hash is rejected before native runtime staging.
The profile records `SetActualDateTime`, saved-situation dispatch, and the
vehicle primitives observed in static/upstream evidence. Recording a symbol is
not an authorization to call it: each native ABI wrapper requires its own
fingerprint guard and proven calling contract.

The Current map global is `0x00861588`. The former candidate at `0x00859D94`
is not an `OmsiMap` object for this profile and is rejected as a map binding.
The profile also catalogs the internal AI configuration slot, raw-entrypoint
diagnostic index slot, and internet-texture native routine used by existing
native code. These remain profile-scoped implementation details, not public
pointer or address APIs.

The profile also records the Current D3D candidate slot `0x008627D0` as
`D3DDevice`. Runtime session `3cbd7bb6-d73f-4853-adc8-1213200527d2` proved
that the borrowed slot candidate supports `QueryInterface<IDirect3DDevice9>`,
`TestCooperativeLevel` returns `S_OK`, and all D3D operations execute on the
OMSI primary UI/main/render-owner thread. OmsiLaunch retains one QI reference;
the borrowed slot itself is never released. This entry is not generalized to
another executable fingerprint.

The matching static caller for `TProgMan.MakeVehicle` references candidate
globals at `0x00859DEC`, `0x008591DC`, and `0x00858D28`. Their exact list/index
roles remain unresolved, so they are deliberately not named BuildProfile
symbols. They are not public pointers and do not constitute a completed
player-vehicle implementation.
