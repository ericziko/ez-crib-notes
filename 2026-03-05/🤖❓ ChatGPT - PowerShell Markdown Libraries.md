---
uid: 550bcb54-74af-49ac-a008-4d5283c9189f
---

# 🤖❓ ChatGPT - PowerShell Markdown Libraries

#para/resources/PowerShell #para/resources/markdown 
## Links
[ChatGPT - PowerShell Markdown Libraries](https://chatgpt.com/share/69a9df33-deb8-8000-89f5-34b71a52bdce)

## Summary
🤖❓ Are there any particular PowerShell libraries around the consumption, generation, or use of Markdown

## 🤖💡 
PowerShell has quietly grown a small ecosystem around Markdown. Nothing like the massive JavaScript universe, but enough tools exist to treat Markdown as a data format, not just text. Think of them in three buckets: rendering, parsing/manipulating, and document generation.

### PSMarkdown — the classic PowerShell Markdown module

This is the one most people encounter first. It lets PowerShell generate Markdown programmatically.

#### Repo
<https://github.com/wholeyjeff/PSMarkdown>

#### Example

```powershell
Import-Module PSMarkdown

New-Markdown -FilePath README.md {
    New-MDHeader "My Project" -Level 1
    New-MDParagraph "This documentation was generated from PowerShell."
    
    New-MDTable -Columns Name, Value -Rows @(
        @{Name="Version"; Value="1.0"}
        @{Name="Author"; Value="EZ"}
    )
}
```

What it's good for:
- Generating docs from scripts
- Automated README generation
- CI/CD documentation steps
- Turning objects into Markdown tables

It basically treats Markdown like a templating DSL.

### Markdig (via .NET) — the industrial-strength Markdown engine

Under the hood, most serious .NET Markdown work uses Markdig, which is the same parser used in many .NET tools.

PowerShell can call it directly because PowerShell is .NET.

**Install:**

```powershell
Install-Package Markdig
```

**Example:**

```powershell
Add-Type -Path "Markdig.dll"
$markdown = "# Hello`nThis is **Markdown**."
$html = [Markdig.Markdown]::ToHtml($markdown)
$html
```

Why this matters:

Markdig gives you:
- full AST parsing
- Markdown → HTML
- custom extensions
- syntax trees you can manipulate

So if you wanted to:
- parse Markdown files
- extract headings
- modify tables
- build Markdown linters

This is the real engine.

### platyPS — documentation generator for PowerShell modules

This one is official Microsoft tooling.

It generates Markdown-based help docs from PowerShell modules.

#### Install

```powershell
Install-Moule platyPS
```

#### Generate docs

```powershell
New-MarkdownHelp -Module MyModule -OutputFolder docs
```

##### Result

```sh
docs/
  Get-Widget.md
  Set-Widget.md
  Remove-Widget.md
```

Each file becomes the canonical documentation source.

Then you can generate PowerShell help:

```powershell
New-ExternalHelp docs -OutputPath en-US
```

Many large PowerShell projects use this workflow.

### Show-Markdown — render Markdown inside the console

PowerShell 6+ added native rendering.

#### Example

```powershell
Get-Content README.md -Raw | Show-Markdown
```

This uses a terminal Markdown renderer, which can display:
- headings
- code blocks
- lists
- tables

right inside the console.

Useful for CLI tools.

### PSWriteHTML — Markdown → HTML sites

#### PSWriteHTML

This module can consume Markdown and generate HTML reports.

Example uses:
- documentation sites
- dashboards
- CI reports

PowerShell people often combine it with Markdown pipelines.

The interesting trick: Markdown as structured data

Once you start parsing Markdown via **Markdig**, something fun happens.

Markdown stops being text and becomes a syntax tree:

```
Document
 ├── Heading
 ├── Paragraph
 ├── List
 └── CodeBlock
```

Which means you can do things like:
- extract all links from a Markdown vault
- convert tables → PowerShell objects
- auto-index documentation
- build static sites
- lint docs in CI

It's basically HTML DOM manipulation for Markdown.

### A very PowerShell-y trick

You can convert Markdown tables directly into objects with a little parser.

Example Markdown:

```markdown
| Name | Age |
|------|-----|
| Bob  | 32  |
| Sue  | 28  |
```

#### Sample Table



| Name | Age |
|------|-----|
| Bob  | 32  |
| Sue  | 28  |



#### Parsed into:

```powershell
[pscustomobject]@{
    Name = "Bob"
    Age  = 32
}
```

Now your documentation becomes queryable data.

That idea is weirdly powerful.

If I were building a Markdown toolkit today

For serious work:
- Markdig → parsing engine
- PSMarkdown → document generation
- platyPS → PowerShell help docs
- Show-Markdown → console rendering

That stack covers 95% of use cases.

One particularly fun direction for someone who lives in PowerShell and Markdown vaults (looking at you, Obsidian power users) is building Markdown AST pipelines — scripts that walk your docs like a compiler walks source code. Once you see documentation as structured input, entire automation worlds open up.

