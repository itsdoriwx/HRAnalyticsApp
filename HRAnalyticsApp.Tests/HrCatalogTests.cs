using System.Collections.Generic;
using System.Threading.Tasks;
using HRAnalyticsApp.Entities;
using HRAnalyticsApp.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HRAnalyticsApp.Tests
{
    [TestClass]
    public class HrCatalogTests
    {
        private SqliteConnection _connection = null!;
        private DbContextOptions<AppDbContext> _options = null!;

        [TestInitialize]
        public void Setup()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;
        }

        [TestCleanup]
        public void TearDown()
        {
            _connection.Close();
        }

        [TestMethod]
        public async Task GetCatalogAsync_ShouldReturnDepartmentsFromRepository()
        {
            await using (var db = new AppDbContext(_options))
            {
                await db.Database.EnsureCreatedAsync();
                db.Departments.Add(new DepartmentEntity { DepartmentName = "DevOps" });
                await db.SaveChangesAsync();
            }

            await using (var db = new AppDbContext(_options))
            {
                var repo = new SqliteHrRepository(db);
                var catalog = new HrCatalog(repo);

                var result = await catalog.GetCatalogAsync();

                Assert.AreEqual(1, result.Count);
                Assert.AreEqual("DevOps", result[0].DepartmentName);
            }
        }
    }
}