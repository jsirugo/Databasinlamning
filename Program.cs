using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.Identity.Client;
using System.Threading;

namespace Northwind2023_Johannes_Sirugo
{
    public class Program
    {
        public static string connectionString =
            @"Data Source=LAPTOP-33VKSN3F\SQLEXPRESS01;Initial Catalog=Northwind2023_Johannes_Sirugo;"
             + "Integrated Security=true; TrustServerCertificate=true; MultipleActiveResultSets=true;"; // lade till multipleactiveresultsets för att få delete att fungera

        public static void Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            bool running = true;
            while (running)
            {
                int selectedOption = ShowMenu("What do you want to do?", new[]
                {
                    "Add Customer to database",
                    "Delete Customer",
                    "Update an existing employees adress",
                    "Show sales by country",
                    "Add both a new order and a new customer (Transaction)",
                    "Exit"
                });
                if (selectedOption == 0)
                {
                    Customer newCustomer = GetCustomerDetailsFromUser();
                    AddCustomer(newCustomer);
                }
                if (selectedOption == 1)
                {
                    DeleteCustomer();

                }
                if (selectedOption == 2)
                {
                    UpdateEmployeePrompt();
                }
                if (selectedOption == 5)
                {
                    Console.WriteLine("Bye!");
                    running = false;
                }
                Thread.Sleep(2500);
                Console.Clear();
            }

        }

        public static void AddCustomer(Customer customer)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {


                // sql query för att lägga till ny kund
                string query = "INSERT INTO Customers (CustomerID, CompanyName, ContactName, Address, City, PostalCode, Country, Phone, Fax) " +
                               "VALUES (@CustomerID, @CompanyName, @ContactName, @Address, @City, @PostalCode, @Country, @Phone, @Fax)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CustomerID", customer.CustomerId);
                    command.Parameters.AddWithValue("@CompanyName", customer.CompanyName);
                    command.Parameters.AddWithValue("@ContactName", customer.ContactName);
                    command.Parameters.AddWithValue("@Address", customer.Address);
                    command.Parameters.AddWithValue("@City", customer.City);
                    command.Parameters.AddWithValue("@PostalCode", customer.PostalCode);
                    command.Parameters.AddWithValue("@Country", customer.Country);
                    command.Parameters.AddWithValue("@Phone", customer.Phone);
                    command.Parameters.AddWithValue("@Fax", customer.Fax);

                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        Console.WriteLine("Customer added successfully!");
                    }
                    else
                    {
                        Console.WriteLine("Failed to add customer.");
                    }
                }
                connection.Close(); // för säkerhetens skull, vet att using stänger connection automatiskt men detta är en slags "fallskärm"
            }
        }
        public static Customer GetCustomerDetailsFromUser()

        {

            Customer newCustomer = new Customer("", "CompanyName"); // Ful lösning, fullt medveten, men den gör jobbet.
            Console.WriteLine("Enter Company Name");

            newCustomer.CompanyName = Console.ReadLine();
            newCustomer.CustomerId = Customer.GenerateAcronym(newCustomer.CompanyName);


            while (string.IsNullOrWhiteSpace(newCustomer.CustomerId) || string.IsNullOrWhiteSpace(newCustomer.CompanyName) || newCustomer.CustomerId.Length > 5)
            {
                Console.WriteLine("Customer ID and Company Name are required. Please enter valid values. ID must be 5 characters long.");
                Console.Write("Enter Customer ID: ");
                newCustomer.CustomerId = Console.ReadLine();

                Console.Write("Enter Company Name: ");
                newCustomer.CompanyName = Console.ReadLine();
            }

            Console.Write("Enter Contact Name: ");
            newCustomer.ContactName = Console.ReadLine();

            Console.Write("Enter Address: ");
            newCustomer.Address = Console.ReadLine();

            Console.Write("Enter City: ");
            newCustomer.City = Console.ReadLine();

            Console.Write("Enter Postal Code: ");
            newCustomer.PostalCode = Console.ReadLine();

            Console.Write("Enter Country: ");
            newCustomer.Country = Console.ReadLine();

            Console.Write("Enter Phone: ");
            newCustomer.Phone = Console.ReadLine();

            Console.Write("Enter Fax: ");
            newCustomer.Fax = Console.ReadLine();

            return newCustomer;
        }

        public static void DeleteCustomer()
        {
            Console.WriteLine("Enter the Customer ID or Company Name to delete:");
            string userInput = Console.ReadLine();

            string userInputUpperCase = userInput.ToUpper();
            Console.WriteLine("Attempting to delete customerID:"+userInputUpperCase+"...");

            if (userInput.Length == 5)
            {
                if (DeleteCustomerById(userInputUpperCase))
                {
                    Console.WriteLine($"Customer with ID {userInputUpperCase} deleted successfully.");
                }
                else
                {
                    Console.WriteLine($"No customer with ID: {userInputUpperCase} found.");
                }
            }
            else
            {
                if (DeleteCustomerByName(userInput))
                {
                    Console.WriteLine($"Customer with Company Name {userInput} deleted successfully.");
                }
                else
                {
                    Console.WriteLine($"No customer with Company Name {userInput} found.");
                }
            }
        }

        public static bool DeleteCustomerById(string customerId)
        {

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string customerIDToDelete = customerId;

                int rowsAffected;

                // Kollar efter ordrar relaterade till customerid
                using (SqlCommand checkOrdersCommand = new SqlCommand("SELECT OrderID FROM Orders WHERE CustomerID = @CustomerID", connection))
                {
                    checkOrdersCommand.Parameters.AddWithValue("@CustomerID", customerIDToDelete);

                    using (SqlDataReader reader = checkOrdersCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int orderID = (int)reader["OrderID"]; //typecast till int, hämtar värdet från kolumnen orderID från nuvarande rad i sqldatareader som skickats in ovan

                            // tar bort orderdetaljer relaterade till aktuell order 
                            using (SqlCommand deleteOrderDetailsCommand = new SqlCommand("DELETE FROM [Order Details] WHERE OrderID = @OrderID", connection))
                            {
                                deleteOrderDetailsCommand.Parameters.AddWithValue("@OrderID", orderID);
                                deleteOrderDetailsCommand.ExecuteNonQuery();
                            }
                        }

                        // stänger readern så att nästa command kan köras
                        reader.Close();
                    }
                }

                // tar nu bort orders relaterade till customers
                using (SqlCommand deleteOrdersCommand = new SqlCommand("DELETE FROM Orders WHERE CustomerID = @CustomerID", connection))
                {
                    deleteOrdersCommand.Parameters.AddWithValue("@CustomerID", customerIDToDelete);
                    deleteOrdersCommand.ExecuteNonQuery();
                }

                // NU tas själva kunden bort.
                using (SqlCommand deleteCustomerCommand = new SqlCommand("DELETE FROM Customers WHERE CustomerID = @CustomerID", connection))
                {
                    deleteCustomerCommand.Parameters.AddWithValue("@CustomerID", customerIDToDelete);
                    rowsAffected = deleteCustomerCommand.ExecuteNonQuery();
                }

                return rowsAffected > 0;
            }

        }


        public static bool DeleteCustomerByName(string companyName)
        {
            //**********************************************************************************************************************************
            // medveten om att den är exakt samma som DeleteCustomerById metoden, orkade inte lösa det snyggt. kopierade över och ändrade variabelnamn med andra ord.
            //**********************************************************************************************************************************
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string customerNameToDelete = companyName;

                int rowsAffected;

                // Kollar efter ordrar relaterade till customerName(via customerid)
                using (SqlCommand checkOrdersCommand = new SqlCommand("SELECT OrderID FROM Orders WHERE CustomerID IN (SELECT CustomerID FROM Customers WHERE CompanyName = @CompanyName)", connection))
                {
                    checkOrdersCommand.Parameters.AddWithValue("@CompanyName", customerNameToDelete);

                    using (SqlDataReader reader = checkOrdersCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int orderID = (int)reader["OrderID"]; //typecast till int, hämtar värdet från kolumnen orderID från nuvarande rad i sqldatareader som skickats in ovan

                            // tar bort orderdetaljer relaterade till aktuell order 
                            using (SqlCommand deleteOrderDetailsCommand = new SqlCommand("DELETE FROM [Order Details] WHERE OrderID = @OrderID", connection)) // nytt med paramtern orderid som hämtats ovan
                            {
                                deleteOrderDetailsCommand.Parameters.AddWithValue("@OrderID", orderID);
                                deleteOrderDetailsCommand.ExecuteNonQuery();
                            }
                        }
                    }
                }

                // radera ordrar relaterade till specifierad kund
                using (SqlCommand deleteOrdersCommand = new SqlCommand("DELETE FROM Orders WHERE CustomerID IN (SELECT CustomerID FROM Customers WHERE CompanyName = @CompanyName)", connection))
                {
                    deleteOrdersCommand.Parameters.AddWithValue("@CompanyName", customerNameToDelete);
                    deleteOrdersCommand.ExecuteNonQuery();
                }

                // NU tas customern bort från databasen
                using (SqlCommand deleteCustomerCommand = new SqlCommand("DELETE FROM Customers WHERE CompanyName = @CompanyName", connection))
                {
                    deleteCustomerCommand.Parameters.AddWithValue("@CompanyName", customerNameToDelete);
                    rowsAffected = deleteCustomerCommand.ExecuteNonQuery();
                }

                return rowsAffected > 0;
            }
        }

        public static void UpdateEmployeePrompt()
        {
            Console.Write("Enter the Employee ID: ");
            int employeeID = int.Parse(Console.ReadLine());

            Console.Write("Enter the new Address: ");
            string newAddress = Console.ReadLine();

            bool updateSuccess = UpdateEmployeeAddress(employeeID, newAddress);

            if (updateSuccess)
            {
                Console.WriteLine("Employee address updated successfully.");
            }
            else
            {
                Console.WriteLine("Failed to update employee address. Employee not found or no changes made.");
            }
        }
        public static bool UpdateEmployeeAddress(int employeeID, string newAddress)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                int rowsAffected;


                using (SqlCommand updateEmployeeCommand = new SqlCommand("UPDATE Employees SET Address = @NewAddress WHERE EmployeeID = @EmployeeID", connection))
                {
                    updateEmployeeCommand.Parameters.AddWithValue("@NewAddress", newAddress);
                    updateEmployeeCommand.Parameters.AddWithValue("@EmployeeID", employeeID);
                    rowsAffected = updateEmployeeCommand.ExecuteNonQuery();
                }

                return rowsAffected > 0;
            }
        }



        public static int ShowMenu(string prompt, IEnumerable<string> options)
        {
            if (options == null || options.Count() == 0)
            {
                throw new ArgumentException("Cannot show a menu for an empty list of options.");
            }

            Console.WriteLine(prompt);

            // Hide the cursor that will blink after calling ReadKey.
            Console.CursorVisible = false;

            // Calculate the width of the widest option so we can make them all the same width later.
            int width = options.MaxBy(option => option.Length).Length;

            int selected = 0;
            int top = Console.CursorTop;
            for (int i = 0; i < options.Count(); i++)
            {
                // Start by highlighting the first option.
                if (i == 0)
                {
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.ForegroundColor = ConsoleColor.White;
                }

                var option = options.ElementAt(i);
                // Pad every option to make them the same width, so the highlight is equally wide everywhere.
                Console.WriteLine("- " + option.PadRight(width));

                Console.ResetColor();
            }
            Console.CursorLeft = 0;
            Console.CursorTop = top - 1;

            ConsoleKey? key = null;
            while (key != ConsoleKey.Enter)
            {
                key = Console.ReadKey(intercept: true).Key;

                // First restore the previously selected option so it's not highlighted anymore.
                Console.CursorTop = top + selected;
                string oldOption = options.ElementAt(selected);
                Console.Write("- " + oldOption.PadRight(width));
                Console.CursorLeft = 0;
                Console.ResetColor();

                // Then find the new selected option.
                if (key == ConsoleKey.DownArrow)
                {
                    selected = Math.Min(selected + 1, options.Count() - 1);
                }
                else if (key == ConsoleKey.UpArrow)
                {
                    selected = Math.Max(selected - 1, 0);
                }

                // Finally highlight the new selected option.
                Console.CursorTop = top + selected;
                Console.BackgroundColor = ConsoleColor.Blue;
                Console.ForegroundColor = ConsoleColor.White;
                string newOption = options.ElementAt(selected);
                Console.Write("- " + newOption.PadRight(width));
                Console.CursorLeft = 0;
                // Place the cursor one step above the new selected option so that we can scroll and also see the option above.
                Console.CursorTop = top + selected - 1;
                Console.ResetColor();
            }

            // Afterwards, place the cursor below the menu so we can see whatever comes next.
            Console.CursorTop = top + options.Count();

            // Show the cursor again and return the selected option.
            Console.CursorVisible = true;
            return selected;
        }
    }

}
