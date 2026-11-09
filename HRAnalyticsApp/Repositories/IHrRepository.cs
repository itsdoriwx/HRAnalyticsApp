using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HRAnalyticsApp.Entities;

namespace HRAnalyticsApp.Repositories
{
    public interface IHrRepository
    {
        Task<List<DepartmentEntity>> GetAllDepartmentsAsync(CancellationToken cancellationToken = default);
        Task<EmployeeEntity?> GetEmployeeByIdAsync(string employeeId, CancellationToken cancellationToken = default);
        Task AddEmployeeAsync(EmployeeEntity employee, CancellationToken cancellationToken = default);
        Task<bool> DeleteEmployeeAsync(string employeeId, CancellationToken cancellationToken = default);
    }
}