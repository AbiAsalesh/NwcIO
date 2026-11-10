# Private Test Fixtures & Clean-Room Policy

Some integration tests require sample Navisworks files (`.nwc`, `.nwd`, `.nwf`) that cannot be published due to intellectual property and confidentiality considerations. Those tests locate their input through an abstracted fixture resolver in the test suite and are reported as skipped when private fixtures are not present.

The repository contains **no proprietary models, customer files, or binary Navisworks caches**.

## Where Fixtures Are Looked For

1. The path or folder specified by the `NWCIO_TEST_FILES` environment variable.
2. An optional `test_files` folder in the repository root (or in any parent directory of the test binary).

```
NwcIO/
├── src/
├── tests/
└── test_files/               <-- Uncommitted, ignored by git
    ├── Sample1.nwc
    └── Sample2.nwc
```

If neither is set, tests that rely on external binary models skip cleanly with zero failure. All core unit tests run on synthetic streams and procedurally generated geometry.

## Running Tests with Private Fixtures

PowerShell:
```powershell
$env:NWCIO_TEST_FILES = "C:\path\to\fixtures"
dotnet test -c Release
```

Bash / Linux:
```bash
NWCIO_TEST_FILES=/path/to/fixtures dotnet test -c Release
```

## Temporary Diagnostic Dumps

Tests that generate output (such as IFC, glTF, OBJ, or BCF files) write exclusively to the system temporary directory (`Path.GetTempPath()`) with unique GUID suffixes, and clean up their files in `finally` blocks. No fixed absolute paths or local directory paths are hardcoded.
