@{
    ModuleVersion     = '1.0.0'
    GUID              = 'a3f2e1b4-8c7d-4e5f-9a0b-1c2d3e4f5a6b'
    Author            = 'ericziko'
    Description       = 'Recursively finds all Git repositories under a directory using fd for fast traversal.'
    PowerShellVersion = '7.0'
    RootModule        = 'GitRepoFinder.psm1'
    FunctionsToExport = @('Find-GitRepositories')
    AliasesToExport   = @('fgr')
    PrivateData       = @{
        PSData = @{
            Tags = @('Git', 'Repository', 'Search', 'fd', 'DevTools')
        }
    }
}
