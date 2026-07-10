---
uid: 80e324cf-e2a2-439b-849f-ca21cc641777
---
# 🤖❓Detailed explanation of  mac `security` commands 

Below is a list of the Mac cli `security` commands - obtained from running `man security` on my Mac 

```
     list-keychains              Display or manipulate the keychain search
                                 list.
     default-keychain            Display or set the default keychain.
     login-keychain              Display or set the login keychain.
     create-keychain             Create keychains.
     delete-keychain             Delete keychains and remove them from the
                                 search list.
     lock-keychain               Lock the specified keychain.
     unlock-keychain             Unlock the specified keychain.
     set-keychain-settings       Set settings for a keychain.
     set-keychain-password       Set password for a keychain.
     show-keychain-info          Show the settings for keychain.
     dump-keychain               Dump the contents of one or more keychains.
     create-keypair              Create an asymmetric key pair.
     add-generic-password        Add a generic password item.
     add-internet-password       Add an internet password item.
     add-certificates            Add certificates to a keychain.
     find-generic-password       Find a generic password item.
     delete-generic-password     Delete a generic password item.
     set-generic-password-partition-list
                                 Set the partition list of a generic password
                                 item.
     find-internet-password      Find an internet password item.
     delete-internet-password    Delete an internet password item.
     set-internet-password-partition-list
                                 Set the partition list of a internet password
                                 item.
     find-key                    Find keys in the keychain
     set-key-partition-list      Set the partition list of a key.
     find-certificate            Find a certificate item.
     find-identity               Find an identity (certificate + private key).
     delete-certificate          Delete a certificate from a keychain.
     delete-identity             Delete a certificate and its private key from
                                 a keychain.
     set-identity-preference     Set the preferred identity to use for a
                                 service.
     get-identity-preference     Get the preferred identity to use for a
                                 service.
     create-db                   Create a db using the DL.
     export                      Export items from a keychain.
     import                      Import items into a keychain.
     cms                         Encode or decode CMS messages.
     install-mds                 Install (or re-install) the MDS database.
     add-trusted-cert            Add trusted certificate(s).
     remove-trusted-cert         Remove trusted certificate(s).
     dump-trust-settings         Display contents of trust settings.
     user-trust-settings-enable  Display or manipulate user-level trust
                                 settings.
     trust-settings-export       Export trust settings.
     trust-settings-import       Import trust settings.
     verify-cert                 Verify certificate(s).
     authorize                   Perform authorization operations.
     authorizationdb             Make changes to the authorization policy
                                 database.
     execute-with-privileges     Execute tool with privileges.
     leaks                       Run /usr/bin/leaks on this process.
     smartcards                  Enable, disable or list disabled smartcard
                                 tokens.
     list-smartcards             Display available smartcards.
     export-smartcard            Export/display items from a smartcard.
     error                       Display a descriptive message for the given
                                 error code(s).

```

For each command above generate for me 
 - A short summary of what each commands does
 - Common use cases for using the command
 - Common examples of invoking  the command
 - Also please explain overall concepts related to the `security` command such as 
	 - What keychains are, how many I have by default and why I would want to create new onese
	 - The difference between `add-generic-password` and `add-internet-password`
	 - How the `security` cli tool does or does not intersect with the Mac `Passswords` app
	 - What is the `leaks` command

---

## 🤖💡 Overview Concepts

Before walking through each subcommand, it helps to understand the model that the `security` tool sits on top of.

### 🔑 What are keychains?

A **keychain** is an encrypted SQLite-style database (`.keychain-db` file) managed by macOS's **Keychain Services** (part of the Security framework). It stores secrets — passwords, private keys, certificates, identities (cert + key), secure notes — and gates access to them with ACLs that name which apps may read/write each item.

The `security` CLI is a thin wrapper around the same `SecKeychain*` C APIs that GUI apps (Safari, Mail, Keychain Access, the Passwords app) use, so anything you do at the CLI is visible to the GUI and vice-versa.

#### How many keychains do I have by default?

On a typical macOS install you will already have:

| Keychain | Path | Purpose |
|---|---|---|
| `login.keychain-db` | `~/Library/Keychains/login.keychain-db` | Your personal default keychain. Unlocked with your login password at login, locked when you log out. Holds Wi‑Fi passwords, Safari saved passwords (legacy), app passwords, your personal certs and keys. |
| `System.keychain` | `/Library/Keychains/System.keychain` | Machine-wide secrets (e.g. Wi‑Fi passwords for every user, MDM payloads, computer certificates). Requires admin privileges to modify. |
| `System Roots` | `/System/Library/Keychains/SystemRootCertificates.keychain` | Read-only bundle of Apple-trusted root CA certificates. |
| `iCloud / "Local Items"` | hidden (CloudKit-backed) | Syncs the modern Keychain (Safari passwords, AutoFill, Wi-Fi if iCloud Keychain is on) across your Apple devices. Not directly listed by `list-keychains` but visible in `dump-keychain` output and the Passwords app. |

Run `security list-keychains` to see your current search list — usually just your login keychain plus the System keychain.

#### Why would I create a new keychain?

- **Project-scoped secrets** — keep build/CI credentials separate from personal secrets so a script can unlock just that one keychain.
- **Different password / lock policy** — you may want a keychain that auto-locks after 5 minutes of idle time even though your login keychain stays open all session.
- **Sharing** — a keychain file can be exported (`export`) and imported (`import`) on another Mac.
- **Throwaway / test keychains** — e.g. while writing automation that mutates keychain items, you don't want to risk your login keychain.

### 🆚 `add-generic-password` vs `add-internet-password`

Both create password entries, but they're different *Keychain item classes* with different schemas:

| | `generic-password` (`kSecClassGenericPassword`) | `internet-password` (`kSecClassInternetPassword`) |
|---|---|---|
| Schema fields | service name (`-s`), account (`-a`), value (`-w`) | server (`-s`), account (`-a`), protocol (`-r`, e.g. `htps`), port (`-P`), path (`-p`), auth-type (`-t`), security-domain (`-d`) |
| Used by | Apps that just need a named secret ("My App API key", "Postgres CLI password") | Browsers / network clients that key on a URL — Safari, Mail, FTP clients, `curl --netrc`-style flows |
| Lookup intent | "Give me the secret called X for account Y" | "Give me the password for `https://example.com:443/admin` as user `eric`" |

If in doubt, use `generic-password` — it's the "free-form" bucket. Use `internet-password` only when the secret really is keyed by a server/protocol/port/path tuple.

### 🆕 `security` CLI vs the macOS **Passwords** app

The Passwords app (introduced in macOS 15 Sequoia) is a GUI for the **modern Keychain / iCloud Keychain** item set — Safari saved passwords, AutoFill credentials, passkeys, Wi‑Fi, verification codes, shared groups.

The `security` CLI predates that app by ~20 years and primarily speaks to the **legacy file-based keychains** (`login.keychain-db`, `System.keychain`). The overlap looks like this:

- ✅ Items added with `add-internet-password` to the login keychain **do** show up in the Passwords app (and Safari AutoFill), because both back-ends are unified under Keychain Services.
- ✅ Reading Safari-saved passwords with `find-internet-password` works for the same reason.
- ❌ Passkeys, shared password groups, verification (TOTP) codes, and items that live purely in iCloud Keychain are **not** manageable via `security` — there is no CLI surface for them yet.
- ❌ The `security` tool will not trigger an iCloud sync of items it creates in the local login keychain.

Rule of thumb: **use `security` for automation against local keychains; use the Passwords app for anything synced, shared, or passkey-shaped.**

### 🩺 What is the `leaks` command?

`security leaks` is a thin shim that runs `/usr/bin/leaks` against the current `security` process — it's a debugging hook the Apple engineers left in for diagnosing memory leaks inside the `security` tool itself. It is **not** a general "scan my Mac for leaked passwords" tool, despite the name. You will almost certainly never need it. The standalone `leaks(1)` utility is the one used for memory-leak hunting in arbitrary processes (e.g. `leaks --atExit -- ./myapp`).

---

## 🤖💡 Command Reference

### Keychain lifecycle & search list

#### `list-keychains`
- **Summary:** Display or modify the keychain search list (the ordered set of keychains the system queries when looking up an item).
- **Use cases:** See which keychains are active; add a project-specific keychain so its items are findable by `find-*` lookups; restrict the list before running an automation.
- **Examples:**
  ```bash
  security list-keychains                                    # show current list
  security list-keychains -d user                            # user-domain only
  security list-keychains -s ~/Library/Keychains/login.keychain-db ~/ci.keychain-db
  ```

#### `default-keychain`
- **Summary:** Show or set the *default* keychain — the one that receives new items when no `-k` flag is given.
- **Use cases:** Temporarily redirect new items to a CI-only keychain; verify your default after creating a new one.
- **Examples:**
  ```bash
  security default-keychain                                  # show
  security default-keychain -s ~/ci.keychain-db              # set
  ```

#### `login-keychain`
- **Summary:** Show or set which keychain is treated as the login keychain (auto-unlocked at login).
- **Use cases:** Rare — usually only after restoring a Mac or migrating accounts.
- **Examples:**
  ```bash
  security login-keychain
  security login-keychain -s ~/Library/Keychains/login.keychain-db
  ```

#### `create-keychain`
- **Summary:** Create a new keychain file with a password.
- **Use cases:** Per-project secret stores, CI keychains, throwaway sandboxes.
- **Examples:**
  ```bash
  security create-keychain -p "$KCPASS" ~/ci.keychain-db
  security create-keychain ~/scratch.keychain-db             # prompts for password
  ```

#### `delete-keychain`
- **Summary:** Delete keychain file(s) and remove from the search list.
- **Use cases:** Tear down CI keychains; clean up after tests.
- **Examples:**
  ```bash
  security delete-keychain ~/ci.keychain-db
  ```

#### `lock-keychain`
- **Summary:** Lock a keychain (require password before next access).
- **Use cases:** Force-lock at the end of a sensitive script; lock all keychains before walking away.
- **Examples:**
  ```bash
  security lock-keychain ~/ci.keychain-db
  security lock-keychain -a                                  # lock all
  ```

#### `unlock-keychain`
- **Summary:** Unlock a keychain so its items can be read.
- **Use cases:** CI scripts that need access to signing identities; unattended automations.
- **Examples:**
  ```bash
  security unlock-keychain -p "$KCPASS" ~/ci.keychain-db
  security unlock-keychain                                   # interactive prompt
  ```

#### `set-keychain-settings`
- **Summary:** Configure auto-lock timeouts and lock-on-sleep behavior.
- **Use cases:** Make a CI keychain stay unlocked for the duration of a build; tighten security on a sensitive keychain.
- **Examples:**
  ```bash
  security set-keychain-settings -lut 3600 ~/ci.keychain-db  # lock after 1h idle, lock on sleep
  security set-keychain-settings -t 0 ~/ci.keychain-db       # never timeout
  ```

#### `set-keychain-password`
- **Summary:** Change a keychain's password.
- **Use cases:** Rotate a keychain password; sync the keychain password after the user changes their login password manually.
- **Examples:**
  ```bash
  security set-keychain-password -o "$OLD" -p "$NEW" ~/ci.keychain-db
  ```

#### `show-keychain-info`
- **Summary:** Print the current settings (timeout, lock-on-sleep) for a keychain.
- **Use cases:** Debugging why a keychain keeps locking mid-build.
- **Examples:**
  ```bash
  security show-keychain-info ~/ci.keychain-db
  ```

#### `dump-keychain`
- **Summary:** Dump the metadata (and optionally values) of every item in a keychain.
- **Use cases:** Audit what's stored; export for inspection. With `-d` it prompts (per item!) for permission to reveal each secret.
- **Examples:**
  ```bash
  security dump-keychain ~/Library/Keychains/login.keychain-db
  security dump-keychain -d ~/Library/Keychains/login.keychain-db   # include data — many prompts
  ```

### Keys

#### `create-keypair`
- **Summary:** Create an asymmetric (RSA/EC) key pair and store it in a keychain.
- **Use cases:** Generate signing keys for code signing or ad-hoc cryptography.
- **Examples:**
  ```bash
  security create-keypair -a rsa -s 2048 -f "MyKey" ~/ci.keychain-db
  ```
  > Note: in modern macOS most folks use `openssl genpkey` or `ssh-keygen` and then `import` into the keychain — `create-keypair` is rarely the most convenient path.

#### `find-key`
- **Summary:** Locate keys in a keychain.
- **Use cases:** Verify a code-signing private key was imported; list all keys before cleanup.
- **Examples:**
  ```bash
  security find-key
  security find-key -t priv ~/Library/Keychains/login.keychain-db
  ```

#### `set-key-partition-list`
- **Summary:** Edit the **partition list** of a key — the ACL that names which apps may use it without prompting. Critical for CI: without this, signing tools will trigger an interactive "Allow / Deny" dialog.
- **Use cases:** Allow `codesign` / `productsign` / Xcode to use an imported signing key in headless CI.
- **Examples:**
  ```bash
  security set-key-partition-list \
      -S apple-tool:,apple:,codesign: \
      -s -k "$KCPASS" ~/ci.keychain-db
  ```

### Generic passwords (free-form named secrets)

#### `add-generic-password`
- **Summary:** Add a named secret (service + account → password).
- **Use cases:** Store an API token for a script; cache a DB password.
- **Examples:**
  ```bash
  security add-generic-password -s "MyApp/API" -a "eric" -w "$TOKEN"
  security add-generic-password -s "psql" -a "postgres" -w "$PW" -U  # update if exists
  ```

#### `find-generic-password`
- **Summary:** Look up a generic password.
- **Use cases:** Read a token from a script without hard-coding it.
- **Examples:**
  ```bash
  security find-generic-password -s "MyApp/API" -a "eric" -w   # print only the password
  TOKEN=$(security find-generic-password -s "MyApp/API" -a "eric" -w)
  ```

#### `delete-generic-password`
- **Summary:** Delete a generic password item.
- **Examples:**
  ```bash
  security delete-generic-password -s "MyApp/API" -a "eric"
  ```

#### `set-generic-password-partition-list`
- **Summary:** Set the partition list (ACL of allowed apps) for a generic password — same idea as `set-key-partition-list` but for password items.
- **Examples:**
  ```bash
  security set-generic-password-partition-list \
      -S apple-tool:,apple: -k "$KCPASS" \
      -s "MyApp/API" -a "eric"
  ```

### Internet passwords (URL-keyed credentials)

#### `add-internet-password`
- **Summary:** Add a credential keyed by server/protocol/port/path.
- **Use cases:** Pre-seed Safari/AutoFill, store an FTP/HTTP credential for a tool that consults the keychain.
- **Examples:**
  ```bash
  security add-internet-password \
      -s example.com -a eric -r htps -P 443 -p /admin -w "$PW"
  ```

#### `find-internet-password`
- **Summary:** Look up an internet password by server/account.
- **Examples:**
  ```bash
  security find-internet-password -s example.com -a eric -w
  ```

#### `delete-internet-password`
- **Summary:** Delete an internet password item.
- **Examples:**
  ```bash
  security delete-internet-password -s example.com -a eric
  ```

#### `set-internet-password-partition-list`
- **Summary:** Set the partition list (ACL) for an internet password item.
- **Examples:**
  ```bash
  security set-internet-password-partition-list \
      -S apple-tool:,apple: -k "$KCPASS" \
      -s example.com -a eric
  ```

### Certificates & identities

#### `add-certificates`
- **Summary:** Import certificate file(s) into a keychain (no private key — see `import` for `.p12` bundles).
- **Use cases:** Add an internal CA cert; install a peer's public cert for verification.
- **Examples:**
  ```bash
  security add-certificates -k ~/ci.keychain-db ./internal-ca.cer
  ```

#### `find-certificate`
- **Summary:** Find a certificate by common name, email, or hash; optionally print its PEM.
- **Examples:**
  ```bash
  security find-certificate -c "Apple Development: Eric Z" -p
  security find-certificate -a -p ~/ci.keychain-db > all-certs.pem
  ```

#### `find-identity`
- **Summary:** Find identities (cert + matching private key). Most useful for code-signing workflows.
- **Examples:**
  ```bash
  security find-identity -v -p codesigning
  security find-identity -v -p codesigning ~/ci.keychain-db
  ```

#### `delete-certificate`
- **Summary:** Delete a certificate from a keychain.
- **Examples:**
  ```bash
  security delete-certificate -c "Stale Cert Name"
  security delete-certificate -Z <SHA1-hash>
  ```

#### `delete-identity`
- **Summary:** Delete a certificate **and** its private key in one shot.
- **Examples:**
  ```bash
  security delete-identity -c "Apple Development: Old Cert" ~/ci.keychain-db
  ```

#### `set-identity-preference`
- **Summary:** Pin a specific identity (cert+key) as the preferred one to use for a service URL/string.
- **Use cases:** Tell macOS "for this internal service, always present this client cert."
- **Examples:**
  ```bash
  security set-identity-preference -s "https://internal.example.com" -c "My Client Cert"
  ```

#### `get-identity-preference`
- **Summary:** Show which identity is pinned for a service.
- **Examples:**
  ```bash
  security get-identity-preference -s "https://internal.example.com"
  ```

### Database, import/export, CMS, MDS

#### `create-db`
- **Summary:** Low-level — create a database via the Data Library (CDSA/DL). Predates the modern keychain APIs and is rarely needed; `create-keychain` is what you almost always want.
- **Examples:**
  ```bash
  security create-db ~/test.db
  ```

#### `export`
- **Summary:** Export items (certs, keys, identities) from a keychain to a file (PEM, PKCS#7, PKCS#12, etc.).
- **Examples:**
  ```bash
  security export -k ~/ci.keychain-db -t identities -f pkcs12 -o signing.p12
  security export -k login.keychain-db -t certs -f pemseq -o certs.pem
  ```

#### `import`
- **Summary:** Import certificates / keys / identities from a file into a keychain.
- **Use cases:** Bring a `.p12` signing identity into a CI keychain.
- **Examples:**
  ```bash
  security import signing.p12 -k ~/ci.keychain-db -P "$P12_PW" \
      -T /usr/bin/codesign -T /usr/bin/security
  ```

#### `cms`
- **Summary:** Encode or decode CMS (Cryptographic Message Syntax, RFC 5652) messages — sign, verify, encrypt, decrypt.
- **Use cases:** Sign a payload like macOS provisioning profiles; verify a CMS-signed blob.
- **Examples:**
  ```bash
  security cms -S -N "My Signing Cert" -i payload.plist -o signed.cms
  security cms -D -i signed.cms -o payload.plist
  ```

#### `install-mds`
- **Summary:** (Re)install the Module Directory Services database used by the legacy CDSA security stack. Diagnostic only — Apple has been winding down CDSA for years.

### Trust settings & cert verification

#### `add-trusted-cert`
- **Summary:** Mark a certificate as trusted (root or intermediate) at user, admin, or system scope.
- **Use cases:** Trust an internal CA on a dev machine; trust a self-signed cert for local HTTPS.
- **Examples:**
  ```bash
  security add-trusted-cert -d -r trustRoot \
      -k ~/Library/Keychains/login.keychain-db ./internal-ca.cer
  sudo security add-trusted-cert -d -r trustRoot \
      -k /Library/Keychains/System.keychain ./internal-ca.cer
  ```

#### `remove-trusted-cert`
- **Summary:** Reverse of `add-trusted-cert`.
- **Examples:**
  ```bash
  security remove-trusted-cert ./internal-ca.cer
  ```

#### `dump-trust-settings`
- **Summary:** Print user/admin/system trust settings.
- **Examples:**
  ```bash
  security dump-trust-settings -d                            # admin domain
  security dump-trust-settings -s                            # system domain
  ```

#### `user-trust-settings-enable`
- **Summary:** Enable, disable, or query whether user-level trust settings are honored.
- **Examples:**
  ```bash
  security user-trust-settings-enable                        # show status
  security user-trust-settings-enable -d                     # disable
  security user-trust-settings-enable -e                     # enable
  ```

#### `trust-settings-export`
- **Summary:** Export trust settings to an XML plist for backup or transport.
- **Examples:**
  ```bash
  security trust-settings-export -d ./admin-trust.plist
  ```

#### `trust-settings-import`
- **Summary:** Import a previously exported trust-settings plist.
- **Examples:**
  ```bash
  sudo security trust-settings-import -d ./admin-trust.plist
  ```

#### `verify-cert`
- **Summary:** Verify a certificate against a policy (SSL, code signing, etc.) and a date.
- **Use cases:** Validate a cert during build / pre-deployment.
- **Examples:**
  ```bash
  security verify-cert -c ./server.cer -p ssl -s example.com
  security verify-cert -c ./code.cer -p codeSign
  ```

### Authorization

#### `authorize`
- **Summary:** Perform an authorization request against the macOS authorization framework (the same one that drives the "enter your password to make changes" prompts).
- **Use cases:** Programmatically request a right; debug authorization plugins.
- **Examples:**
  ```bash
  security authorize -u system.privilege.admin
  ```

#### `authorizationdb`
- **Summary:** Read or modify entries in `/etc/authorization` (the authorization policy database).
- **Use cases:** Allow non-admin users to perform a specific privileged action; tighten an unusually loose right.
- **Examples:**
  ```bash
  security authorizationdb read system.preferences
  sudo security authorizationdb write system.preferences allow
  ```

#### `execute-with-privileges`
- **Summary:** Run a tool elevated via the Authorization API (prompts via the GUI). Officially **deprecated** in modern macOS — use `sudo` or a privileged helper tool instead.
- **Examples:**
  ```bash
  security execute-with-privileges /usr/sbin/installer -pkg ./pkg -target /
  ```

### Diagnostics & misc

#### `leaks`
- **Summary:** Run `/usr/bin/leaks` against the current `security` process (debugging hook for the tool itself — see overview above).
- **Examples:**
  ```bash
  security leaks
  ```

#### `smartcards`
- **Summary:** Enable, disable, or list disabled smartcard tokens.
- **Use cases:** PIV / CAC card workflows in enterprise environments.
- **Examples:**
  ```bash
  security smartcards token -l                               # list
  security smartcards token -d com.apple.CryptoTokenKit.pivtoken
  security smartcards token -e com.apple.CryptoTokenKit.pivtoken
  ```

#### `list-smartcards`
- **Summary:** Show the smartcard readers/tokens currently visible to the system.
- **Examples:**
  ```bash
  security list-smartcards
  ```

#### `export-smartcard`
- **Summary:** Export (or display) certificates/items from a connected smartcard.
- **Examples:**
  ```bash
  security export-smartcard -i com.apple.pivtoken:XXXX -e ./piv-cert.pem
  ```

#### `error`
- **Summary:** Translate a numeric Security/OSStatus error code into a human-readable description.
- **Use cases:** Decoding the cryptic `-25300`-style errors that pop out of `codesign`, `security`, or app logs.
- **Examples:**
  ```bash
  security error -25300                                      # → errSecItemNotFound
  security error -34018 -25291
  ```

---

## 🤖💡 Putting it together — a typical CI signing recipe

The single most common reason engineers learn `security` deeply is to set up a CI keychain for code signing on macOS. Most of the commands above appear in this one workflow:

```bash
KCPASS=$(openssl rand -hex 16)
KC=~/ci.keychain-db

security create-keychain -p "$KCPASS" "$KC"
security set-keychain-settings -lut 21600 "$KC"          # auto-lock after 6h
security unlock-keychain -p "$KCPASS" "$KC"
security list-keychains -d user -s "$KC" \
    $(security list-keychains -d user | tr -d '" ')      # prepend, keep existing

security import signing.p12 -k "$KC" -P "$P12_PW" \
    -T /usr/bin/codesign -T /usr/bin/security

security set-key-partition-list \
    -S apple-tool:,apple:,codesign: -s -k "$KCPASS" "$KC"

security find-identity -v -p codesigning "$KC"
```

That single recipe touches `create-keychain`, `set-keychain-settings`, `unlock-keychain`, `list-keychains`, `import`, `set-key-partition-list`, and `find-identity` — which is a good sign you've absorbed the most load-bearing parts of the tool.

