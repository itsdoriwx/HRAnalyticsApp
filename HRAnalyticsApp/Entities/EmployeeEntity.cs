using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRAnalyticsApp.Entities
{
    public class EmployeeEntity
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int DepartmentId { get; set; }  // зовнішній ключ для зв'язку з DepartmentEntity
        public DepartmentEntity Department { get; set; } = null!;
        public ICollection<TimeLogEntity> TimeLogs { get; set; } = new List<TimeLogEntity>();
    }
}
