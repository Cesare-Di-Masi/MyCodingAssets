using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Cat : Animal
    {
        public Cat(string name, List<VeterinaryVisit> visits = null) : base(name, visits)
        {
        }
    }
}