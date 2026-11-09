using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRAnalyticsApp.Entities
{
    public class DepartmentEntity
    {
        public int Id { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public ICollection<EmployeeEntity> Employees { get; set; } = new List<EmployeeEntity>();
    }
}
