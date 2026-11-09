using System;
using System.Collections.Generic;
using HRAnalyticsApp.Entities;
using HRAnalyticsApp.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HRAnalyticsApp.Tests
{
    [TestClass]
    public class HrAnalyticsServiceTests
    {
        private HrAnalyticsService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _service = new HrAnalyticsService();
        }

        [TestMethod]
        public void GetOvertimeStreaks_ShouldDetectStreak_WhenEmployeeWorksOver8HoursThreeDaysInARow()
        {
            // Arrange
            var emp = new EmployeeEntity
            {
                EmployeeId = "E-01",
                FullName = "Дмитро Сидоренко"
            };

            var logs = new List<TimeLogEntity>
            {
                new() { Employee = emp, Date = new DateTime(2026, 9, 1), HoursWorked = 9.0 },
                new() { Employee = emp, Date = new DateTime(2026, 9, 2), HoursWorked = 10.0 },
                new() { Employee = emp, Date = new DateTime(2026, 9, 3), HoursWorked = 9.5 }
            };

            // Act
            var result = _service.GetOvertimeStreaks(logs);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("E-01", result[0].EmployeeId);
            Assert.AreEqual(3, result[0].DaysCount);
            Assert.AreEqual(9.5, result[0].AvgHours);
        }

        [TestMethod]
        public void GetOvertimeStreaks_ShouldIgnore_WhenStreakIsLessThanMinDays()
        {
            // Arrange
            var emp = new EmployeeEntity { EmployeeId = "E-02", FullName = "Олена Кравченко" };

            var logs = new List<TimeLogEntity>
            {
                new() { Employee = emp, Date = new DateTime(2026, 9, 1), HoursWorked = 9.0 },
                new() { Employee = emp, Date = new DateTime(2026, 9, 2), HoursWorked = 10.0 }
            };

            var result = _service.GetOvertimeStreaks(logs);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void GetProjectStats_ShouldAggregateHoursAndCountCorrectly()
        {
            var logs = new List<TimeLogEntity>
            {
                new() { ProjectCode = "Grammarly", HoursWorked = 8.0 },
                new() { ProjectCode = "Grammarly", HoursWorked = 4.0 },
                new() { ProjectCode = "MEGOGO", HoursWorked = 6.0 }
            };

            var result = _service.GetProjectStats(logs);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("Grammarly", result[0].ProjectCode);
            Assert.AreEqual(12.0, result[0].TotalHours);
            Assert.AreEqual(2, result[0].LogsCount);
        }

        [TestMethod]
        public void GetEmployeeTotalHours_ShouldCalculateTotalHoursPerEmployee()
        {
            var employees = new List<EmployeeEntity>
            {
                new()
                {
                    EmployeeId = "E-01",
                    FullName = "Дмитро Сидоренко",
                    TimeLogs = new List<TimeLogEntity>
                    {
                        new() { HoursWorked = 8.0 },
                        new() { HoursWorked = 7.5 }
                    }
                }
            };

            var result = _service.GetEmployeeTotalHours(employees);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(15.5, result[0].TotalHours);
        }

        [TestMethod]
        public void GetDepartmentStats_ShouldGroupAndCalculateAverageCorrectly()
        {
            var dept = new DepartmentEntity { DepartmentName = "Software Engineering" };
            var emp = new EmployeeEntity { EmployeeId = "E-01", Department = dept };

            var logs = new List<TimeLogEntity>
            {
                new() { Employee = emp, HoursWorked = 10.0 },
                new() { Employee = emp, HoursWorked = 6.0 }
            };

            var result = _service.GetDepartmentStats(logs);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Software Engineering", result[0].DepartmentName);
            Assert.AreEqual(16.0, result[0].TotalHours);
            Assert.AreEqual(8.0, result[0].AvgHoursPerLog);
        }
    }
}