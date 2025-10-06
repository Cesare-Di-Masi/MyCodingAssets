using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeasonExtensionMethodLib
{
    public static class SeasonExtension
    {
        public static Seasons GetSeason(this DateTime currDate)
        {
            if (currDate.Year % 4 == 0)
            {
                if(currDate.DayOfYear >= 81 && currDate.DayOfYear <= 172)

                if (currDate.DayOfYear >= 173 && currDate.DayOfYear <= 263)
                {
                    return Seasons.SUMMER;
                }

                if(currDate.DayOfYear >= 264 && currDate.DayOfYear<=354)
                {
                    return Seasons.FALL;
                }
            }
            else
            {
                if (currDate.DayOfYear >= 172 && currDate.DayOfYear <= 263)
                {
                    return Seasons.SUMMER;
                }
            }
        }

        public static bool IsSummer(this DateTime currDate)
        {
            
        }




    }
}
