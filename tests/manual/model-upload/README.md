# Manual test fixtures — model upload

- `valid-cube.obj`: valid 10 mm watertight cube; upload must succeed.
- `invalid-content.stl`: renamed/plain-text content; upload must fail with HTTP 415 and must not consume quota.

To exercise quota quickly, set `MODEL_UPLOAD_MAX_MODELS=2` in the root `.env`, recreate the API container,
then upload two valid files. The third upload must fail with `quota_exceeded`. Restore the value to `20`
and recreate the API after the test.
