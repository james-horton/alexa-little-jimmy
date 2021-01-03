using AlexaSkill.Data;
using System;
using System.Linq;

namespace AlexaSkill.Repositories
{

    public class BurnRepository
    {
        private AlexaDbContext _context;

        public BurnRepository()
        {
            _context = new AlexaDbContext();
        }

        public string GetRandomBurn(string burnType)
        {
            var burn = "";
            var burns = _context.Burns.ToList().Where(b => b.BurnType == burnType);

            if (burns.Count() > 0)
            {
                int randomPos = new Random().Next(0, burns.Count());
                burn = burns.ElementAtOrDefault(randomPos).Content;
            }

            return burn;
        }
    }

    
}