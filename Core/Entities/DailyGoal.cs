using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitnessBot.Core.Entities
{
    public class DailyGoal
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public DateTime Date { get; set; }

        public int TargetSteps { get; set; }
        public double TargetCaloriesIn { get; set; }
        public double TargetCaloriesOut { get; set; }

        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
