# 🤖❓ Prompt

We were pointing our client application, which was running inside a POD in OpenShift pointing directly at a Primary SQL server through a DNS host name

In order to reconfigure our app to point at the SQL Server High Availability cluster - our engineer simply tried to update the DNS resolution to point from the primary sql server to the HA endpoint and we ran into problems - it seemed to initially work and then it failed to connect the next day - any idea what kind of problems we may have introduced through DNS redirection from an app running inside OpenShift.


Please create a separate markdown document in this directory with A detailed analysis of everything that could possibly go wrong with this configuration.
