using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HRAnalyticsApp.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRAnalyticsApp.Repositories
{
    public class SqliteHrRepository : IHrRepository
    {
        private readonly AppDbContext _context;
        public SqliteHrRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<DepartmentEntity>> GetAllDepartmentsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Departments
                .Include(d => d.Employees)
                .ThenInclude(e => e.TimeLogs)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<EmployeeEntity?> GetEmployeeByIdAsync(string employeeId, CancellationToken cancellationToken = default)
        {
            return await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.TimeLogs)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId, cancellationToken);
        }

        public async Task AddEmployeeAsync(EmployeeEntity employee, CancellationToken cancellationToken = default)
        {
            await _context.Employees.AddAsync(employee, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> DeleteEmployeeAsync(string employeeId, CancellationToken cancellationToken = default)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId, cancellationToken);

            if (employee == null) return false;

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}