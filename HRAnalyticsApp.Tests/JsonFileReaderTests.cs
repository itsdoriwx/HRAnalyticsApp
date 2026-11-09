using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HRAnalyticsApp.DTOs;
using HRAnalyticsApp.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HRAnalyticsApp.Tests
{
    [TestClass]
    public class JsonFileReaderTests
    {
        private string _tempFilePath = null!;

        [TestInitialize]
        public void Setup()
        {
            _tempFilePath = Path.GetTempFileName();
            var sampleJson = @"[
                {
                    ""departmentName"": ""Software Engineering"",
                    ""employees"": [
                        {
                            ""employeeId"": ""E-01"",
                            ""fullName"": ""Дмитро Сидоренко"",
                            ""timeLogs"": [
                                { ""date"": ""2026-09-01"", ""hoursWorked"": 8.0, ""projectCode"": ""Grammarly"" }
                            ]
                        }
                    ]
                }
            ]";
            File.WriteAllText(_tempFilePath, sampleJson);
        }

        [TestCleanup]
        public void TearDown()
        {
            if (File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }
        }

        [TestMethod]
        public async Task LoadStreamAsync_ShouldReadJsonFileCorrectly()
        {
            var reader = new JsonFileReader();
            var list = new System.Collections.Generic.List<DepartmentDto>();
            await foreach (var item in reader.LoadStreamAsync<DepartmentDto>(_tempFilePath))
            {
                list.Add(item);
            }

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("Software Engineering", list[0].DepartmentName);
            Assert.AreEqual("E-01", list[0].Employees[0].EmployeeId);
        }
    }
}