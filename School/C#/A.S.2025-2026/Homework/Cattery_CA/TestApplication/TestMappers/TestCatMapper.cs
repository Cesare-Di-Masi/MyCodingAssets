using Application.Dto;
using Application.Mappers;
using Domain.Model.Entities;

namespace TestApplication.TestMappers
{
    [TestClass]
    public class TestCatMapper
    {
        [TestMethod]
        public void ToEntity_Correct()
        {
            CatDto dto = new CatDto("test1", true, new DateOnly(2020, 5, 21), null, null, "A friendly cat", "12345M2020ASD");
            Cat entity = new Cat("test1", true, new DateOnly(2020, 5, 21), null, null, "A friendly cat", dto.Id);
            Assert.AreEqual(entity, dto.ToEntity());
        }

        [TestMethod]
        public void ToDto_Correct()
        {
            CatDto dto = new CatDto("test1", true, new DateOnly(2020, 5, 21), null, "A friendly cat", "no breed", "12345M2020ASD");
            Cat entity = new Cat("test1", true, new DateOnly(2020, 5, 21), null, null, "A friendly cat", dto.Id);
            Assert.AreEqual(dto, entity.ToDto());
        }
    }
}