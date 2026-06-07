using Lus.Contracts.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lus.Application.Common.Entities
{
    public abstract class IdName<TEntityKey> : IEntityWithKey<TEntityKey>
    {
        public TEntityKey Id { get; set; }

        public string Name { get; set; }

        IdName(TEntityKey Id, string Name){
            this.Id = Id;
            this.Name = Name;
}

    }
}
