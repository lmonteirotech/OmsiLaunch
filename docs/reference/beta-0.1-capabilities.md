# OmsiLaunch Beta 0.1 Capability Catalog

> Superseded by [Capabilities Reference](capabilities.md). This page is kept
> only so that existing links keep resolving; it is not normative.

Summary of the current behaviour, documented in full on the new page:

- The catalog is generated from `PublicCapabilityRegistry.All`; each entry has
  a classification (`PublicStableBeta`, `PublicExperimental`, `InternalOnly`,
  `Unsupported`), a kind, an API route, a CLI route and a validation state.
- Only operations in `PublicCapabilityRegistry.PublicRuntimeOperationIds` cross
  the public boundary; `internal.*` and unknown operations are rejected with
  `OL_E_RUNTIME_OPERATION_UNKNOWN` before any session state is consulted.
- `weather.set` is `UNAVAILABLE` (rejected with
  `OL_E_RUNTIME_SETTING_NOT_PERSISTENT`); `map.read` is `STABLE_BETA` and
  `camera.lock` is `EXPERIMENTAL`, both runtime-validated; see the
  [runtime validation status](../status/runtime-validation-status.md).
