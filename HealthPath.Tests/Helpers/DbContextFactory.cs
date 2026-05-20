using HealthPath.API.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace HealthPath.Tests.Helpers
{
    public static class DbContextFactory
    {
        public static HealthpathDbContext Create()
        {
            var options = new DbContextOptionsBuilder<HealthpathDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new HealthpathDbContext(options);
        }
    }
}
