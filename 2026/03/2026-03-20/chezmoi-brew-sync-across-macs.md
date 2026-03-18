---
uid: a3f7c2e1-8b4d-4a9e-b1c6-d2e5f8a0c3b7
title: Syncing Homebrew Installs Across Macs with chezmoi
created: 2026-03-21T00:00:00+11:00
modified: 2026-03-21T00:00:00+11:00
tags:
  - chezmoi
  - homebrew
  - dotfiles
  - macos
  - devops
  - tooling
---

# 🍺 Syncing Homebrew Installs Across Macs with chezmoi

## 📖 Overview

[chezmoi](https://www.chezmoi.io/) is a dotfile manager that uses a Git-backed source directory to keep your home directory configuration in sync across multiple machines. One of its most powerful features is the ability to run **scripts** — including scripts that install and maintain your Homebrew packages via a `Brewfile`.

This tutorial explains the full workflow: from initial setup, to managing a `Brewfile`, to bootstrapping a brand-new Mac automatically.

---

## 🧱 Prerequisites

- Homebrew installed on at least one Mac (`/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"`)
- A GitHub (or GitLab/Bitbucket) account for hosting your dotfiles
- Basic familiarity with the terminal

---

## 🤖❓ What is a Brewfile?

A `Brewfile` is a plain-text manifest of everything you want Homebrew to install. It is the `package.json` of your Mac tooling. Running `brew bundle` against one installs everything listed in it.

### 🤖💡 Example Brewfile

```ruby
# Taps (third-party repositories)
tap "homebrew/bundle"
tap "homebrew/cask"
tap "homebrew/cask-fonts"

# CLI tools
brew "git"
brew "neovim"
brew "ripgrep"
brew "fd"
brew "bat"
brew "fzf"
brew "zoxide"
brew "starship"
brew "chezmoi"
brew "gh"

# Cask applications (GUI apps)
cask "visual-studio-code"
cask "iterm2"
cask "docker"
cask "rectangle"

# Mac App Store apps (requires mas-cli)
brew "mas"
mas "1Password 7", id: 1333542190
```

---

## 🛠️ Step 1 — Install chezmoi and Initialise Your Dotfiles Repo

On your **primary** Mac:

```bash
# Install chezmoi via Homebrew
brew install chezmoi

# Initialise chezmoi — this creates ~/.local/share/chezmoi (your source directory)
chezmoi init
```

chezmoi creates a Git repository at `~/.local/share/chezmoi`. This is where all managed files live.

---

## 📦 Step 2 — Generate Your Brewfile

Use `brew bundle dump` to capture everything currently installed:

```bash
# Dump current brew state to a Brewfile in your home directory
brew bundle dump --file="~/.Brewfile" --force

# Or, if you want to include MAS (Mac App Store) apps too:
brew bundle dump --file="~/.Brewfile" --force --describe
```

The `--describe` flag adds comments above each entry explaining what the package does (sourced from Homebrew's metadata).

---

## 📁 Step 3 — Add the Brewfile to chezmoi

```bash
# Tell chezmoi to manage your Brewfile
chezmoi add ~/.Brewfile
```

This copies `~/.Brewfile` into `~/.local/share/chezmoi/dot_Brewfile`. chezmoi translates the leading `.` to `dot_` so Git can track hidden files cleanly.

Verify it was added:

```bash
chezmoi managed
# → ~/.Brewfile should appear in the list
```

---

## 🤖💡 Step 4 — Create the run_once Install Script

This is the key mechanism for syncing Homebrew packages. chezmoi supports **run scripts** — shell scripts placed in the source directory that are executed when you run `chezmoi apply`.

There are three script prefixes:

| Prefix | Behaviour |
|---|---|
| `run_` | Runs every time `chezmoi apply` is called |
| `run_once_` | Runs **once** per unique file content hash (re-runs only if the script itself changes) |
| `run_onchange_` | Runs whenever the file content changes |

For Brewfile syncing, `run_onchange_` is the best choice — it re-runs `brew bundle` **only when your Brewfile changes**, making it efficient and idempotent.

### 🤖💡 Create the script

```bash
# Create the script file in chezmoi's source directory
cat > ~/.local/share/chezmoi/run_onchange_install-packages.sh.tmpl << 'EOF'
#!/usr/bin/env bash
# chezmoi:template:left-delimiter="{{" right-delimiter="}}"
# Hash: {{ include "dot_Brewfile" | sha256sum }}

set -eufo pipefail

echo "🍺 Running brew bundle..."
brew bundle --file="{{ .chezmoi.homeDir }}/.Brewfile" --no-lock
echo "✅ brew bundle complete."
EOF
```

> **Why the hash comment?**
> The line `# Hash: {{ include "dot_Brewfile" | sha256sum }}` is a chezmoi template expression. Every time the content of `dot_Brewfile` changes, this line's output changes — which changes the script's hash — which causes chezmoi to classify it as "changed" and re-execute it. This is how `run_onchange_` detects that the Brewfile has been updated.

### 🤖💡 What the `.tmpl` extension means

The `.tmpl` suffix tells chezmoi to process the file as a [Go template](https://pkg.go.dev/text/template) before running it. This unlocks:

- Machine-specific conditionals (`{{ if eq .chezmoi.os "darwin" }}`)
- Dynamic paths (`{{ .chezmoi.homeDir }}`)
- Computed hashes for change detection

---

## 🔗 Step 5 — Push to GitHub

```bash
cd ~/.local/share/chezmoi

# Initialise with your GitHub remote (replace with your actual repo URL)
git remote add origin git@github.com:YOUR_USERNAME/dotfiles.git

git add .
git commit -m "Add Brewfile and brew bundle install script"
git push -u origin main
```

Now your dotfiles (including Brewfile and install script) are version-controlled and accessible from any machine.

---

## 💻 Step 6 — Bootstrap a Brand New Mac

On a **new Mac**, the entire setup is a single command after installing Homebrew and chezmoi:

```bash
# 1. Install Homebrew
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

# 2. Install chezmoi (the only manual brew install you'll ever need)
brew install chezmoi

# 3. Pull your dotfiles and apply them in one shot
chezmoi init --apply YOUR_GITHUB_USERNAME
```

The `chezmoi init --apply <username>` command:
1. Clones `https://github.com/YOUR_GITHUB_USERNAME/dotfiles` into `~/.local/share/chezmoi`
2. Immediately runs `chezmoi apply`, which:
   - Deploys all managed files (including `~/.Brewfile`)
   - Executes any `run_once_` or `run_onchange_` scripts — triggering `brew bundle`

The result: your new Mac installs every package in your Brewfile automatically.

---

## 🔄 Day-to-Day Workflow

### Adding a new brew package

```bash
# 1. Install the package
brew install some-new-tool

# 2. Regenerate your Brewfile
brew bundle dump --file="~/.Brewfile" --force

# 3. Stage the change with chezmoi and push
chezmoi re-add ~/.Brewfile       # re-syncs the managed Brewfile from disk
cd ~/.local/share/chezmoi
git add dot_Brewfile
git commit -m "Add some-new-tool to Brewfile"
git push
```

### Pulling changes on another Mac

```bash
# Pull latest dotfiles and apply
chezmoi update
```

`chezmoi update` = `git pull` + `chezmoi apply`. It fetches the latest changes from your remote and applies them, re-running the `run_onchange_` script if the Brewfile changed.

---

## 🧠 Advanced: Machine-Specific Packages

Not every machine needs every package. chezmoi templates let you conditionally install packages based on hostname, OS, or custom data.

### 🤖💡 Using chezmoi data for machine roles

Define machine-specific data in `~/.config/chezmoi/chezmoi.toml`:

```toml
[data]
  role = "work"   # or "personal", "server", etc.
```

Then use a templated Brewfile (`dot_Brewfile.tmpl`):

```ruby
# Always installed
brew "git"
brew "neovim"
brew "ripgrep"

{{ if eq .role "work" -}}
# Work-only tools
cask "slack"
cask "zoom"
brew "awscli"
{{ end -}}

{{ if eq .role "personal" -}}
# Personal tools
cask "steam"
cask "spotify"
{{ end -}}
```

Now `chezmoi apply` generates a `~/.Brewfile` tailored to the machine's role before running `brew bundle`.

### 🤖💡 Using hostname-based conditionals

```ruby
{{ if eq .chezmoi.hostname "my-work-macbook" -}}
brew "corporate-vpn-cli"
{{ end -}}
```

---

## 🧩 Putting It All Together — Directory Structure

Your chezmoi source directory (`~/.local/share/chezmoi`) will look like this:

```
~/.local/share/chezmoi/
├── .chezmoi.toml.tmpl              ← chezmoi config template
├── dot_Brewfile.tmpl               ← managed Brewfile (templated)
├── dot_zshrc.tmpl                  ← your .zshrc (templated)
├── dot_gitconfig                   ← your .gitconfig
├── run_onchange_install-packages.sh.tmpl   ← brew bundle trigger script
├── run_once_configure-macos.sh     ← macOS defaults (runs once ever)
└── private_dot_ssh/
    └── config                      ← SSH config (marked private)
```

---

## 🔒 Security Considerations

- **Never commit secrets** (API keys, tokens) to your dotfiles repo — use chezmoi's [secret manager integrations](https://www.chezmoi.io/user-guide/templating/#secrets) (1Password, Bitwarden, `pass`, etc.)
- The `private_` prefix in chezmoi source names sets file permissions to `0600` (owner-read-only) on apply
- Consider making your dotfiles repo **private** on GitHub if it contains any personal configuration

---

## 🏁 Quick Reference Cheatsheet

| Task | Command |
|---|---|
| Apply dotfiles to current machine | `chezmoi apply` |
| Pull + apply from remote | `chezmoi update` |
| Add/re-sync a file | `chezmoi add ~/.Brewfile` or `chezmoi re-add ~/.Brewfile` |
| See what would change | `chezmoi diff` |
| Edit a managed file | `chezmoi edit ~/.Brewfile` |
| Open source directory | `chezmoi cd` |
| Bootstrap a new Mac | `chezmoi init --apply YOUR_GITHUB_USERNAME` |
| Dump current brew state | `brew bundle dump --file="~/.Brewfile" --force` |
| Install from Brewfile | `brew bundle --file="~/.Brewfile"` |

---

## 📚 Further Reading

- [chezmoi official docs](https://www.chezmoi.io/)
- [chezmoi templating reference](https://www.chezmoi.io/user-guide/templating/)
- [Homebrew Bundle docs](https://github.com/Homebrew/homebrew-bundle)
- [`brew bundle` cheatsheet](https://gist.github.com/ChristopherA/a579274536aab36ea9966f301ff14f3f)
