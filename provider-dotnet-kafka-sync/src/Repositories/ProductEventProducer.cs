using System;
using System.Text.RegularExpressions;

namespace Products
{
    public class ProductEventProducer
    {
        public ProductEvent CreateEvent(Product product, string eventType)
        {
            var productId = product.id != null ? product.id : Guid.NewGuid().ToString();
            return new ProductEvent
            (
               productId,
               product.type,
               product.name,
               IncrementVersion(product.version),
               eventType
            );
        }

        private string IncrementVersion(string version)
        {
            int versionNumber = 1;
            if (!string.IsNullOrEmpty(version))
            {
                var match = Regex.Match(version, @"\d+");
                if (match.Success)
                {
                    versionNumber = int.Parse(match.Value) + 1;
                }
            }
            return $"v{versionNumber}";
        }
    }
}