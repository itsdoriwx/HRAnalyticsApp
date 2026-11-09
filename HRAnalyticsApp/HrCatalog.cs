using System.Collections.Generic;
using System.Threading.Tasks;
using HRAnalyticsApp.Entities;
using HRAnalyticsApp.Repositories;

namespace HRAnalyticsApp
{
    public class HrCatalog
    {
        private readonly IHrRepository _repository;
        public HrCatalog(IHrRepository repository)
        {
            _repository = repository;
        }
        public Task<List<DepartmentEntity>> GetCatalogAsync() => _repository.GetAllDepartmentsAsync();
        public Task AddEmployeeAsync(EmployeeEntity employee) => _repository.AddEmployeeAsync(employee);
        public Task<bool> RemoveEmployeeAsync(string employeeId) => _repository.DeleteEmployeeAsync(employeeId);
    }
}