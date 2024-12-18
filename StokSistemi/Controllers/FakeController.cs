using Bogus;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using StokSistemi.Data;
using StokSistemi.Models;

public class FakeController : Controller
{
    private readonly UserManager<Customer> _userManager;
    private readonly ApplicationDbContext _context; // YourDbContext sýnýfýnýzý buraya ekleyin

    public FakeController(UserManager<Customer> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context; // Veritabaný baðlamýný baþlat
    }

    public async Task<List<Customer>> GenerateFakeCustomers()
    {
        var faker = new Faker<Customer>()
            .RuleFor(c => c.Id, f => Guid.NewGuid().ToString())
            .RuleFor(c => c.UserName, f => Regex.Replace(f.Internet.UserName(), "[^a-zA-Z0-9]", ""))
            .RuleFor(c => c.Budget, f => f.Random.Decimal(500, 3000))
            .RuleFor(c => c.TotalSpent, 0)
            .RuleFor(c => c.PriorityScore, 0)
            .RuleFor(c => c.WaitingTime, new TimeSpan(2, 0, 0))
            .RuleFor(c => c.CustomerType, (f, c) => c.Budget > 2000 ? "Premium" : "Standard")
            .RuleFor(c => c.EnqueueTime, new DateTime(2024, 1, 1, 2, 0, 0));

        var randomizer = new Randomizer();
        int customerCount = randomizer.Int(5, 10);
        var customers = faker.Generate(customerCount);

        EnsureMinimumPremiumCustomers(customers);

        foreach (var customer in customers)
        {
            customer.Id = Guid.NewGuid().ToString();

            // Benzersiz ID üretme
            while (await _userManager.FindByIdAsync(customer.Id) != null)
            {
                customer.Id = Guid.NewGuid().ToString();
            }

            var existingUserByName = await _userManager.FindByNameAsync(customer.UserName);

            if (existingUserByName == null)
            {
                var result = await _userManager.CreateAsync(customer, "Test1234!");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(customer, "User");
                    _context.Customers.Add(customer);
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"Error creating user {customer.UserName}: {error.Description}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"User {customer.UserName} already exists.");
            }
        }

        await _context.SaveChangesAsync();

        return customers;
    }





    private void EnsureMinimumPremiumCustomers(List<Customer> customers)
    {
        int premiumCount = customers.Count(c => c.CustomerType == "Premium");

        if (premiumCount < 2)
        {
            // Premium müþteri sayýsýný artýr
            var standardCustomers = customers.Where(c => c.CustomerType == "Standard").Take(2 - premiumCount).ToList();
            foreach (var customer in standardCustomers)
            {
                customer.Budget = 2500; // Premium yapmak için bütçeyi artýr
                customer.CustomerType = "Premium";
            }
        }
    }

    public IActionResult Index()
    {
        return View();
    }
}
