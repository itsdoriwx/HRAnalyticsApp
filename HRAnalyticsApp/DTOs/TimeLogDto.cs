using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRAnalyticsApp.DTOs
{
    public class TimeLogDto
    {
        public string Date { get; set; } = string.Empty;
        public double HoursWorked { get; set; } = 0;
        public string ProjectCode { get; set; } = string.Empty;
    }
}
