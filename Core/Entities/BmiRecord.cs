using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitnessBot.Core.Entities
{
    public class BmiRecord
    {
        public long Id { get; set; }
        public long UserId { get; set; }

        public double HeightCm { get; set; }
        public double WeightKg { get; set; }

        public double Bmi { get; set; }
        public string Category { get; set; } = null!;
        public string Recommendation { get; set; } = null!;
        public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
    }
}
