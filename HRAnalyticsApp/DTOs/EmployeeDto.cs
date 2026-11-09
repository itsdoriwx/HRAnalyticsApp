using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRAnalyticsApp.DTOs
{
    public class EmployeeDto
    {
        public string EmployeeId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public List<TimeLogDto> TimeLogs { get; set; } = new List<TimeLogDto>();
    }
}

