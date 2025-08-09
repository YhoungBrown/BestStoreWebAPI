using BestStoreApi.Models;
using BestStoreApi.Services;
using EllipticCurve.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BestStoreApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext Context;
        private readonly IWebHostEnvironment env;

        private readonly List<string> categorylist = new List<string>
        {
            "Phones",
            "Computers",
            "Accessories",
            "Printers",
            "Cameras",
            "Others"
        };

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
              this.Context = context;
            this.env = env;
        }


        [HttpGet("Categories")]
        public IActionResult GetCategories()
        {
            return Ok(categorylist);
        }


        [HttpGet]
        public IActionResult GetProducts(string ? search, string? category, int? minPrice, int? MaxPrice, string? sort, string? order, int? page)
        {
            IQueryable<Product> query = Context.Products;

            //search functionality
            if (search != null)
            { 
                query = query.Where(p => p.Name.Contains(search) || p.Brand.Contains(search) || p.Description.Contains(search));
            }

            if (category != null)
            { 
                query = query.Where(p => p.Category == category);
            }

            if (minPrice != null)
            { 
                query = query.Where(p => p.Price >= minPrice);
            }

            if (MaxPrice != null)
            { 
                query = query.Where(p => p.Price <= MaxPrice);
            }

            //sort functionality
            if (sort == null) sort = "id";
            if(order == null || order != "asc") order = "desc";
                
            //sorting by name
            if (sort.ToLower() == "name")
            {
                if (order == "asc")
                {
                    query = query.OrderBy(p => p.Name);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Name);
                }
            }
            else if (sort.ToLower() == "brand")
            {
                if (order == "asc")
                {
                    query = query.OrderBy(p => p.Brand);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Name);
                }
            }
            else if (sort.ToLower() == "category")
            {
                if (order == "asc")
                {
                    query = query.OrderBy(p => p.Category);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Category);
                }
            }
            else if (sort.ToLower() == "price")
            {
                if (order == "asc")
                {
                    query = query.OrderBy(p => p.Price);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Price);
                }
            }
            else if (sort.ToLower() == "date")
            {
                if (order == "asc")
                {
                    query = query.OrderBy(p => p.CreatedAt);
                }
                else
                {
                    query = query.OrderByDescending(p => p.CreatedAt);
                }
            }
            else 
            {
                if (order == "asc")
                {
                    query = query.OrderBy(p => p.Id);
                }
                else
                {
                    query = query.OrderByDescending(p => p.Id);
                }
            }

            //pagination functionality

            if (page == null || page < 1) page = 1;

            int pageSize = 5;
            int totalPages = 0;

            decimal count = query.Count();
            totalPages = (int)Math.Ceiling(count / pageSize);

            query = query.Skip((int)(page - 1) * pageSize).Take(pageSize);


            var products = query.ToList();

            var response = new
            {
                Products = products,
                TotalPages = totalPages,
                CurrentPage = page,
                PageSize = pageSize
            };
            return Ok(response);
        }

        [HttpGet("{id}")]
        public IActionResult GetProduct(int id)
        {
           var product = Context.Products.Find(id);

            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }


        [Authorize(Roles = "admin")]
        [HttpPost]
        public IActionResult CreateProduct([FromForm] ProductDto productDto)
        {
            if (!categorylist.Contains(productDto.Category))
            { 
                ModelState.AddModelError("Category", "Invalid category.");
                return BadRequest(ModelState);
            }

            if (productDto.ImageFile == null)
            { 
                ModelState.AddModelError("ImageFile", "Image file is required.");
                return BadRequest(ModelState);
            }

            try {

                //save the image on the server
                string imageFileName = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
                imageFileName += Path.GetExtension(productDto.ImageFile.FileName);
                // Define the absolute path to the wwwroot folder, we need the env declared in our constructor that is an inbuilt dependency
                string imagesFolder = env.WebRootPath + "/images/products/";

                using (var stream = System.IO.File.Create(imagesFolder + imageFileName))
                {
                    productDto.ImageFile.CopyTo(stream);
                }

                //string imagesFolder = Path.Combine(env.WebRootPath, "images", "products");
                //Directory.CreateDirectory(imagesFolder); // Ensures it exists
                //string filePath = Path.Combine(imagesFolder, imageFileName);
                //using var stream = System.IO.File.Create(filePath);
                //productDto.ImageFile.CopyTo(stream);



                //save the product to the database

                Product product = new Product()
                {
                    Name = productDto.Name,
                    Brand = productDto.Brand,
                    Category = productDto.Category,
                    Price = productDto.Price,
                    Description = productDto.Description ?? "",
                    ImageFileName = imageFileName
                };

                Context.Products.Add(product);
                Context.SaveChanges();

                return Ok(product);

            } catch (Exception ex)
            {
                ModelState.AddModelError("ImageFile", "Error saving image file: " + ex.Message);
                return BadRequest(ModelState);
            }
        }


        [Authorize(Roles = "admin")]
        [HttpPut ("{id}")]
        public IActionResult UpdateProduct(int id, [FromForm] ProductDto productDto)
        {
            if (!categorylist.Contains(productDto.Category))
            {
                ModelState.AddModelError("Category", "Invalid category.");
                return BadRequest(ModelState);
            }

            var product = Context.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            string imageFileName = product.ImageFileName;
            if (productDto.ImageFile != null) 
            {
                //save the new image on the server
                imageFileName = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
                imageFileName += Path.GetExtension(productDto.ImageFile.FileName);

                string imageFolder = env.WebRootPath + "/images/products/";
                using (var stream = System.IO.File.Create(imageFolder + imageFileName))
                {
                    productDto.ImageFile.CopyTo(stream);
                }

                //delete the previous image from the server
                System.IO.File.Delete(imageFolder + product.ImageFileName);
            }

            //updating the product in the database
            product.Name = productDto.Name;
            product.Brand = productDto.Brand;
            product.Category = productDto.Category;
            product.Price = productDto.Price;
            product.Description = productDto.Description ?? "";
            product.ImageFileName = imageFileName;


            Context.SaveChanges();
            return Ok(product);
        }


        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public IActionResult DeleteProduct(int id)
        {
            var product = Context.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            //delete the image from the database
            string imageFolder = env.WebRootPath + "/images/products/";
            System.IO.File.Delete(imageFolder + product.ImageFileName);

            //delete the image from the server
            Context.Products.Remove(product);
            Context.SaveChanges();
            return Ok();
        }
    }
}
