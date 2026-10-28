# Third-party notices

WebExpress.WebIndex is published under the MIT licence, see [LICENSE](LICENSE).
This file names its direct dependencies - what its own projects reference or ship - not what those bring in turn; each WebExpress package carries its own notices.

## Third-party components

| Component | Version | Used by | Licence |
|---|---|---|---|
| [.NET runtime](https://github.com/dotnet/runtime) | 10.0 | all projects | [MIT](https://spdx.org/licenses/MIT.html) |
| [Microsoft.Maui.Controls](https://github.com/dotnet/maui) | 10.0.110 | WebExpress.WebIndex.WiUI | [MIT](https://spdx.org/licenses/MIT.html) |
| [Microsoft.Extensions.Logging.Debug](https://github.com/dotnet/runtime) | 10.0.0 | WebExpress.WebIndex.WiUI | [MIT](https://spdx.org/licenses/MIT.html) |
| [CommunityToolkit.Maui](https://github.com/CommunityToolkit/Maui) | 15.0.1 | WebExpress.WebIndex.WiUI | [MIT](https://spdx.org/licenses/MIT.html) |
| [Open Sans (Regular, Semibold)](https://fonts.google.com/specimen/Open+Sans) | 1.10 | WebExpress.WebIndex.WiUI (Resources/Fonts) | [Apache-2.0](https://spdx.org/licenses/Apache-2.0.html), licence text and attribution in [licenses/OpenSans](licenses/OpenSans) |

## Build and test only

Used to build and test the projects; not part of what is distributed.

| Component | Version | Used by | Licence |
|---|---|---|---|
| [xunit.v3](https://github.com/xunit/xunit) | 4.0.2 | WebExpress.WebIndex.Test, .Wi.Test, .WiUI.Test | [Apache-2.0](https://spdx.org/licenses/Apache-2.0.html) |

## Licence texts

### MIT License

Applies to the components above marked MIT, under these copyright notices:

- .NET runtime: Copyright (c) .NET Foundation and Contributors
- Microsoft.Maui.Controls: Copyright (c) Microsoft Corporation
- Microsoft.Extensions.Logging.Debug: Copyright (c) Microsoft Corporation
- CommunityToolkit.Maui: Copyright (c) Microsoft Corporation

```
Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
```

### Apache License 2.0

- Open Sans (Regular, Semibold): Digitized data copyright 2010-2011, Google Corporation; licence text and attribution in [licenses/OpenSans](licenses/OpenSans).

The build and test components are not distributed; their licences are linked in the table above.
