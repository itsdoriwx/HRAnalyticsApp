using System.IO;
using System.Threading.Tasks;
using HRAnalyticsApp.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HRAnalyticsApp.Tests
{
    [TestClass]
    public class DbInitializerTests
    {
        private SqliteConnection _connection = null!;
        private DbContextOptions<AppDbContext> _options = null!;
        private string _tempFilePath = null!;

        [TestInitialize]
        public void Setup()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            _tempFilePath = Path.GetTempFileName();
            var json = @"[
                {
                    ""departmentName"": ""Testing Department"",
                    ""employees"": [
                        {
                            ""employeeId"": ""E-99"",
                            ""fullName"": ""Іван Іваненко"",
                            ""timeLogs"": [
                                { ""date"": ""2026-09-01"", ""hoursWorked"": 8.0, ""projectCode"": ""Ajax Systems"" }
                            ]
                        }
                    ]
                }
            ]";
            File.WriteAllText(_tempFilePath, json);
        }

        [TestCleanup]
        public void TearDown()
        {
            _connection.Close();
            if (File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }
        }

        [TestMethod]
        public async Task InitializeAsync_ShouldPopulateDatabaseCorrectly()
        {
 
            await using var db = new AppDbContext(_options);
            var reader = new JsonFileReader();
            var initializer = new DbInitializer(db, reader);

            var summary = await initializer.InitializeAsync(_tempFilePath);

            Assert.AreEqual(1, summary.Departments);
            Assert.AreEqual(1, summary.Employees);
            Assert.AreEqual(1, summary.TimeLogs);
            Assert.AreEqual(0, summary.SkippedLogs);

            var deptInDb = await db.Departments.FirstOrDefaultAsync();
            Assert.IsNotNull(deptInDb);
            Assert.AreEqual("Testing Department", deptInDb.DepartmentName);
        }
    }
}