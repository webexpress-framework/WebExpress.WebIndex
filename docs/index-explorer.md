# Index-Explorer

## Workspace

The Windows application keeps its existing application identity and provides a dark desktop workspace with a project tree, WQL editor, result tabs and status bar. Open a `.ws` schema beside its `.wds` document store or use **Sample** to create a separate demonstration index. Choose the language used when the source index was created before opening it.

## Projects and durable links

The project tree groups index references and pinned document identities. Create a project with **+ Project**, use **Open / link** to add an index to the active project, and choose **Link to project** for a selected document. Clicking a project makes it the destination for subsequent links. A project can reference indexes in multiple directories.

The project settings persist in `%APPDATA%/WebExpress.WebIndex.WiUI/projects.xml`. Existing folder projects migrate by discovering their schema files once. Discovery never removes missing references. Removing a link does not delete index files, and a removed link is not silently rediscovered on the next startup.

The availability indicators distinguish missing targets from temporarily unavailable targets. Missing schema files, document stores and deleted pinned documents stay visible as disabled tree entries. Startup, refresh and relevant document mutations check availability. A locked or unreadable source is shown as unavailable rather than assumed deleted. The link menu remains usable to rename, remove or locate a moved index, including when the target is disabled. Restoring a source and refreshing enables its surviving references again.

The project properties screen provides name and optional discovery folder settings, index linking, path repair and removal. Changing the discovery folder adds its schemas while retaining existing references. Document labels can be renamed independently of the stored document content.

## Queries and tables

The WQL editor validates with the same parser used by the index engine. Contextual suggestions replace the identifier at the caret, preserve following text and avoid inserting inside quoted literals. Invalid queries leave the previous successful result displayed and identify the parser error. Query history and favorites are scoped to the index path and persist in the application data directory.

The result tabs show documents, schema fields and reverse terms with document postings. Flat column headers sort typed values, including numeric values. The result filter searches loaded cells, and pagination provides 25, 50 or 100 rows per page. Selection exposes full JSON details and clipboard copying. CSV and JSON exports include all filtered pages of the loaded view. CSV quotes delimiters and guards formula-like values.

The result limit is 10,000 documents or terms to bound desktop memory usage. The status bar explicitly identifies truncated results. Use WQL conditions, `skip` and `take` to browse other ranges. Header sorting and the result filter operate on loaded results, rather than executing another storage query. Pinned document lookup is independent of the result limit and opens a query for its immutable identifier.

## Document changes

The default connection reads an isolated snapshot and leaves source files unchanged. **Enable editing** reopens the original index with exclusive access after confirmation. Other applications must release the source index first. The editor supports creating, updating and deleting documents through the existing index manager, keeping reverse postings synchronized. **Finish editing** returns to snapshot inspection.

The document editor provides schema fields, explicit null controls and a JSON mode. It validates complete property coverage, duplicate or unknown JSON properties, field types, immutable existing identifiers and duplicate inserts before writing. Failed validation retains entered values. Canceling a modified editor asks whether to discard the changes, and deleting a document requires confirmation. Refresh reopens storage to replace a stale snapshot.

The reconstruction boundary is the information persisted in the schema. Scalar CLR fields and dotted nested object paths are supported. Types that cannot be reconstructed from that metadata are rejected explicitly. Writing also requires an exact schema match, preventing the explorer from silently migrating an incompatible original model.

## Display languages

The interface follows the Windows display language at application startup. German display cultures, including `de-DE` and `de-AT`, use the German satellite resources. English is the neutral fallback for other languages. Captions, project management, accessibility descriptions, document dialogs, validation messages, WQL diagnostics and count labels share the resources in `Localization/Strings.resx` and `Localization/Strings.de.resx`.

The localization boundary preserves data and query semantics. Project names, document values, schema property paths, exported property names and WQL keywords are not translated. The index language picker controls token processing independently of the display language. The JSON editor and document form retain invariant decimal and date input conventions, while status counts use the operating system's number formatting culture.

The resource contract requires matching keys and format placeholders in both languages. Add corresponding accessors in `Localization/AppText.cs` for XAML or code captions. Parser diagnostics use the engine's existing message keys. The WiUI localization tests verify language fallback, resource parity, placeholders, singular and plural counts, and unchanged WQL grammar.

## Build and validation

The application and its dedicated `WebExpress.WebIndex.WiUI.Test` project require Windows, .NET 10 and the `maui-windows` workload. The test project references the actual application project and exercises its services against isolated persisted indexes. It covers source immutability, CRUD, reverse postings, decimal culture handling, schema protection, WQL, completion, exports, query history, project migration and durable missing references.

The managed WinUI build imports `src/ManagedWinUI.targets` after the SDK targets. This removes the native `GetVCInstallPath` dependency from the three XAML compilation pipelines for these C# projects. Windows App SDK 1.8 otherwise enumerates a missing MSVC directory when Visual Studio supplies `VsInstallRoot`, even though managed XAML does not use `vcmeta.dll`. No C++ compiler installation or machine-specific path is required for this managed build.

The following commands build and run the native test project from the repository root. MAUI asset generation is disabled in the test runner because it consumes the application assembly and does not package another UI application.

```powershell
dotnet build src/WebExpress.WebIndex.WiUI.Test/WebExpress.WebIndex.WiUI.Test.csproj -c DebugLocal
& src/WebExpress.WebIndex.WiUI.Test/bin/DebugLocal/net10.0-windows10.0.19041.0/win-x64/WebExpress.WebIndex.WiUI.Test.exe -noLogo
```
