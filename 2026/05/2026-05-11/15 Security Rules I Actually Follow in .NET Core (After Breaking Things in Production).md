---
title: 15 Security Rules I Actually Follow in .NET Core (After Breaking Things in Production)
source: https://medium.com/lets-code-future/15-security-rules-i-actually-follow-in-net-core-after-breaking-things-in-production-707ec1b89aff
author:
  - "[[CodeWithYog]]"
published: 2026-04-09
created: '2026-05-11T00:00:00+00:00'
description: Not theory. These are the fixes I added after seeing real bugs, leaks, and late-night incidents.
tags:
  - clippings
uid: 3289d066-6478-4040-9457-276659166782
modified: '2026-05-11T19:39:05+19:39'
---
# 15 Security Rules I Actually Follow in .NET Core (After Breaking Things in Production)
## Not theory. These are the fixes I added after seeing real bugs, leaks, and late-night incidents.

![](https://miro.medium.com/v2/resize:fit:1400/format:webp/0*7UIHVBYiuKJqLBQ0)

Photo by Eric Prouzet on Unsplash

> [Non medium members, click here…](https://medium.com/@CodeWithYog/707ec1b89aff?sk=undefined)

I still remember the first time a simple API mistake exposed more data than it should.

Nothing dramatic. No hacker movie moment. Just a quiet bug sitting in production.  
And the worst part? It passed all tests.  
That day changed how I write APIs in ASP.NET Core. Security stopped being a checklist. It became a habit.

This post is not a copy of documentation. It is what I now do by default in every project.

## 1\. Force HTTPS. No Exceptions.

If your API still allows HTTP, you are leaving the door open.

**Implementation**

```c
app.UseHttpsRedirection();
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});
```

**What I learned**  
One missed redirect can expose tokens in plain text traffic.

## 2\. Protect Endpoints With Authentication

Every endpoint that matters must require identity.

**Implementation**

```c
[Authorize]
[HttpGet("secure-data")]
public IActionResult GetSecureData()
{
    return Ok("This is protected");
}
```

JWT setup:

```c
builder.Services.AddAuthentication("Bearer")
.AddJwtBearer("Bearer", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true
    };
});

app.MapControllers().RequireAuthorization();
```

## [APIsec University - Free API Security Training](https://www.apisecuniversity.com/courses/api-security-fundamentals?source=post_page-----707ec1b89aff---------------------------------------)

### Learn API security and cybersecurity with free courses from APIsec University. Join over 100,000 students in mastering…

www.apisecuniversity.com

## 3\. Validate Tokens Properly

We have a token that doesn’t mean it’s valid.

**Implementation**

```c
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidIssuer = "your-issuer",
    ValidAudience = "your-audience",
    IssuerSigningKey = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes("your-secret-key")),
    ClockSkew = TimeSpan.Zero
};
```

**Real mistake**  
Leaving default clock skew allowed expired tokens to work longer than expected.

## 4\. Use Role or Policy Based Authorization

Hardcoding checks inside controllers creates hidden bugs.

**Implementation**

```c
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
});

[Authorize(Policy = "AdminOnly")]
public IActionResult AdminPanel()
{
    return Ok();
}
```

## 5\. Validate Every Input

Never trust input. Even from your own frontend.

**Implementation**

```c
public class CreateUserDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    [StringLength(50)]
    public string Name { get; set; }
}
```

## 6\. Use DTOs. Always.

Entities should never travel outside your API.

**Implementation**

```c
public class UserDto
{
    public string Name { get; set; }
    public string Email { get; set; }
}
```

Map manually or use AutoMapper.

**What this prevents**  
Over-posting attacks and hidden fields getting updated.

## 7\. Lock Down CORS

Open CORS is equal to open API abuse.

**Implementation**

```c
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        policy.WithOrigins("https://yourfrontend.com")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
app.UseCors("AllowedOrigins");
```

## 8\. Hide Detailed Errors in Production

Stack traces should never reach users.

**Implementation**

```c
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}
```

## 9\. Add Rate Limiting

Bots do not get tired. Your API should slow them down.

**Implementation (.NET 7+)**

```c
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
    });
});
app.UseRateLimiter();
```

## 10\. Never Log Sensitive Data

Logs are often less protected than your database.

**Bad**

```c
_logger.LogInformation("Token: {token}", token);
```

**Good**

```c
_logger.LogInformation("User logged in");
```

## 11\. Add Security Headers

Headers stop many common attacks before they start.

**Implementation**

```c
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    await next();
});
```

## 12\. Update Packages Regularly

Old packages carry known vulnerabilities.

**Command**

```c
dotnet list package --outdated
```

Then update them.

## 13\. Secure Cookies

Even if you use JWT, cookies still appear in some flows.

**Implementation**

```c
options.Cookie.HttpOnly = true;
options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
options.Cookie.SameSite = SameSiteMode.Strict;
```

## 14\. Remove What You Don’t Use

Unused endpoints are silent risks.

**What I do now**

- Delete old controllers
- Disable test endpoints
- Remove debug routes

## 15\. Enforce Strong Password Rules

Weak passwords break everything else.

**Implementation**

```c
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
});
```

## How I Apply All This in Real Projects

I do not implement these one by one randomly.

I follow a simple order:

```c
Step 1
 Set HTTPS, authentication, and token validation.
Step 2
 Add authorization policies and secure endpoints.
Step 3
 Lock input using DTOs and validation.
Step 4
 Restrict CORS and add headers.
Step 5
 Add rate limiting and logging rules.
Step 6
 Clean unused code and update packages.
```

This is stepwise flow that should be follow to have all the security measures in place.

### Before Deployment“If someone tries to misuse this API today, where would they start?”

Then I close that gap.

## Final Thought

Security is not one feature. It is a mindset. Most issues do not come from complex attacks. They come from small oversights.

I have made those mistakes already.

You do not have to repeat them…