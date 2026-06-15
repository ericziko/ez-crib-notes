---
tags: []
title: Idempotency in Distributed Systems Preventing Duplicate Operations - WF Version
created: 2026-06-15T08:20:05-07:00
modified: 2026-06-15T08:22:20-07:00
---

# Idempotency in Distributed Systems Preventing Duplicate Operations - WF Version

## Extracted from building an e-commerce checkout system

We charged the same customer $1,847 three times in 6 seconds.

The retry logic worked perfectly. The payment gateway confirmed each transaction. Our monitoring showed zero errors.

**The customer got three confirmation emails and three charges on their credit card.**

## How We Got Here

October 2025. Building a checkout service for an e-commerce platform.

Standard Spring Boot microservices. REST APIs. PostgreSQL. RabbitMQ for async processing.

The product team wanted one thing: **"Make sure orders never fail due to network issues."**

Simple request. I added retry logic everywhere.

```c
@Retryable(maxAttempts = 3)
public OrderResponse createOrder(OrderRequest request) {
    PaymentResponse payment = paymentService.charge(request.amount);
    Order order = orderRepository.save(new Order(payment));
    emailService.sendConfirmation(order);
    return new OrderResponse(order);
}
```

Deployed Friday. Went home feeling confident.

**Monday morning, our support team had 47 tickets about duplicate charges.**

While writing this article, I realized this is actually one of the same recurring decisions I see engineers face over and over.

## The Investigation

First theory: payment gateway is broken.

Called their support. They showed me the logs:

```c
2025-10-28 14:23:41 - Charge $1,847 - APPROVED
2025-10-28 14:23:44 - Charge $1,847 - APPROVED  
2025-10-28 14:23:47 - Charge $1,847 - APPROVED
```

Three separate successful charges. Three seconds apart.

Me: "That's impossible. We only call payment once per order."

Them: "Your server sent three POST requests. We processed all three."

I checked our application logs:

```c
ERROR: Connection timeout during order creation
WARN: Retrying order creation (attempt 2/3)
ERROR: Connection timeout during order creation  
WARN: Retrying order creation (attempt 3/3)
SUCCESS: Order created successfully
```

**The retries worked. Too well.**

## What Actually Happened

Here's the timeline:

**14:23:41** — User clicks "Place Order"  
**14:23:41** — Our service calls payment gateway  
**14:23:41** — Payment succeeds, returns 200  
**14:23:42** — Database save times out (Postgres was slow)  
**14:23:42** — Spring throws exception  
**14:23:44** — Retry #1: Payment gateway gets charged again  
**14:23:47** — Retry #2: Payment gateway gets charged again  
**14:23:47** — Database finally responds, order saved

**The payment succeeded. The database save failed. The retry charged them again.**

From the payment gateway's perspective, these were three different transactions.

From our perspective, it was one transaction with two retries.

**Both perspectives were technically correct. The customer was fucked either way.**

## The Fix Everyone Gets Wrong

My first solution: "Just don't retry payment operations."

**Wrong.**

The product team was right. Network failures happen. We need retries.

My second solution: "Check if payment exists before retrying."

**Also wrong.**

Race conditions everywhere. What if two retries run at the same time?

```c
// This doesn't work
if (!paymentExists(orderId)) {
    charge(amount);
}
```

Between checking and charging, another thread can charge too.

**The real solution: Make operations idempotent.**

## What Idempotency Actually Means

Idempotent = you can call it multiple times, but it only executes once.

Like a light switch. Flip it on three times. Light is still just "on."

**In distributed systems, this is not automatic. You have to build it.**

The payment gateway needed a unique identifier:

```c
public PaymentResponse charge(String idempotencyKey, Money amount) {
    // If payment with this key exists, return existing result
    Payment existing = paymentRepository.findByKey(idempotencyKey);
    if (existing != null) {
        return new PaymentResponse(existing);
    }
    
    // Otherwise, create new payment
    Payment payment = gateway.charge(amount);
    payment.setIdempotencyKey(idempotencyKey);
    paymentRepository.save(payment);
    
    return new PaymentResponse(payment);
}
```

Now retries are safe:

**Attempt 1:** Key doesn't exist → charge customer  
**Attempt 2:** Key exists → return same payment  
**Attempt 3:** Key exists → return same payment

**One charge. Three attempts. Customer happy.**

## The Part Everyone Forgets

Idempotency keys need to be:

**1\. Unique per operation**
Don't use `orderId`. Use `orderId + operation + timestamp`.
If customer retries checkout, that's a different operation.

**2\. Deterministic**
Generate the key from request data, not random UUID.
Same request = same key = same result.

**3\. Stored reliably**
If key generation fails, you're back to duplicate charges.
I use:

```c
String idempotencyKey = DigestUtils.sha256Hex(
    customerId + productId + amount + timestamp
);
```

**4\. Validated early**
Check for duplicate key BEFORE doing expensive operations.
Don't charge the customer, then realize it was a duplicate.

## Where This Gets Complicated
Idempotency isn't just for payments.
Every external operation in your distributed system needs it:

**Sending emails:**  Don't send three order confirmations because of retries.

**Inventory updates:** Don't decrement stock three times for one purchase.

**Webhook calls:** Don't notify shipping provider three times.

**Database writes:**  Don't create three order records.

**Here's the brutal truth: Most microservices don't handle this.**
They retry. They succeed. They create duplicates.
Then six months later, support team finds 10,000 duplicate records.

## The Mistakes I See Everywhere

### mistake 1: only adding retries, not idempotency
Retries without idempotency = systematic duplicate operations.
**Every retry is a potential duplicate.**

### Mistake 2: Using database transactions as idempotency
Transactions prevent duplicate database writes.
They don't prevent duplicate API calls to external services.
### Mistake 3: Assuming HTTP methods are idempotent
GET and DELETE are supposed to be idempotent.
POST is not.
But if your POST doesn't enforce idempotency, it's still not safe to retry.

### Mistake 4: Generating random idempotency keys
Random UUID = every retry is a new operation.
Defeats the entire purpose.
### Mistake 5: Not documenting which operations need idempotency
Six months later, different engineer adds a retry.
Duplicate operations start appearing.
Nobody knows why.

## How I Actually Implement This Now

**Rule 1: Every external call gets an idempotency key**

Payment gateway, email service, inventory system, webhooks.

No exceptions.

**Rule 2: Store idempotency keys in the database**

Use a separate table:

```c
CREATE TABLE idempotency_keys (
    key VARCHAR(255) PRIMARY KEY,
    operation VARCHAR(100),
    result TEXT,
    created_at TIMESTAMP
);
```

**Rule 3: Fail fast on duplicate keys**

If key exists, return cached result immediately.

Don't even call the external service.

**Rule 4: Set expiration on old keys**

Keep keys for 24 hours, then delete.

Prevents infinite database growth.

**Rule 5: Make it a cross-cutting concern**

Don't implement this 47 times.

Create a reusable annotation or wrapper.

```c
@Idempotent
public PaymentResponse charge(PaymentRequest request) {
    // Framework handles key generation and duplicate detection
}
```

## The Real Cost of Getting This Wrong

That Monday morning with 47 duplicate charge tickets?

**Direct costs:**

- $73,000 in refunds
- 6 engineering days debugging
- Payment gateway fraud review (they thought we were hacked)

**Hidden costs:**

- Customer trust destroyed
- Support team overwhelmed
- PCI compliance audit triggered

**All because I added retries without idempotency.**

## What Actually Matters

Distributed systems fail in creative ways.

Network partitions. Timeouts. Partial failures.

**Retries are mandatory. Idempotency makes them safe.**

Your system will retry operations. Plan for it.

Or your customers will get charged three times.

**One is a feature. The other is a lawsuit.**
