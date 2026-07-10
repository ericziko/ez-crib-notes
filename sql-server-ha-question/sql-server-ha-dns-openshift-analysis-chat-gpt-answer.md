---
uid: 9c49f552-0089-464d-832d-0170e58d9f61
---

## 🤖❓Prompt


We were pointing our client application, which was running inside a POD in OpenShift pointing directly at a Primary SQL server through a DNS host name
In order to reconfigure our app to point at the SQL Server High Availability cluster - our engineer simply tried to update the DNS resolution to point from the primary sql server to the HA endpoint and we ran into problems - it seemed to initially work and then it failed to connect the next day - any idea what kind of problems we may have introduced through DNS redirection from an app running inside OpenShift.

## 🤖💡 
Yes — that kind of DNS redirection can absolutely create intermittent breakage, especially from an app in OpenShift to SQL Server HA.

What likely happened is that your app was originally connecting to a specific SQL Server instance, but after the DNS change it started reaching something with different connection semantics: a listener, a cluster name, a different port, a different certificate identity, or a different failover behavior. That can look fine at first and then fail later once caches expire, failover occurs, or a different replica answers.

The main failure modes I'd suspect are these.

First, **port mismatch**. If the old primary was on one SQL port and the HA endpoint or listener is on another, changing DNS alone would not fix that. Your app may have still been targeting hostname:1433 or just hostname, while the listener expected a different port. It may appear to work if one path still resolved or if SQL Browser/dynamic port behavior temporarily helped, then fail later.

Second, **you redirected DNS to the wrong HA name**. With SQL Server HA, clients should usually connect to the proper **AG listener** or the **FCI network name**, not just any cluster-related IP/name. If someone made the old hostname resolve to a cluster VIP or some other endpoint, the SQL protocol behavior may not match what the client expects.

Third, **DNS caching inside the app, driver, JVM/.NET runtime, node, or OpenShift DNS layer**. One day it resolves one way, the next day a cache expires and the app gets a different IP. If multiple A records or changing listener IPs are involved, the app may connect inconsistently depending on which cached address it uses.

Fourth, **TLS/certificate name mismatch**. If your connection string or driver validates the server certificate, and the app thinks it is connecting to old-primary.company.com but DNS now sends it to ha-listener.company.com, the certificate presented by SQL Server may no longer match the hostname the client used. That often causes "it used to work" symptoms if trust settings, pooled connections, or fallback behavior masked it at first.

Fifth, **MultiSubnetFailover / listener behavior**. If the HA setup spans subnets, some drivers need the connection string configured correctly for AG listeners. Without the appropriate settings, failover or reconnection can become slow or fail unpredictably. DNS tricks do not replace proper listener-aware client configuration.

Sixth, **read/write routing or replica role issues**. If the new target sometimes lands on a secondary replica that does not allow the kind of connection your app is making, it may work during one phase and fail later after a failover or route change. This is especially likely if the redirection bypassed the intended SQL listener semantics.

Seventh, **stale connection pooling**. Your app may have held pooled connections to the original server, so things looked fine immediately after the DNS change. The next day, after pods recycled, pools expired, or new sockets were opened, fresh connections started using the new resolution and failures showed up.

Eighth, **firewall or NetworkPolicy asymmetry**. The original primary may have been reachable from the cluster, but the HA endpoint or listener VIP may not have been fully allowed from all worker nodes or from all relevant paths. Initial success from one pod/node does not guarantee steady success everywhere.

Ninth, **SQL Browser / dynamic port dependence**. If the old target was a default instance on 1433 and the HA target depends on SQL Browser or a named instance with a dynamic port, DNS redirection alone will be fragile. OpenShift egress rules or firewall rules may allow TCP 1433 but not UDP 1434 or the dynamically assigned SQL port.

Tenth, **TTL and negative-cache weirdness**. The "worked, then failed the next day" timing strongly suggests some cache boundary was crossed: DNS TTL, JVM DNS cache, .NET DNS refresh, pod restart, CoreDNS cache expiry, node resolver cache, or load balancer ARP/VIP propagation.

The biggest conceptual problem is this:

**DNS aliasing a server name is not the same thing as configuring the client for SQL Server HA.**

For SQL Server HA, the safe pattern is usually:

- point the app at the **actual AG listener** or **FCI virtual network name**
- specify the **correct port**
- ensure the **certificate matches that name**
- use the **right driver settings** for failover behavior
- verify **firewalls and NetworkPolicies**
- avoid "pretending" the old primary hostname is now the HA endpoint

A good troubleshooting checklist would be:

1. From inside a pod, verify what the hostname resolves to now.
    
    - nslookup
    - dig
    - getent hosts

2. Verify the actual reachable SQL port from the pod.
    
    - nc -vz host port
    - telnet host port if available

3. Confirm whether the app is using:
    
    - old hostname
    - listener hostname
    - explicit port
    - encryption
    - certificate validation
    - failover-related connection-string options

4. Check whether the HA target is:
    
    - AG listener
    - FCI network name
    - plain DNS alias
    - load balancer VIP
    - individual node name

5. Compare certificate subject/SANs against the hostname your app uses.
6. Check OpenShift for pod restarts and node placement changes around the time it started failing.
7. Confirm whether the SQL HA environment changed role overnight due to failover or maintenance.

My strongest guess, based on your description, is one of these three:

- the DNS change pointed to the HA environment but **not using the proper listener name/port**
- things initially worked because of **connection pooling or DNS caching**, then broke when fresh connections were made
- the new target exposed a **TLS name mismatch or port mismatch** that was hidden at first

If you paste the connection string format you're using, whether this is AG or FCI, and whether encryption is on, I can narrow this down pretty fast.

