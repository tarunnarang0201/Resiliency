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

# Code Explanation

To demonstrate the partial failures management principles in the distributed environment, we have used Polly (http://www.thepollyproject.org/) to work with C# and .NETCore code.

The code has been kept very simple, two different projects have been created by the name of Customer and Order. 

**Customer microservice/project**: has CustomerController class which instantiates the dictionary object with prefilled values of CustomerID and the Customer Name. 
This controller has a function which returns the name when CustomerID is passed. There are some more functions in this controller which are added to reproduce the issue when suppose Order service calls the Customer Service to get the Customer Name, we want ot return Null, or partial failure or no data. How does the implementation of Order Service would handle the partial failures has been explained.

**Order microservice/project**: has the OrderController which calls the CustomerController for the Customer Data and when the data is not returned by Customer service, how does retry, timeout, buldhead, fallback and circuit behavior acts and manage the failures have been explained in this class.

The policies have been created for different cases and applied in the code using Polly, please have a look at the Order Microservice. You may need to configure the URL of the Customer Controller when you call the service from OrderController. 

**Install Polly**: To configure policies of Polly in ASP.NET Core you need to install the Polly package in the project. 
**Install-Package Polly**


**Reference URL**
Other than this, I found a very good documentation by microsoft which talks about using of IHTTPFactory when calling the microservice instead of directly using HttpClient - https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests.






