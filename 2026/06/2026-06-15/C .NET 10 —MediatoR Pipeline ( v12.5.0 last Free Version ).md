---
title: "C# .NET 10 —MediatoR Pipeline ( v12.5.0 last Free Version )"
source: "https://medium.com/@gabrieletronchin/c-net-10-mediator-pipeline-v12-5-0-last-free-version-e5e3ac8c1b9d"
author:
  - "[[Gabriele Tronchin]]"
published: 2026-05-10
created: 2026-06-15
description: "More"
tags:
  - "clippings"
---
# C .NET 10 —MediatoR Pipeline ( v12.5.0 last Free Version )
![](https://miro.medium.com/v2/resize:fit:1400/format:webp/0*d5NKz8a-gEiVMzjC.png)

## Introduction

This is a companion article for my MediatR Playground repository, a project I’ve been using to experiment with MediatR features in.NET. Over the past year I wrote a series of articles on Medium, each one covering a different aspect of MediatR pipelines.

You can find them here:

- [**MediatR Pipelines Overview**](https://medium.com/@gabrieletronchin/c-net-8-mediatr-pipelines-edcc9ae8224b)
- [**Unit Of Work Pattern with MediatR Pipelines**](https://medium.com/@gabrieletronchin/c-net-8-unit-of-work-pattern-with-mediatr-pipeline-d7a374df3dcb)
- [**Handle Exceptions with MediatR**](https://medium.com/@gabrieletronchin/c-net-8-handle-exceptions-with-mediatr-48cbf80bae4e)
- [**Exploring Stream Requests and Pipelines**](https://medium.com/@gabrieletronchin/c-net-8-stream-request-and-pipeline-with-mediatr-a26ddb911b39)
- [**Notifications and Notification Publisher**](https://medium.com/@gabrieletronchin/c-net-8-mediatr-notifications-and-notification-publisher-b72a36f0e9ee)
- [**Caching Requests With MediatR Pipeline**](https://blog.devgenius.io/c-net-caching-requests-with-mediatr-pipeline-44a7b92f9978)
- [**Upgrading a Mediator Pipeline Project with Kiro Spec-First**](https://medium.com/@gabrieletronchin/kiro-spec-first-an-ai-support-system-with-semantic-kernel-akka-net-4-akka-net-optimization-72a72896b566)

The project recently got a full refresh: upgraded to.NET 10, all packages updated, proper documentation, tests, and a bunch of fixes.

If you want the full story of how I did that upgrade using Kiro IDE’s spec-first workflow, check out the companion article:

## [C#.NET — Upgrading a Mediator Pipeline Project with Kiro Spec-First](https://medium.com/@gabrieletronchin/c-net-upgrading-a-mediator-pipeline-project-with-kiro-spec-first-02fc30b7ee63?source=post_page-----e5e3ac8c1b9d---------------------------------------)

### Upgrading a MediatR project to.NET 10 with Kiro IDE: testing the spec-first workflow to see how it performs and my…

medium.com

Here I want to focus on the project itself. What’s in it, what each piece does, and why you might find it useful as a reference.

## The GitHub Project

The full source code is available here:

## [GitHub - GabrieleTronchin/MediatRPipelines at net10-mediatr12.5.0](https://github.com/GabrieleTronchin/MediatRPipelines/tree/net10-mediatr12.5.0?source=post_page-----e5e3ac8c1b9d---------------------------------------)

### This repository serves as a playground for exploring MediatR pipelines in.NET. It demonstrates various use cases such…

github.com

Now let me walk you through what the project actually does.

### Pipeline Behaviors

This is the core of the whole project. MediatR pipelines let you intercept the ***request/response*** flow by wrapping logic around the handler execution.

> Think of them like middleware, but for your MediatR requests.

In this project I implemented several pipeline behaviors:

- **Logging***:* measures execution time
- **Validation***:* using FluentValidation to reject bad input before the handler even runs
- **Authorization***:* checks a fake auth service before proceeding

The interesting part is how they’re filtered. Not every behavior should run for every request. I use custom marker interfaces like ***ICommand***, ***IQueryRequest***, and ***ITransactionCommand*** to control which behaviors apply to which request types.

So a query goes through the caching pipeline but skips validation and auth, while a command goes through logging, validation, and auth but skips caching. It’s all driven by generic constraints on the behaviors.

### Unit of Work

The Unit of Work pattern wraps handler execution in a database transaction. The UnitOfWorkBehavior automatically begins a transaction before the handler runs, commits if everything goes well, and rolls back if something throws.

Commands opt into this by implementing ***ITransactionCommand*** instead of ***ICommand***. So when you send an ***AddSampleEntityCommand***, it goes through the transaction pipeline automatically.

The handler just focuses on the business logic, and the pipeline takes care of the transaction lifecycle.

### Exception Handling

MediatR has a built-in mechanism for handling exceptions: ***IRequestExceptionHandler***.

You can register handlers that target a specific request type and exception type combination. When the handler throws, MediatR looks for a matching exception handler and gives it a chance to provide a fallback response.

In this project there are two exception handlers for ***SampleCommand***: one that catches any Exception and one that specifically catches ***InvalidOperationException***.

On top of that, there’s a ***GlobalExceptionHandlingBehavior*** that wraps every request in a try/catch for centralized logging.

### Notifications

MediatR notifications follow a publish-subscribe pattern. You publish a notification and all registered handlers receive it.

The interesting part here is how you control the delivery strategy.

The project implements a custom ***MultipleNotificationPublisher*** that picks the right strategy based on the notification type.

Regular notifications go through sequentially (one handler at a time).

Notifications marked with ***IParallelNotification*** run all handlers in parallel using Task.WhenAll.

And notifications marked with ***IPriorityNotification*** go through a custom publisher that orders handlers by a priority value, so you can control exactly which handler runs first.

### Stream Requests

MediatR supports streaming through ***IStreamRequest*** and ***IAsyncEnumerable***.

Instead of returning a full collection at once, the handler yields results one element at a time.

Stream requests have their own pipeline system. The behaviors operate on the stream itself, processing each element as it flows through.

### Caching Pipeline

Queries implement *IQueryRequest* which includes a CacheKey property.

The ***CachingBehavior*** checks the cache before calling the handler. On a cache hit, it returns immediately without executing anything else in the pipeline. On a miss, it runs the handler and stores the result.

It’s registered as the first behavior in the pipeline chain, so cached responses skip all the other behaviors entirely.

The cache has a short TTL (5 seconds) and fail-safe enabled, meaning it can return a stale value if the handler throws or takes too long.

*By sharing these insights, I hope to create a valuable resource that will help to become more proficient in.NET development.*

*If you enjoyed the content or found it useful, please give a clap to show your support. Your feedback and suggestions will be greatly appreciated as they will help shape the content and ensure it meets the needs of the community.*

*Thank you for reading, and happy coding!*