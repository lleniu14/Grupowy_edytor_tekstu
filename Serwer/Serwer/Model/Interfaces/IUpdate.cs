using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serwer.Model.Interfaces
{
    interface IUpdateDto
    {
        int AddUpdateDto(UpdateDto UpdateDto);
        UpdateDto GetUpdateDto(int UpdateDtoId);
        void DeleteUpdateDto(UpdateDto UpdateDto);
    }
}
