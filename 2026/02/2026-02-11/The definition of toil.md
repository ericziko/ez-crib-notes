---
uid: 6f460ec2-7ec0-4a61-aca8-a504cbb6ec96
title: The definition of toil
created: 2026-02-11T10:03:08
modified: 2026-02-11T10:05:10
---

# The definition of toil

## Summary 
When Google talks about **"toil,"** they're not being poetic. They're being diagnostic.

The term comes from Google's Site Reliability Engineering philosophy, especially the book [**Site Reliability Engineering**](chatgpt://generic-entity?number=0).

Here's the definition they use:

> **Toil is manual, repetitive, automatable, tactical work that provides no enduring value and scales linearly as a service grows.**

That sentence is dense. Let's unpack it like engineers.

### Manual

If a human has to click it, type it, or run it over and over — that's a warning sign.

### Repetitive

If you've done it twice the same way, your brain is already screaming, "Why isn't this a script?"

### Automatable

If a machine could do it, but isn't, you're burning human cognitive cycles on something silicon would happily do without complaining.

### Tactical

It's reactive. You're fixing today's symptom instead of improving the system.

### No enduring value

Once you finish it, nothing is fundamentally better. You'll likely have to do it again.

### Scales linearly

This one is the killer. If doubling traffic doubles the amount of human effort required, your system is quietly asking for automation or redesign.

Google's SRE guidance is pretty sharp on this: engineers should spend **less than 50% of their time on toil**, and ideally much less. If toil starts dominating, reliability suffers because engineers don't have time to improve the system.

Now here's the subtle philosophical move Google made: they reframed "hard work" as a smell.

Toil isn't virtuous. It's a systems failure signal.

### Some examples

- Restarting crashed services by hand every week
- Manually provisioning the same environment repeatedly
- Responding to the same class of alert without fixing root cause
- Copy-pasting config updates across servers

Notice something? None of these improve the system. They just keep it limping along.

In contrast, writing automation, improving monitoring, redesigning architecture, eliminating alert noise — those reduce future toil. That's engineering.

The clever part is that "toil" gives teams a shared vocabulary. Instead of saying, "I'm busy," you can say, "This is toil," which implies: we should design our way out of this.

It's almost thermodynamic. Toil is entropy in socio-technical systems. If you don't actively reduce it, it accumulates.

There's a quiet lesson in there:

If your day feels like pushing a rock uphill repeatedly, the system is telling you something.
