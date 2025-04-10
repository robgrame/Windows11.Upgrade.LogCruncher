using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogMacinator.Data
{
    internal class OperationResult
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Executed { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan Elapsed { get; set; }

        public string ToLogString()
        {
            return $"OperationId: {Id}, Name: {Name}, Executed: {Executed}, StartTime: {StartTime}, EndTime: {EndTime}, Elapsed: {Elapsed}";
        }
    }
}
