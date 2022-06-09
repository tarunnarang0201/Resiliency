using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerPolly.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController: ControllerBase
    {
        private Dictionary<int, string> customerNameDict = null;
        private readonly ILogger<CustomerController> _logger;
        public CustomerController(ILogger<CustomerController> logger)
        {
            this._logger = logger;
            if (customerNameDict==null)
            {
                customerNameDict = new Dictionary<int, string>();
                customerNameDict.Add(1, "Katty Perry");
                customerNameDict.Add(2, "Jason Statham");
                customerNameDict.Add(3, "Jim Carrey");
                customerNameDict.Add(4, "Cameron Diaz");
            }
        }

        [HttpGet("GetCustomerName/{customerId}")]
        public ActionResult<string> GetCustomerName(int customerId)
        {
            if (customerNameDict.TryGetValue(customerId, out string value))
            {
                value = customerNameDict[customerId];
            }
            else {
                value = "Customer Not found";
            }
            return value;

        }

        [HttpGet("GetCustomerNameRandomFailureForRetry/{customerId}")]
        public ActionResult<string> GetCustomerNameRandomFailureForRetry(int customerId)
        {
            // ---------------------- To reproduce random failure of service - to check Retry Service-----------------
            Random random = new Random();
            int randomNumber = random.Next(1, 10);

            if (randomNumber % 2 == 0)
            {
                throw new Exception("Service not available");
            }
            // ------------------------------------------------------------------------------

            if (customerNameDict.TryGetValue(customerId, out string value))
            {
                value = customerNameDict[customerId];
            }
            else
            {
                value = "Customer Not found";
            }
            return value;

        }

        [HttpGet("GetCustomerNameAddingDelayForTimeout/{customerId}")]
        public ActionResult<string> GetCustomerNameAddingDelayForTimeout(int customerId)
        {
            // ---------------Adding the delay to the service to check Timeout pattern---------------
            // Delay of 2 minutes
            Thread.Sleep(new TimeSpan(0, 2, 0));
            if (customerNameDict.TryGetValue(customerId, out string value))
            {
                value = customerNameDict[customerId];
            }
            else
            {
                value = "Customer Not found";
            }
            return value;

        }

        [HttpGet]
        [Route("GetCustomerNameWithPermFailure/{customerId}")]
        public string GetCustomerNameWithPermFailure(int customerId)
        {
            _logger.LogError("############## Permanent Failure Error Occured - Customer Service#######");
            throw new TimeoutException("Service not available");
        }

    }
}
