using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfServiceAPI.Classes
{
   public class TestLevel
    {
        public int Id { get; set; }

        public string Value { get; set;}

        public List<TestLevel> GetLevels()
        {
            List<TestLevel> lstTestLevels = new List<TestLevel>
            {
                new TestLevel() { Id = 1, Value = "A" },
                new TestLevel() { Id = 2, Value = "AS" },
                new TestLevel() { Id = 3, Value = "O" }
            };

            return lstTestLevels;
        }
    }
}
