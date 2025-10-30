using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.UseCases;
using Application.Dto;

namespace TestApplication.TestUseCases
{
    [TestClass]
    public class TestCatteryService
    {

        [TestMethod]
        public void RegisterNewCat_InvalidValues_ShouldThrow()
        {
            IAdopterRepository adopterRepository = null;
            ICatRepository catRepository = null;
            IAdoptionInterface adoptionRepository = null;
            CatteryService catteryService = new CatteryService(catRepository,adopterRepository,adoptionRepository);
            DateOnly birthdate = DateOnly.FromDateTime(DateTime.Now.AddDays(-25));
            DateOnly arrivingDate = DateOnly.FromDateTime(DateTime.Now);
            Assert.ThrowsException<ArgumentException>(() => catteryService.RegisterNewCat(new CatDto("", true, arrivingDate, birthdate, "A friendly cat.", null, "12345M2020ASD")));

        }

    }
}
