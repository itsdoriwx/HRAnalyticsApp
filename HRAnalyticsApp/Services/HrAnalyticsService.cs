using System;
using System.Collections.Generic;
using System.Linq;
using HRAnalyticsApp.Entities;

namespace HRAnalyticsApp.Services
{
    public record OvertimeStreak( // співробітник перепрацьовував кілька днів поспіль
        string EmployeeId,
        string FullName,
        DateTime StartDate,
        DateTime EndDate,
        int DaysCount,
        double AvgHours);

    public record ProjectStat(string ProjectCode, double TotalHours, int LogsCount);
    public record EmployeeTotalHours(string EmployeeId, string FullName, double TotalHours);
    public record DepartmentStat(string DepartmentName, int EmployeesCount, double TotalHours, double AvgHoursPerLog);
    public class HrAnalyticsService
    {
        public const double DailyNorm = 8.0;   
        public const int MinStreakDays = 3;    // 3 дні = аномалія

        public List<OvertimeStreak> GetOvertimeStreaks(IEnumerable<TimeLogEntity> logs, double norm = DailyNorm, int minDays = MinStreakDays)
        {
            var result = new List<OvertimeStreak>();

            var byEmployee = logs
                .Where(t => t.Employee != null)
                .GroupBy(t => new { t.Employee.EmployeeId, t.Employee.FullName });

            foreach (var emp in byEmployee)
            {
                // години по днях (на випадок кількох записів за один день) + лише дні з перевищенням
                var overDays = emp
                    .GroupBy(t => t.Date.Date)
                    .Select(g => new { Date = g.Key, Hours = g.Sum(t => t.HoursWorked) })
                    .Where(d => d.Hours > norm)
                    .OrderBy(d => d.Date)
                    .ToList();

                // ділимо на серії днів, що йдуть підряд
                var run = new List<(DateTime Date, double Hours)>();
                foreach (var day in overDays)
                {
                    if (run.Count > 0 && day.Date != run[^1].Date.AddDays(1))
                    {
                        AddIfStreak(result, emp.Key.EmployeeId, emp.Key.FullName, run, minDays);
                        run.Clear();
                    }
                    run.Add((day.Date, day.Hours));
                }
                AddIfStreak(result, emp.Key.EmployeeId, emp.Key.FullName, run, minDays);
            }

            return result
                .OrderByDescending(s => s.DaysCount)
                .ThenByDescending(s => s.AvgHours)
                .ToList();
        }

        private static void AddIfStreak(
            List<OvertimeStreak> result, string employeeId, string fullName,
            List<(DateTime Date, double Hours)> run, int minDays)
        {
            if (run.Count < minDays) return;

            result.Add(new OvertimeStreak(
                employeeId,
                fullName,
                run[0].Date,
                run[^1].Date,
                run.Count,
                Math.Round(run.Average(d => d.Hours), 2)));
        }
        public List<ProjectStat> GetProjectStats(IEnumerable<TimeLogEntity> logs)
        {
            return logs
                .GroupBy(t => t.ProjectCode)
                .Select(g => new ProjectStat(
                    g.Key,
                    g.Sum(t => t.HoursWorked),
                    g.Count()))
                .OrderByDescending(p => p.TotalHours)
                .ToList();
        }
        public List<EmployeeTotalHours> GetEmployeeTotalHours(IEnumerable<EmployeeEntity> employees)
        {
            return employees
                .Select(e => new EmployeeTotalHours(
                    e.EmployeeId,
                    e.FullName,
                    e.TimeLogs.Sum(t => t.HoursWorked)))
                .OrderByDescending(e => e.TotalHours)
                .ToList();
        }
        public List<DepartmentStat> GetDepartmentStats(IEnumerable<TimeLogEntity> logs)
        {
            return logs
                .Where(t => t.Employee?.Department != null)
                .GroupBy(t => t.Employee.Department.DepartmentName)
                .Select(g => new DepartmentStat(
                    g.Key,
                    g.Select(t => t.Employee.EmployeeId).Distinct().Count(),
                    g.Sum(t => t.HoursWorked),
                    Math.Round(g.Average(t => t.HoursWorked), 2)))
                .OrderByDescending(d => d.TotalHours)
                .ToList();
        }
    }
}