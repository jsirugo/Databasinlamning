using System;

namespace Northwind2023_Johannes_Sirugo
{
    public class Customer
    {
        public string CustomerId { get; set; } 
        public string CompanyName { get; set; }
        public string ContactName { get; set; }
        public string ContactTitle { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string Phone { get; set; }
        public string Fax { get; set; }
        

      
        public Customer(string contactName, string companyName)   //construktor för att få generateacronym att fungera
        {
            ContactName = contactName;
            CompanyName = companyName;
            CustomerId = GenerateAcronym(CompanyName);
        }

        public static string GenerateAcronym(string name)
         {
         string[] words = name.Split(' ');
         string acronym = "";

         foreach (string word in words)
            {
                 int remainingLength = 5 - acronym.Length;

                 if (remainingLength <= 0)
                   break;
    
               int length = Math.Min(remainingLength, word.Length);
               acronym += word.Substring(0, length);
              }
    
            // Pad or truncate to ensure exactly 5 characters
                return acronym.PadRight(5).Substring(0, 5).ToUpper();
}


    }

}
