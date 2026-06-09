---
title: Resolving Multiple LazyGit Versions on macOS
created: 2026-03-22
modified: 2026-03-22
tags:
  - homebrew
  - lazygit
  - tools
  - troubleshooting
uid: 6ecfd232-c5df-420e-970d-445587a81a8f
---

# Resolving Multiple LazyGit Versions on macOS

## Problem

When you had two versions of LazyGit installed via Homebrew (0.57.0 and 0.60.0), the system was using the older version 0.57.0 by default, even though you wanted 0.60.0.

### Symptoms

```sh
❯ lazygit --version
version=0.57.0  # Old version still active
```

This happens when Homebrew has multiple versions of a package installed in the Cellar, but the symlink in `/opt/homebrew/bin/` points to the wrong one.

## Root Cause

When you install multiple versions of a package with Homebrew, they get stored separately:
- `/opt/homebrew/Cellar/lazygit/0.57.0/` - Old version
- `/opt/homebrew/Cellar/lazygit/0.60.0/` - New version

The symlink at `/opt/homebrew/bin/lazygit` initially pointed to 0.57.0, so that's the version you were using globally.

## Solution

Run the following command:

```bash
brew link --overwrite lazygit
```

### What This Command Does

- **`brew link`**: Creates or updates symlinks in `/opt/homebrew/bin/` for the currently active formula version
- **`--overwrite`**: Forces Homebrew to update the symlink, overwriting any existing one

### Result

After running `brew link --overwrite lazygit`:

```bash
❯ brew link --overwrite lazygit
Linking /opt/homebrew/Cellar/lazygit/0.60.0... 1 symlinks created.

❯ lazygit --version
version=0.60.0  # Now using the new version!

❯ ls -la /opt/homebrew/bin/lazygit
lrwxr-xr-x lazygit -> ../Cellar/lazygit/0.60.0/bin/lazygit
```

The symlink now correctly points to version 0.60.0, making it the default.

## Cleanup (Optional)

If you want to completely remove the old version to free up disk space:

```bash
brew uninstall lazygit@0.57.0  # If it's tracked as a separate formula
# OR
rm -rf /opt/homebrew/Cellar/lazygit/0.57.0  # Manual removal
```

In your case, the 0.57.0 version has already been automatically cleaned up, leaving only 0.60.0.

## Verification

To confirm you have the correct version:

```bash
lazygit --version                    # Should show 0.60.0
which lazygit                        # Should show /opt/homebrew/bin/lazygit
ls -la /opt/homebrew/bin/lazygit     # Should point to 0.60.0
```

## Key Takeaway

When you have multiple versions of a Homebrew package installed, use:

```bash
brew link --overwrite <package-name>
```

This is the standard way to "activate" a specific version of a Homebrew formula without manually managing symlinks.
