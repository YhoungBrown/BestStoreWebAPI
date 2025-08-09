namespace BestStoreApi.Services
{
    public class OrderHelper
    {
        public static decimal ShippingFee { get; } = 5;

        public static Dictionary<string, string> PaymentMethods { get; } = new()
        {
            {"Cash", "Cash on Delivery"},
            {"CreditCard", "Credit Card"},
            {"PayPal", "PayPal"}
        };

        public static List<string> PaymentStatuses { get; } = new()
        {
            "Pending",
            "Successful",
            "Failed or cancelled"
        };  

        public static List<string> OrderStatuses { get; } = new()
        {
            "Created",
            "Accepted",
            "Shipped",
            "Delivered",
            "Cancelled",
            "Returned"
        };
        /*
         *Recieves a string of product identifiers seperated by '-' in the format "9-9-7-9-6" 
         *
         * and returns a list of pairs ( dictionary ):
         *  - the pair name is the product identifier (ProductID)
         *  - the pair value is the product quantity (Quantity)
         *  
         *  example:
         *  {
         *      9: 3,
         *      7: 1,
         *      6: 1
         *  }
         * 
         */
        public static Dictionary<int, int> GetProductDictionary(string ProductIdentifiers)
        { 
            var productDictionary = new Dictionary<int, int>();

            if (ProductIdentifiers.Length > 0)
            {
                string[] productIdArray = ProductIdentifiers.Split('-');
                foreach (var productId in productIdArray)
                {
                    try
                    {
                        int id = int.Parse(productId);

                        if (productDictionary.ContainsKey(id))
                        {
                            productDictionary[id] += 1;
                        }
                        else
                        {
                            productDictionary[id] = 1;
                        }
                    }
                    catch (Exception){}
                }
            }

            return productDictionary;
        }
    }
}
