using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OrderPolly.Models;
using Polly;
using Polly.Bulkhead;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Retry;
using Polly.Timeout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace OrderPolly.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly ILogger<OrderController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private HttpClient _httpClient;
        private Dictionary<int, OrderDTO> orderDict = null;
        private string apiurlCustomer = @"https://localhost:44383/";

        //--------Polly Policies---------------------
        private readonly RetryPolicy _retryPolicy;
        private TimeoutPolicy _timeoutPolicy;
        private readonly FallbackPolicy<string> _fallbackPolicy;

        private readonly FallbackPolicy _fallbackPolicyVoidCase;

        private static CircuitBreakerPolicy _circuitBreakerPolicy;

        private static BulkheadPolicy _bulkheadPolicy;

        public OrderController(ILogger<OrderController> logger, IHttpClientFactory httpClientFactory)
        {

            //---------------------Configuring the Polly - Retry Policy -------------
            _retryPolicy = Policy
                    .Handle<Exception>()
                    .Retry(5);

            // configuring Timeout Policy of Polly
            _timeoutPolicy = Policy.Timeout(20, TimeoutStrategy.Pessimistic);

            // configuring fallback policy of Polly
            // I am sending TimeoutException from Customer service, however, it throws Aggregate exception- may be it's wrapping 
            // up the exception in Aggregate Exception and that's why we are catching here Aggregate exception, we can catch the generic exception too.
            _fallbackPolicy = Policy<string>.Handle<AggregateException>().Fallback("Customer Name not available - fallback policy");

            // configuring the fallback policy in case some other function need to be called instead of filling with the object
            //MyHandledScenario is the method created in the class below
            _fallbackPolicyVoidCase = Policy.Handle<ArgumentNullException>().Fallback(() => MyHandledScenario());

            // configuring circuit breaker policy, it says if the exception comes for two consecutive times then 
            // circuit will break (open state) and call to called service will not be triggered for 1 minute
            if (_circuitBreakerPolicy == null)
            {
                _circuitBreakerPolicy = Policy.Handle<AggregateException>()
                                                .CircuitBreaker(2, TimeSpan.FromMinutes(1));
            }

            // Configure Buldhead policy
            //This defines that while calling service limit the number of resources to call the service i.e. max 3 parallelizations
            //of executions through the bulkhead & max 6 number of requests that may be queuing (waiting to acquire an execution slot) at any time.
            _bulkheadPolicy = Policy.Bulkhead(3, 6);
            // --------------------------------------------------------------------------


            this._logger = logger;
            this._httpClientFactory = httpClientFactory;

            if (orderDict == null)
            {
                orderDict = new Dictionary<int, OrderDTO>();
                //-------------Initialize Order 1 for Customer 1---------------
                OrderDTO orderDTO = new OrderDTO();
                orderDTO.OrderId = 7629;
                orderDTO.CustomerId = 1;
                orderDTO.ItemList = new List<ItemDTO>();

                orderDTO.ItemList.Add(new ItemDTO
                {
                    ItemId = 1111,
                    Desc = "Book on Music"
                });
                orderDTO.ItemList.Add(new ItemDTO
                {
                    ItemId = 2222,
                    Desc = "Book on Acting"
                });

                // Add this to the dictionary
                orderDict.Add(orderDTO.CustomerId, orderDTO);

                //-------------------Initialize Order2 for Customer 2 ----------------------
                OrderDTO orderDTO2 = new OrderDTO();
                orderDTO2.OrderId = 8589;
                orderDTO2.CustomerId = 2;
                orderDTO2.ItemList = new List<ItemDTO>();

                orderDTO2.ItemList.Add(new ItemDTO
                {
                    ItemId = 33333,
                    Desc = "Book on Fighting"
                });
                orderDTO2.ItemList.Add(new ItemDTO
                {
                    ItemId = 44444,
                    Desc = "Book on Martial arts"
                });
                // Add this to the dictionary
                orderDict.Add(orderDTO2.CustomerId, orderDTO2);

            }
        }

        [HttpGet]
        [Route("GetOrderByCustomer/{customerId}")]
        public OrderDTO GetOrderByCustomer(int customerId)
        {
            _httpClient = _httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri(apiurlCustomer);
            var uri = "/api/Customer/GetCustomerName/" + customerId;
            string nameFromCustomerService = _httpClient.GetStringAsync(uri).Result;

            if (orderDict.TryGetValue(customerId, out OrderDTO orderDTO))
            {
                orderDTO = orderDict[customerId];
                orderDTO.CustomerName = nameFromCustomerService;
            }

            return orderDTO;
        }

        [HttpGet]
        [Route("GetOrderByCustomerUsingRetryPolicy/{customerId}")]
        public OrderDTO GetOrderByCustomerUsingRetryPolicy(int customerId)
        {
            _httpClient = _httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri(apiurlCustomer);
            var uri = "/api/Customer/GetCustomerNameRandomFailureForRetry/" + customerId;

            //---------Retry policy is used here which will retry for two times and stop afterwards.
            string nameFromCustomerService = _retryPolicy.Execute(() => _httpClient.GetStringAsync(uri).Result);

            if (orderDict.TryGetValue(customerId, out OrderDTO orderDTO))
            {
                orderDTO = orderDict[customerId];
                orderDTO.CustomerName = nameFromCustomerService;
            }

            return orderDTO;
        }



        [HttpGet]
        [Route("GetOrderByCustomerUsingTimeoutPolicy/{customerId}")]
        public OrderDTO GetOrderByCustomerUsingTimeoutPolicy(int customerId)
        {
            string nameFromCustomerService = null;
            try
            {
                _httpClient = _httpClientFactory.CreateClient();
                _httpClient.BaseAddress = new Uri(apiurlCustomer);
                var uri = "/api/Customer/GetCustomerNameAddingDelayForTimeout/" + customerId;

                //---------Timeout policy is used which will timeout if the response does not come with in 20 seconds.
                nameFromCustomerService = _timeoutPolicy.Execute(() => _httpClient.GetStringAsync(uri).Result);
            }catch(Exception ex)
            {
                _logger.LogError(ex, "Exception Occured");
                //default behavior
                nameFromCustomerService = "Customer does not exist";
            }

            if (orderDict.TryGetValue(customerId, out OrderDTO orderDTO))
            {
                orderDTO = orderDict[customerId];
                orderDTO.CustomerName = nameFromCustomerService;
            }

            return orderDTO;
        }

        [HttpGet]
        [Route("GetOrderByCustomerFallbackPolicy/{customerId}")]
        public OrderDTO GetOrderByCustomerFallbackPolicy(int customerId)
        {
            string nameFromCustomerService = null;
            _httpClient = _httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri(apiurlCustomer);
            var uri = "/api/Customer/GetCustomerNameWithPermFailure/" + customerId;
            //---------Fallback policy is used to substitute the value with the one defined in the policy.
            nameFromCustomerService = _fallbackPolicy.Execute(() => _httpClient.GetStringAsync(uri).Result);

            if (orderDict.TryGetValue(customerId, out OrderDTO orderDTO))
            {
                orderDTO = orderDict[customerId];
                orderDTO.CustomerName = nameFromCustomerService;
            }

            return orderDTO;
        }

        [HttpGet]
        [Route("GetOrderByCustomerUsingCircuitBreakerPolicy/{customerId}")]
        public OrderDTO GetOrderByCustomerUsingCircuitBreakerPolicy(int customerId)
        {
            string nameFromCustomerService = null;
            try
            {
                _httpClient = _httpClientFactory.CreateClient();
                _httpClient.BaseAddress = new Uri(apiurlCustomer);
                var uri = "/api/Customer/GetCustomerNameWithPermFailure/" + customerId;
                //---------Fallback policy is used to substitute the value with the one defined in the policy.
                nameFromCustomerService = _circuitBreakerPolicy.Execute(() => _httpClient.GetStringAsync(uri).Result);
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Excpetion Occurred");
                nameFromCustomerService = "Customer Name not found - Circuit breaker";
            }
            if (orderDict.TryGetValue(customerId, out OrderDTO orderDTO))
            {
                orderDTO = orderDict[customerId];
                orderDTO.CustomerName = nameFromCustomerService;
            }

            return orderDTO;
        }

        [HttpGet]
        [Route("GetOrderByCustomerUsingBulkHeadPolicy/{customerId}")]
        public OrderDTO GetOrderByCustomerUsingBulkHeadPolicy(int customerId)
        {
            _httpClient = _httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri(apiurlCustomer);
            var uri = "/api/Customer/GetCustomerName/" + customerId;

            //---------Retry policy is used here which will retry for two times and stop afterwards.
            string nameFromCustomerService = _retryPolicy.Execute(() => _httpClient.GetStringAsync(uri).Result);


            if (orderDict.TryGetValue(customerId, out OrderDTO orderDTO))
            {
                orderDTO = orderDict[customerId];
                orderDTO.CustomerName = nameFromCustomerService;
            }

            return orderDTO;
        }

        [HttpGet]
        [Route("GetOrderByCustomerUsingRetryAndFallbackBoth/{customerId}")]
        public OrderDTO GetOrderByCustomerUsingRetryAndFallbackBoth(int customerId)
        {
            _httpClient = _httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri(apiurlCustomer);
            var uri = "/api/Customer/GetCustomerNameWithPermFailure/" + customerId;

            //---------Retry 3 times as per the policy above and then calls fallback
            string nameFromCustomerService = _bulkheadPolicy.Execute(()=>_fallbackPolicy.Execute(() => _retryPolicy.Execute(() => _httpClient.GetStringAsync(uri).Result)));

            if (orderDict.TryGetValue(customerId, out OrderDTO orderDTO))
            {
                orderDTO = orderDict[customerId];
                orderDTO.CustomerName = nameFromCustomerService;
            }

            return orderDTO;
        }


        private void MyHandledScenario()
        {
            Console.WriteLine("Fallback scenario handled....");
        }

    }
}
