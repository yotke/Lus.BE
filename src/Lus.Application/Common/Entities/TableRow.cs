using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lus.Application.Common.Entities
{
    public abstract class TableRow<TEntityKey> :IOptionalValue<TEntityKey>
    {
        public Settings? Settings { get; set; }

        public string Note { get; set; }

        public int GroupId { get; set; }
    }
}
