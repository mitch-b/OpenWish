"""Instrument an isolated build; never modify the app source in the worktree."""

import json
from pathlib import Path
import re
import sys


root = Path(sys.argv[1])
rows = []
mode_pattern = re.compile(r"^@rendermode\s+InteractiveAuto[ \t]*$", re.MULTILINE)
for project in ["OpenWish.Web", "OpenWish.Web.Client"]:
    for path in sorted((root / project / "Components").rglob("*.razor")):
        if path.name == "_Imports.razor":
            continue
        text = path.read_text(encoding="utf-8-sig")
        key = path.relative_to(root / project / "Components").with_suffix("").as_posix()
        route = re.search(r'^@page\s+"([^"]+)"', text, re.MULTILINE)
        auto = mode_pattern.search(text)
        if not auto and re.search(r"^@rendermode\b", text, re.MULTILINE):
            raise ValueError(f"Review new render mode before benchmarking: {path}")
        rows.append({
            "component": f"{project}/{key}",
            "key": key,
            "route": route[1] if route else None,
            "boundary": "Auto" if auto else "Static" if project == "OpenWish.Web" else "Inherited",
        })
        if not auto:
            continue
        text = mode_pattern.sub(
            "@rendermode OpenWish.Web.Client.RenderBenchmark.Mode", text, count=1
        )
        code = re.search(r"^@code\s*\{", text, re.MULTILINE)
        if not code:
            raise ValueError(f"No code block for benchmark probe: {path}")
        probe = f'''
@if (RendererInfo.IsInteractive)
{{
    <button type="button" hidden data-render-benchmark="{key}"
            data-renderer="@RendererInfo.Name" data-probe-count="@_benchmarkCount"
            @onclick="() => _benchmarkCount++"></button>
}}

'''
        text = (
            text[:code.start()] + probe + code[0] + "\n    private int _benchmarkCount;\n"
            + text[code.end():]
        )
        path.write_text(text, encoding="utf-8")

(root / "OpenWish.Web.Client/RenderBenchmark.cs").write_text('''\
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace OpenWish.Web.Client;

public static class RenderBenchmark
{
    public static IComponentRenderMode Mode { get; } =
        Environment.GetEnvironmentVariable("OPENWISH_BENCHMARK_MODE") switch
        {
            "Server" => RenderMode.InteractiveServer,
            "WebAssembly" => RenderMode.InteractiveWebAssembly,
            _ => RenderMode.InteractiveAuto
        };
}
''', encoding="utf-8")
print(json.dumps(rows, indent=2))
