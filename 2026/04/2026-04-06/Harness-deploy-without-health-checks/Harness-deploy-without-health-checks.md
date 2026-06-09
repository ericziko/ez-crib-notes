---
uid: 8f1e0522-b802-4a5c-92f4-1a033ab45ac5
---

# Harness-deploy-without-health-checks
In our CICD pipeline at work - we are using Harness to do deployment and during that it requires Health check and Readiness check endpoints in order to check that the code has been deployed properly. We building a container that simply contains a CLI tool that will be invoked by a scheduler and does not have a persistent lifetime. I read somewhere that there is some sort of file that we can drop during the Harness deployment to indicate to Harness that the container is ready if it does not support health check URL's

I am new to Harness and have no idea what I am doing. 

Please walk me through this. Please ask me any questions you may have
