---
uid: f0701608-092f-466a-b7d4-68a3ed8c7b86
---
# open-shift-orleans-etl-architecture-questions


- I am converting a bunch of SSIS jobs into C# code designed to run inside containers inside Kubernetes/OpenShift.
- These C# jobs basically copy data from a source database to a target database and do other various data fix-ups.
- These jobs need to be deployed to several different OpenShift clusters for redundancy.
- We need to make sure that the jobs fire off on a regularly scheduled basis
- We need to make sure that these jobs don't run concurrently in different clusters at the same time, 
  because of data race conditions, they will clobber each other.
    - What are my options around Controlling concurrency and preventing race conditions. I'm thinking about using Microsoft Orlean's. 
      - Is Orleans a good option for handling concurrency?
      - What other options are there that I should consider?
- My initial implementation was a CLI utility using SpectreConsole CLI, mostly for the argument parsing - That was to be fired off by either a cron job or auto assist.
- We need to also handle job failure and retries gracefully.

- I'm wondering if I need to expose HTTP endpoints in order to be able to call the jobs, or if the jobs can just be called via command line.
- I'm thinking about deploying the jobs into containers and having the CLI be reentrant..
- We also need to provide some sort of an interface for our second-level support team to be able to query the status of the job runs 
and re-trigger the jobs if they fail.

- Please interview me and help me flesh this out.

