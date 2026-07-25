using Microsoft.AspNetCore.Mvc;

namespace Products.Controllers;

public class ProductsController : Controller
{
    private ProductRepository _repository;

    // This would usually be from a Repository/Data Store

    public ProductsController()
    {
        _repository = ProductRepository.GetInstance();
    }

    [HttpPost]
    [Route("/products")]
    public IActionResult Create([FromBody] Product product)
    {
        var response = _repository.AddProduct(product);
        return StatusCode(201, response);
    }

    [HttpPut]
    [Route("/products/{id}")]
    public IActionResult Update(string id, [FromBody] Product product)
    {
        if (product != null)
        {
            var response = _repository.UpdateProduct(product);
            return new JsonResult(response);
        }
        return new NotFoundResult();
    }

    [HttpDelete]
    [Route("/products/{id}")]
    public IActionResult Delete(string id, [FromBody] Product product)
    {
        if (product != null)
        {
            var response = _repository.DeleteProduct(product);
            return new JsonResult(response);
        }
        return new NotFoundResult();
    }
}
