---
title: 📰 Clean Code in C A Step-by-Step Guide to SonarQube & .NET
source: https://medium.com/@lakshitha_madhuwantha/clean-code-in-c-a-step-by-step-guide-to-sonarqube-net-d345173f2ef2
author:
  - "[[Lakshitha Madhuwantha]]"
published: 2025-12-11
created: 2026-01-07
description: "Clean Code in C#: A Step-by-Step Guide to SonarQube & .NET We’ve all been there. You push code, merge it, and three weeks later, a critical bug surfaces because of a null reference exception deep …"
tags:
  - clippings
updated: 2026-01-07T22:42
uid: a5badcd3-4203-4fad-9835-7ea0fb9c2ca3
---

# 📰 Clean Code in C A Step-by-Step Guide to SonarQube & .NET

![](<_resources/📰 Clean Code in C A Step-by-Step Guide to SonarQube & .NET/1be8e2becd11c575ab2b6e42b869bbbe_MD5.webp>)

We've all been there. You push code, merge it, and three weeks later, a critical bug surfaces because of a null reference exception deep in a utility class. Or perhaps your codebase has slowly become a "spaghetti monster" of technical debt.

SonarQube provides a structured solution to these challenges.

SonarQube is the industry standard for automated code review. It detects bugs, vulnerabilities, and code smells. Today, I'm going to walk you through **setting up SonarQube locally** and **scanning a.NET project** from scratch.

## Prerequisites

Before we start, ensure you have the following installed:

- **.NET SD (Software Development Kit)K** (Core 3.1, 5, 6, 7, or 8)
- **Docker** (This is the easiest way to run the SonarQube server)
- **Java Runtime (JRE)** (Required only for the scanner part. Use Java Runtime Environment (JRE) version 11 or 17.)

## Step 1: Up and Running with SonarQube (The Easy Way)

Instead of downloading and configuring SonarQube manually, use Docker for a quick setup.

Open your terminal and run this single command:

docker run -d — name sonarqube -p 9000:9000 sonarqube:lts-community

**What this does:**

- Downloads the Long Term Support (LTS) community version of SonarQube.
- Runs it in the background (-d).
- Maps the server's port 9000 to your local port 9000.

Give it about 1–2 minutes to initialise.

## Access the Dashboard

1. Open your browser and go to <http://localhost:9000>.
2. Log in with the default credentials:
- **Login:** admin
- **Password:** admin

You will be prompted to change your password immediately.

## Step 2: Create a Project & Generate a Token

Once logged in, you need to set up a project container for your code.

1. Click **"Create Project"** (or "Manually").
2. **Project Key:** Give it a unique name (e.g., my-dotnet-app).
3. **Display Name:** Something descriptive.
4. Click **Set Up**.

## Generate an Analysis Token

SonarQube will ask how you want to analyse your repository. Select **Locally**.

It will then ask you to generate a token. This token replaces your password for security during the scan.

- **Name:** local-scan-token
- **Expires in:** 30 days
- **Click Generate**

> ***Important:*** *Copy this token now! You won't be able to see it again.*

## Step 3: Install the.NET Scanner

To bridge the gap between your C# code and the SonarQube server, you need the **SonarScanner for.NET**. The modern way to install this is as a.NET Global Tool.

Open your command prompt or terminal and run:

Bashdotnet tool install — global dotnet-sonarscanner

*If you already have it installed, run dotnet tool update — global dotnet-sonarscanner to ensure you have the latest version.*

## Step 4: The "Begin — Build — End" Ritual

This is the most critical part for.NET developers. Unlike interpreted languages (like JavaScript), where you scan *after* the code is written, SonarQube for.NET hooks into the **Build** process.

You must execute the commands in this specific order:

1. **Begin:** Tell SonarQube, "I am about to build; get ready to watch."
2. **Build:** Compile your code (MSBuild/Roslyn creates the analysis data here).
3. **End:** Upload the results to the server.

## The Commands

Navigate to your project's root folder (where your.sln or.csproj file is) and run these three commands sequentially:

**1\. Begin the Analysis.** Replace your-project-key and your-generated-token with the values from Step 2.

Bashdotnet sonarscanner begin /k:"my-dotnet-app" /d:sonar.host.url="http://localhost:9000" /d:sonar.login="your-generated-token"

**2\. Build the Application.** It is crucial to do a clean rebuild to ensure all files are analysed.

Bashdotnet build — no-incremental

**3\. End the Analysis.** This step pushes the data to the dashboard.

Bashdotnet sonarscanner end /d:sonar.login="your-generated-token"

## Step 5: Analyse Your Results

Once the end command finishes successfully, go back to [http://localhost:9000](http://localhost:9000/) and refresh your project page.

You will see:

- **Bugs:** Code that is demonstrably wrong (e.g., potential null pointers).
- **Vulnerabilities:** Security risks (for example, SQL injection — where improper code allows database tampering — or hardcoded passwords, which are passwords written directly into source code and can be easily discovered).
- **Code Smells:** Maintainability issues (for example, using confusing variable names, or creating classes — blueprints for objects in programming — that are too large and difficult to manage).
- **Duplications:** Copy-pasted code blocks, which are sections of code that appear more than once within your codebase and can lead to maintenance challenges.

## Quick Tip: The "Quality Gate"

By default, SonarQube applies a "Quality Gate." This is a set of rules that the code must pass. If your new code has bugs or low test coverage, the gate will fail. This is excellent for setting up CI/CD (Continuous Integration/Continuous Deployment) pipelines later — you can block Pull Requests (requests to merge code branches) that don't pass the Quality Gate!

Adding SonarQubento your workflow takesminutes and offers huge value by guiding code quality.s.

**Once you master local scans, add SonarQube to your CI/CD pipeline for automated checks.**.

Happy Coding!
