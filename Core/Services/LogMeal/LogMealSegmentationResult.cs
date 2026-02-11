using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitnessBot.Core.Services.LogMeal
{
    public class LogMealSegmentationResult
    {
        public NutritionalInfo? TotalNutritionalInfo { get; set; }
        public DishNutritionalInfo[]? Dishes { get; set; }
    }
}
