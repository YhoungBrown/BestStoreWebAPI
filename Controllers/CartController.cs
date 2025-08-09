using BestStoreApi.Models;
using BestStoreApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BestStoreApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ApplicationDbContext Context;
        public CartController(ApplicationDbContext context) 
        { 
            this.Context = context;
        }


        [HttpGet("PaymentMethods")]
        public IActionResult GetPaymentMethods()
        {
            var paymentMethods = OrderHelper.PaymentMethods;
            return Ok(paymentMethods);
        }


        [HttpGet]
        public IActionResult GetCart(string productidentifiers)
        {
            CartDto cartDto = new CartDto
            {
                CartItems = new List<CartItemDto>(),
                SubTotal = 0,
                ShippingFee = OrderHelper.ShippingFee,
                TotalPrice = 0
            };

            var productDictionary = OrderHelper.GetProductDictionary(productidentifiers);

            foreach (var pair in productDictionary)
            { 
                int productid = pair.Key;

                var product = Context.Products.Find(productid);
                if (product == null)
                { 
                    continue;
                }

                CartItemDto cartItemDto = new()
                {
                    Product = product,
                    Quantity = pair.Value,
                };

                cartDto.CartItems.Add(cartItemDto);
                cartDto.SubTotal += product.Price * pair.Value;
                cartDto.TotalPrice = cartDto.SubTotal + cartDto.ShippingFee;
            }

            return Ok(cartDto);
        }
    }
}
