using Microsoft.AspNetCore.Mvc;

namespace Products.Controllers;

public class ProductsController : Controller
{
    private ProductRepository _repository;
    private KafkaRequestReplyClient _kafkaClient;

    // This would usually be from a Repository/Data Store

    public ProductsController()
    {
        _repository = ProductRepository.GetInstance();
        _kafkaClient = new KafkaRequestReplyClient();
    }

    [HttpGet]
    [Route("/products")]
    public IActionResult GetAll()
    {
        return new JsonResult(_repository.GetProducts());
    }

    [HttpGet]
    [Route("/product/{id?}")]
    public IActionResult GetSingle(string id)
    {
        var product = _repository.GetProduct(id);
        if (product != null) 
        {
            return new JsonResult(product);
        }
        return new NotFoundResult();
    }

    [HttpPost]
    [Route("/products")]
    public IActionResult Create([FromBody] Product product)
    {
        var response = _kafkaClient.SendProductEventAndWaitForResponse(product, "CREATED");
        return StatusCode(201, response);
    }

    [HttpPut]
    [Route("/products/{id}")]
    public IActionResult Update(string id, [FromBody] Product product)
    {
        if (product == null)
        {
            return new BadRequestResult();
        }

        product.id = id;
        var response = _kafkaClient.SendProductEventAndWaitForResponse(product, "UPDATED");
        return new JsonResult(response);
    }

    [HttpDelete]
    [Route("/products/{id}")]
    public IActionResult Delete(string id)
    {
        var existing = _repository.GetProduct(id);
        if (existing == null)
        {
            return new NotFoundResult();
        }

        var response = _kafkaClient.SendProductEventAndWaitForResponse(existing, "DELETED");
        return new JsonResult(response);
    }
}
