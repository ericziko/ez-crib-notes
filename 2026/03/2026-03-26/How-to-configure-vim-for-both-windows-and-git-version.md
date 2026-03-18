

# How-to-configure-vim-for-both-windows-and-git-version

I am using VIM on windows. I am using both the windows version that I installed under my `c:\Program Files` 
directory as installed by the enterprise that I work for as well as the more UNIX like version of VIM that
comes pre-installed on windows with GIT

Unfortunately, the UNIX like version of VIM and the Windows version of VIM do not share the same directory 
structure and location for configuration files. The locations, file names and folder names are slightly different 
between both configurations - however I would like to put all of my VIM configuration in a central location
and be able to share those configuration files such as my `.vimrc` and plug in directories across both versions

- How can I achieve such a setup on Windows

Also - I am now getting random errors that VIM can't write to what ever $TMP folder it is trying to write to
when trying to install plugins with [VIM-PLUG](https://github.com/junegunn/vim-plug)

- How do I set the TEMP directory for VIM - where does it read this information from.

Please write me a detailed tutorial / FAQ markdown document on how to normalize my configuration across both
the windows version of VIM and the pseudo linux version of VIM that comes bundled with the windows version of GIT
