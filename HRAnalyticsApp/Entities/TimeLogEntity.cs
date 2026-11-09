using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRAnalyticsApp.Entities
{
    public class TimeLogEntity
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public double HoursWorked { get; set; }
        public string ProjectCode { get; set; } = string.Empty;
        public int EmployeeId { get; set; } // зовнішній ключ до Employee
        public EmployeeEntity Employee { get; set; } = null!;
    }
}
