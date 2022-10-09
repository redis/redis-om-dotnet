using Redis.OM.CreateIndexStore.Models;
using Redis.OM.Searching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Redis.OM.CreateIndexStore
{
    internal static class SeedDataHelpers
    {
        public static async Task SeedEmployess(IRedisCollection<Employee> collection)
        {
            #region Seed for employees

            await collection.InsertAsync(new Employee() { Id = "1", Age = 18, EmploymentType = EmploymentType.PartTime, FullName = "Jean Francois" });
            await collection.InsertAsync(new Employee() { Id = "2", Age = 21, EmploymentType = EmploymentType.PartTime, FullName = "Bowman Sophie" });
            await collection.InsertAsync(new Employee() { Id = "3", Age = 78, EmploymentType = EmploymentType.FullTime, FullName = "Will Quinn" });

            #endregion Seed for employees
        }

        public static async Task SeedCustomers(IRedisCollection<Customer> collection)
        {
            #region Seed for customer

            await collection.InsertAsync(new Customer()
            {
                Id = Guid.NewGuid(),
                Email = "Albert.Einstein@hotmail.com",
                FullName = "Albert Einstein",
                Publications = new[] { "Conclusions Drawn from the Phenomena of Capillarity", "Foundations of the General Theory of Relativity", "Investigations of Brownian Motion" },
                Address = new Address("4891 Island Hwy", "Campbell River")
            });
            await collection.InsertAsync(new Customer()
            {
                Id = Guid.NewGuid(),
                Email = "INewton@hotmail.com",
                FullName = "Isaac Newton",
                Publications = new[] { "Philosophiæ Naturalis Principia Mathematica", "Opticks", "De mundi systemate" },
                Address = new Address("2019 90th Avenue", "Delia")
            }); collection.InsertAsync(new Customer()
            {
                Id = Guid.NewGuid(),
                Email = "Galileo.Galilei@gmail.com",
                FullName = "Galileo Galilei",
                Publications = new[] { "Sidereus Nuncius", "The Assayer" }
            });
            await collection.InsertAsync(new Customer()
            {
                Id = Guid.NewGuid(),
                Email = "MarieCurie@princeton.edu",
                FullName = "Marie Curie",
                Publications = new[] { "Recherches sur les substances radioactives", "Traité de Radioactivité", "L'isotopie et les éléments isotopes" },
                Address = new Address("1704 rue Ontario Ouest", "Montréal")
            });

            #endregion Seed for customer
        }

        public static async Task SeedStore(IRedisCollection<Store> collection)
        {
            #region Seed for stores

            await collection.InsertAsync(new Store() { Name = "CF Toronto Eaton Centre", FullAddress = "220 Yonge St, Toronto, ON M5B 2H1", Id = 599 });
            await collection.InsertAsync(new Store() { Name = "Yorkdale Shopping Centre", FullAddress = "3401 Dufferin St, Toronto, ON M6A 2T9", Id = 600 });

            #endregion Seed for stores
        }
    }
}