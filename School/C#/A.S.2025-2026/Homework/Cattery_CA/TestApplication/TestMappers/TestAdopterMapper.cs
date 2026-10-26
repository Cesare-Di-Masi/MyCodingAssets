using Application.Dto;
using Application.Mappers;
using Domain.Model.Entities;
using Domain.Model.ValueObjects;

namespace TestApplication.TestMappers
{
    [TestClass]
    public class TestAdopterMapper
    {
        [TestMethod]
        public void ToEntity_Correct()
        {
            AdopterDto dto = new AdopterDto("mario", "rossi", "RSSMRA85RTY7IPD3", "00100", "3516333206", "mario.rossi@gmail.com", new DateOnly(1975, 5, 21));
            Adopter entity = new Adopter(new FullName("mario", "rossi"), new DateOnly(1975, 5, 21),
                new TaxIDCode("RSSMRA85RTY7IPD3"), new CAP("00100"), new Email("mario.rossi@gmail.com"), new PhoneNumber("3516333206"));
            var test = dto.ToEntity();

            Assert.AreEqual(test, entity);
        }

        [TestMethod]
        public void ToDto_Correct()
        {
            AdopterDto dto = new AdopterDto("mario", "rossi", "RSSMRA85RTY7IPD3", "00100", "3516333206", "mario.rossi@gmail.com", new DateOnly(1975, 5, 21));
            Adopter entity = new Adopter(new FullName("mario", "rossi"), new DateOnly(1975, 5, 21),
                new TaxIDCode("RSSMRA85RTY7IPD3"), new CAP("00100"), new Email("mario.rossi@gmail.com"), new PhoneNumber("3516333206"));
            var test = entity.ToDto();

            Assert.AreEqual(test, dto);
        }
    }
}