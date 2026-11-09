using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using HRAnalyticsApp.DTOs;
using HRAnalyticsApp.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRAnalyticsApp.Services
{
    public record ImportSummary(int Departments, int Employees, int TimeLogs, int SkippedLogs);

    public class DbInitializer
    {
        private readonly AppDbContext _context;   // доступ до БД SQLite
        private readonly IFileLoader _jsonReader; // читач JSON 

        public DbInitializer(AppDbContext context, IFileLoader jsonReader)
        {
            _context = context;
            _jsonReader = jsonReader;
        }
        public async Task<ImportSummary> InitializeAsync(string source, CancellationToken cancellationToken = default)
        {
            await _context.Database.EnsureCreatedAsync(cancellationToken); // БД існує?
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            // очистка, щоб повторний імпорт оновлював дані
            await _context.TimeLogs.ExecuteDeleteAsync(cancellationToken);
            await _context.Employees.ExecuteDeleteAsync(cancellationToken);
            await _context.Departments.ExecuteDeleteAsync(cancellationToken);

            int departments = 0, employees = 0, timeLogs = 0, skipped = 0;

            await foreach (var deptDto in _jsonReader.LoadStreamAsync<DepartmentDto>(source, cancellationToken))
            {
                var deptEntity = new DepartmentEntity
                {
                    DepartmentName = deptDto.DepartmentName
                };

                foreach (var empDto in deptDto.Employees)
                {
                    var empEntity = new EmployeeEntity
                    {
                        EmployeeId = empDto.EmployeeId,
                        FullName = empDto.FullName,
                        Department = deptEntity
                    };

                    foreach (var logDto in empDto.TimeLogs)
                    {
                        if (!DateTime.TryParse(logDto.Date, CultureInfo.InvariantCulture,
                                DateTimeStyles.None, out var parsedDate))
                        {
                            skipped++;
                            continue;
                        }

                        empEntity.TimeLogs.Add(new TimeLogEntity
                        {
                            Date = parsedDate,
                            HoursWorked = logDto.HoursWorked,
                            ProjectCode = logDto.ProjectCode,
                            Employee = empEntity
                        });
                        timeLogs++;
                    }

                    deptEntity.Employees.Add(empEntity);
                    employees++;
                }

                _context.Departments.Add(deptEntity);
                await _context.SaveChangesAsync(cancellationToken);
                _context.ChangeTracker.Clear(); // не накопичуємо сутності в пам'яті
                departments++;
            }

            if (departments == 0)
            {
                throw new InvalidOperationException("Файл не містить жодного департаменту або його не вдалося прочитати.");
            }

            await transaction.CommitAsync(cancellationToken);
            return new ImportSummary(departments, employees, timeLogs, skipped);
        }
    }
}