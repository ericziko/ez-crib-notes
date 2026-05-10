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



