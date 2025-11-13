using Repository.Entity;
using Repository.Interface;
using Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class LeopardProfileService : ILeopardProfileService
    {
        private readonly ILeopardProfileRepository _repo;

        public LeopardProfileService(ILeopardProfileRepository repo)
        {
            _repo = repo;
        }

        public IEnumerable<LeopardProfile> GetAll() => _repo.GetAllWithType();
        public LeopardProfile Get(int id) => _repo.GetById(id);
        public void Create(LeopardProfile leopardProfile)
        {
            Validate(leopardProfile);
            _repo.Add(leopardProfile);
        }

        public void Update(LeopardProfile leopardProfile)
        {
            Validate(leopardProfile);
            _repo.Update(leopardProfile);
        }

        public void Delete(int id) => _repo.Delete(id);

        public IEnumerable<IGrouping<string, LeopardProfile>> Search(string name, string weight)
            => _repo.Search(name, weight);

        private void Validate(LeopardProfile leopardProfile)
        {
            if (string.IsNullOrWhiteSpace(leopardProfile.LeopardName) ||
                !System.Text.RegularExpressions.Regex.IsMatch(leopardProfile.LeopardName, @"^([A-Z0-9][a-zA-Z0-9#]*\s)*([A-Z0-9][a-zA-Z0-9#]*)$"))
                throw new ArgumentException("Invalid ModelName");

            if (leopardProfile.Weight > 15)
                throw new ArgumentException("Leopard weight must > 15");
        }
    }

}
