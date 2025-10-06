using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeasonExtensionMethodLib
{
    public static class SeasonExtension
    {
        public static Seasons GetSeason(this DateTime date)
        {
            // Estrae giorno e mese per un confronto più semplice
            int day = date.Day;
            int month = date.Month;

            if (month == 3 && day >= 21 || month > 3 && month < 6 || month == 6 && day < 21)
                return Seasons.SPRING;

            if (month == 6 && day >= 21 || month > 6 && month < 9 || month == 9 && day < 22)
                return Seasons.SUMMER;

            if (month == 9 && day >= 22 || month > 9 && month < 12 || month == 12 && day < 21)
                return Seasons.FALL;

            // Deve essere inverno
            return Seasons.WINTER;
        }

        public static bool IsSummer(this DateTime currDate)
        {
            if (currDate.GetSeason() == Seasons.SUMMER)
            {
                return true;
            }

            return false;
        }

        public static int DaysForNextSeason(this DateTime currDate)
        {
            // 1. Ottiene la stagione corrente
            Seasons currentSeason = currDate.GetSeason();

            // 2. Determina la data di inizio della PROSSIMA stagione
            DateTime nextSeasonStartDate;

            switch (currentSeason)
            {
                case Seasons.SPRING:
                    // La prossima è l'Estate (21 Giugno dello stesso anno)
                    nextSeasonStartDate = new DateTime(currDate.Year, 6, 21);
                    break;

                case Seasons.SUMMER:
                    // La prossima è l'Autunno (22 Settembre dello stesso anno)
                    nextSeasonStartDate = new DateTime(currDate.Year, 9, 22);
                    break;

                case Seasons.FALL:
                    // La prossima è l'Inverno (21 Dicembre dello stesso anno)
                    nextSeasonStartDate = new DateTime(currDate.Year, 12, 21);
                    break;

                case Seasons.WINTER:
                    // La prossima è la Primavera (21 Marzo dell'ANNO SUCCESSIVO)
                    nextSeasonStartDate = new DateTime(currDate.Year + 1, 3, 21);
                    break;

                default:
                    // Caso teoricamente irraggiungibile, ma buono per sicurezza
                    throw new ArgumentOutOfRangeException("Stagione non valida.");
            }

            TimeSpan difference = nextSeasonStartDate - currDate;
            int days = (int)difference.Days;

            if (days > 365)
            {
                if (DateTime.IsLeapYear(currDate.Year))
                {
                    days -= 366;
                }
                else
                {
                    days -= 365;
                }
            }

            return days;
        }
    }
}