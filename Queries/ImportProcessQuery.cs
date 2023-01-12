using System;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using LogicApps.Portals.Plugin;
using LogicApps.Portals.Models;
using LogicApps.Portals.CustomEntity;

namespace LogicApps.Portals.Queries
{
    public class ImportProcessQuery
    {
        private const string UserId = "D1F709D6-F440-E911-A99A-000D3A367368";//змінити
        public static EntityCollection RunImportProcessQuery(string args, IUnitOfService unitOfService)
        {
            ImportProcessDto ProcessDto = JsonConvert.DeserializeObject<ImportProcessDto>(args);
            OrganizationRequest req = new OrganizationRequest("new_MyImportAction")
            {
                ["Target"] = new EntityReference(ProcessDto.EntityName, ProcessDto.RecordId),
                ["InitiatingUser"] = new EntityReference("systemuser", new Guid(UserId)),
                ["IsManual"] = true,
                ["IsPortalRequest"] = false
            };

            OrganizationResponse response = unitOfService.OrganizationService.Execute(req);

            var succeeded = (bool)response.Results["Succeeded"];
            if (succeeded)
            {
                response = unitOfService.OrganizationService.Execute(req);
                ProcessDto.Succeeded = (bool)response.Results["Succeeded"];
                ProcessDto.Result = (string)response.Results["Output"];
                ProcessDto.ErrorList = response.Results["ErrorList"].ToString();
                return JsonConvert.SerializeObject(ProcessDto).ToCustomEntityCollection();
            }

            ProcessDto.Succeeded = false;
            ProcessDto.Result = (string)response.Results["Output"];
            ProcessDto.ErrorList = "Import processing failed!";
            return JsonConvert.SerializeObject(ProcessDto).ToCustomEntityCollection();

        }
    }


}
