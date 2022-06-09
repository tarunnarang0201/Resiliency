# Resiliency

In distributed environment,failure handling can be difficult because some components fail while others continue to function. It also leads to more complexity when it comes to troubleshooting and debugging.
And then the resiliency plays its role to recover from failures and continue to function. It isn't about avoiding failures but accepting the fact that failures will happen and responding to them in a way that avoids downtime or data loss. 
The goal of resiliency is to return the application to a fully functioning state after a failure.

**Design principles to manage partial failures**
- Use asynchronous communication (for example, message-based communication) across internal microservices. It's highly advisable not to create long chains of synchronous HTTP calls across the internal microservices because that incorrect design will eventually become the main cause of bad outages. The only synchronous call should be the front-end call between client applications and entry-level microservice or API Gateway.
- **Retry** - There can be intermittent network or channel failure which can be avoided by implementing retries in the service calls. These retries should be for a limited number of times and cannot be infinite.
- **Timeout** - Always implements timeouts for each and every network call. The calling client should not wait endlessly for the response from any service instead it should wait for a predefined time limit and once that time is elapsed then it should fail the call.
- **Circuit breaker**- Use circuit breaker pattern where a retry is made to the failing service and after some fixed some of retries if the service is still failing then the circuit breaker is tripped so that further attempts fail immediately i.e. no new call to failing service will be made instead it will be assumed that its failing or is down.
- **Fallback** - if service request fails then provide some fallback logic like return cached data or default data.
- **Bulkhead Isolation** - This policy of Polly in ASP.NET Core allows us to limit the total amount of resources any part of our application can consume so that a failing part of the application does not cause a cascading failure also bringing down other parts of the application.

The code added to this repository covers all these principles:-
![image](https://user-images.githubusercontent.com/67380484/172799801-8299aead-3219-464a-9bbf-ad53a81d2e2b.png)

*Code Section*
