using BestStoreApi.Models;
using BestStoreApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BestStoreApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {

        private readonly ApplicationDbContext context;
        public OrdersController(ApplicationDbContext context)
        {
            this.context = context;
        }


        [Authorize]
        [HttpGet]
        public IActionResult GetOrders(int? page)
        {
            int userId = JWTReader.GetUserId(User);

            string userRole = JWTReader.GetUserRole(User); //context.Users.Find(userId)?.Role ?? "";

            IQueryable<Order> query = context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product); //thenIncludes means we want to include somthing from inside the OrderItems

            if (userRole != "admin")
            {
                query = query.Where(o => o.UserId == userId);
            }

            query = query.OrderByDescending(o => o.Id);


            //implementing pagination
            if (page == null || page < 1)
            {
                page = 1;
            }

            int pageSize = 5; //number of orders per page
            int totalPages = 0;
            int totalOrders = query.Count();
            totalPages = (int)Math.Ceiling((double)totalOrders / pageSize);

            query = query
                .Skip((page.Value - 1) * pageSize) //skip the previous pages
                .Take(pageSize); //take the current page size

            //reading the orders

            var orders = query.ToList();

            foreach (var order in orders)
            {
                order.User.Password = ""; //remove the password from the user object
                foreach (var item in order.OrderItems)
                {
                    item.Order = null; //getting rid of the object cycle by removing the order from the order items
                }
            }


            var response = new
            {
                Orders = orders,
                pageSize = pageSize,
                TotalPages = totalPages,
                CurrentPage = page.Value
            };

            return Ok(response);
        }



        [Authorize]
        [HttpGet("{id}")]
        public IActionResult GetOrder(int id)
        {
            int userId = JWTReader.GetUserId(User);
            string userRole = JWTReader.GetUserRole(User); //context.Users.Find(userId)?.Role ?? "";

            Order? order = null;

            if (userRole == "admin")
            {
                order = context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefault(o => o.Id == id);
            }
            else
            {
                order = context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefault(o => o.Id == id && o.UserId == userId);
            }

            if (order == null)
            {
                return NotFound($"Order with id : {id} was not found");
            }

            order.User.Password = ""; //remove the password from the user object

            foreach (var item in order.OrderItems)
            {
                item.Order = null; //getting rid of the object cycle by removing the order from the order items
            }


            return Ok(order);
        }



        [Authorize]
        [HttpPost]
        public IActionResult CreateOrder(OrderDto orderDto)
        {
            if (!OrderHelper.PaymentMethods.ContainsKey(orderDto.paymentMethod))
            {
                ModelState.AddModelError("Payment Method", "Please select a valid payment method");
                return BadRequest(ModelState);
            }

            int userId = JWTReader.GetUserId(User);

            var user = context.Users.Find(userId);
            if (user == null)
            {
                ModelState.AddModelError("Order", "unable to place Order");
                return BadRequest(ModelState);
            }

            var productDictionary = OrderHelper.GetProductDictionary(orderDto.productIdentifier);

            //create a new order

            Order order = new()
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                shippingFee = OrderHelper.ShippingFee,
                deliveryAddress = orderDto.deliveryAddress,
                paymentMethod = orderDto.paymentMethod,
                paymentStatus = OrderHelper.PaymentStatuses[0], //pending
                orderStatus = OrderHelper.OrderStatuses[0] //created
            };

            foreach (var pair in productDictionary)
            {
                int productId = pair.Key;
                var product = context.Products.Find(productId);
                if (product == null)
                {
                    ModelState.AddModelError("Product", $"Product with id : {productId} is not available ");
                    return BadRequest(ModelState);
                }

                var orderItem = new OrderItem()
                {
                    ProductId = productId,
                    Quantity = pair.Value,
                    UnitPrice = product.Price
                };

                order.OrderItems.Add(orderItem);
            }

            if (order.OrderItems.Count < 1)
            {
                ModelState.AddModelError("Order", "Unable to place the Order");
                return BadRequest(ModelState);
            }


            //save the order in the database
            context.Orders.Add(order);
            context.SaveChanges();

            foreach (var item in order.OrderItems)
            {
                item.Order = null;
            }

            order.User.Password = "";

            return Ok(order);
        }


        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public IActionResult UpdateOrder(int id, string? paymentStatus, string? OrderStatus)
        {
            if (paymentStatus == null && OrderStatus == null)
            {
                ModelState.AddModelError("UpdateOrder", "There is nothing to update");
                return BadRequest(ModelState);
            }

            if (paymentStatus != null && !OrderHelper.PaymentStatuses.Contains(paymentStatus))
            {
                //the payment status is not valid
                ModelState.AddModelError("PaymentStatus", "The Payment Status is not valid");
                return BadRequest(ModelState);
            }

            if (OrderStatus != null && !OrderHelper.OrderStatuses.Contains(OrderStatus))
            {
                //the order status is not valid
                ModelState.AddModelError("OrderStatus", "The Order Status is not valid");
                return BadRequest(ModelState);
            }



            var order = context.Orders.Find(id);

            if (order == null)
                return NotFound();

            if (paymentStatus != null)
            {
                order.paymentStatus = paymentStatus;
            }

            if (OrderStatus != null)
                order.orderStatus = OrderStatus;

            context.SaveChanges();

            return Ok(order);
        }


        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public IActionResult DeleteOrder(int id)
        {
            var order = context.Orders.Find(id);

            if (order == null)
            {
                return NotFound($"Order with id : {id} was not found");
            }

            context.Orders.Remove(order);
            context.SaveChanges();

            return Ok($"Order with id : {id} was deleted successfully");
        }
    }
}
