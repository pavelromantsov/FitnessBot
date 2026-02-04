using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitnessBot.Scenarios
{
    public enum ScenarioType
    {
        None = 0,
        Bmi = 1,
        DailyGoal = 2,
        Meal = 3,
        CustomCalories = 4,
        MealTimeSetup = 5,
        Registration = 6,
        EditProfile = 7,
        SetDailyGoal = 8,
        ActivityReminderSettings = 9
    }
}
