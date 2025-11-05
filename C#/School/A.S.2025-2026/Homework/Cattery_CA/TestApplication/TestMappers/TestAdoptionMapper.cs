using Application.Dto;
using Application.Mappers;
using Domain.Model.Entities;
using Domain.Model.ValueObjects;

namespace TestApplication.TestMappers
{
    [TestClass]
    public class TestAdoptionMapper
    {
        [TestMethod]
        public void ToEntity_Correct()
        {
            CatDto catDto = new CatDto("test1", true, new DateOnly(2020, 5, 21), null, "A friendly cat", "no breed", "12345M2020ASD");
            AdopterDto adopterDto = new AdopterDto("mario", "rossi", "RSSMRA85RTY7IPD3", "00100", "3516333206", "mario.rossi@gmail.com", new DateOnly(1975, 5, 21));

            AdoptionDto dto = new AdoptionDto(adopterDto, catDto, new DateOnly(2023, 1, 15), "no description");

            Adopter adopterEntity = new Adopter(new FullName("mario", "rossi"), new DateOnly(1975, 5, 21),
                new TaxIDCode("RSSMRA85RTY7IPD3"), new CAP("00100"), new Email("mario.rossi@gmail.com"), new PhoneNumber("3516333206"));
            Cat catEntity = new Cat("test1", true, new DateOnly(2020, 5, 21), null, null, "A friendly cat", catDto.Id);

            Adoption entity = new Adoption(new DateOnly(2023, 1, 15), catEntity, adopterEntity, "no description");

            Assert.AreEqual(dto.ToEntity(), entity);
        }

        [TestMethod]
        public void ToDto_Correct()
        {
            CatDto catDto = new CatDto("test1", true, new DateOnly(2020, 5, 21), null, "A friendly cat", "no breed", "12345M2020ASD");
            AdopterDto adopterDto = new AdopterDto("mario", "rossi", "RSSMRA85RTY7IPD3", "00100", "3516333206", "mario.rossi@gmail.com", new DateOnly(1975, 5, 21));

            AdoptionDto dto = new AdoptionDto(adopterDto, catDto, new DateOnly(2023, 1, 15), "no description");

            Adopter adopterEntity = new Adopter(new FullName("mario", "rossi"), new DateOnly(1975, 5, 21),
                new TaxIDCode("RSSMRA85RTY7IPD3"), new CAP("00100"), new Email("mario.rossi@gmail.com"), new PhoneNumber("3516333206"));
            Cat catEntity = new Cat("test1", true, new DateOnly(2020, 5, 21), null, null, "A friendly cat", catDto.Id);

            Adoption entity = new Adoption(new DateOnly(2023, 1, 15), catEntity, adopterEntity, "no description");

            Assert.AreEqual(entity.ToDto(), dto);
        }
    }
}