using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serwer.Model.Interfaces;
using Serwer.Model.Database;

namespace Serwer.Model.Methods
{
    class UpdateDtoMethods : IUpdateDto
    {
        private readonly DatabaseContext _databaseContext;
        public UpdateDtoMethods(DatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
        }
        int IUpdateDto.AddUpdateDto(UpdateDto UpdateDto)
        {
            if (UpdateDto == null)
            {
                throw new Exception("UpdateDto object cannot be null");
            }

            UpdateDto.UpdateDtoId = 0;

            _databaseContext.Updates.Add(UpdateDto);
            _databaseContext.SaveChanges();

            return UpdateDto.UpdateDtoId;
        }

        void IUpdateDto.DeleteUpdateDto(UpdateDto UpdateDto)
        {
            if (UpdateDto == null)
            {
                throw new Exception("UpdateDto object cannot be null");
            }

            _databaseContext.Updates.Remove(UpdateDto);
            _databaseContext.SaveChanges();
        }



        UpdateDto IUpdateDto.GetUpdateDto(int UpdateDtoId)
        {
            if (UpdateDtoId < 0)
            {
                throw new Exception("Id cannot be less than 0");
            }

            return _databaseContext.Updates.FirstOrDefault(UpdateDto => UpdateDto.UpdateDtoId == UpdateDtoId);
        }
    }
}
