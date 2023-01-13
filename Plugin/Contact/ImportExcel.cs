using CrmEarlyBound;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.IO;
using System.Linq;

namespace LogicApps.Portals.Plugin.Contact
{
    public class ImportExcel : PluginBase
    {
        public ImportExcel() : base(typeof(ImportExcel))
        {
            RegisterEvent(PipelineStages.PostOperation, MessageNames.Create, Annotation.EntityLogicalName, PostOperationCreateHandler);
        }
        private void PostOperationCreateHandler(LocalPluginContext localPluginContext)
        {
            IPluginExecutionContext context = localPluginContext.PluginExecutionContext;
            IOrganizationService organizationService = localPluginContext.OrganizationService;
            if (context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is Entity)
            {
                Entity noteEntity = (Entity)context.InputParameters["Target"];
                Annotation note = noteEntity.ToEntity<Annotation>();
                if (!string.IsNullOrEmpty(note.FileName))
                {
                    var noteBody = note.DocumentBody;
                    byte[] fileContent = Convert.FromBase64String(noteBody);
                    MemoryStream ms = new MemoryStream();
                    ms.Write(fileContent, 0, fileContent.Length);
                    ms.Position = 0;
                    var doc = SpreadsheetDocument.Open(ms, false);
                    WorkbookPart workbookPart = doc.WorkbookPart;
                    WorksheetPart worksheetPart = workbookPart.WorksheetParts.First();
                    SheetData thisSheet = worksheetPart.Worksheet.Elements<SheetData>().First();

                    for (int i = 1; i < thisSheet.Elements<Row>().Count(); i++)
                    {
                        var firstname = ReadExcelCell(thisSheet.Elements<Row>().ElementAt(i).Elements<Cell>().ElementAt(0), workbookPart);
                        var lastname = ReadExcelCell(thisSheet.Elements<Row>().ElementAt(i).Elements<Cell>().ElementAt(1), workbookPart);
                        var email = ReadExcelCell(thisSheet.Elements<Row>().ElementAt(i).Elements<Cell>().ElementAt(2), workbookPart);
                        var phone = ReadExcelCell(thisSheet.Elements<Row>().ElementAt(i).Elements<Cell>().ElementAt(3), workbookPart);

                        var hasId = Guid.TryParse(ReadExcelCell(thisSheet.Elements<Row>().ElementAt(i).Elements<Cell>().ElementAt(4), workbookPart), out var contactId);

                        if (hasId)
                        {
                            var contact = new Entity("contact", contactId);
                            bool isChanged = false;
                            try
                            {
                                var actualContact = organizationService.Retrieve(contact.LogicalName, contactId, new ColumnSet("firstname", "lastname", "emailaddress1", "telephone1", "statecode"));

                                if (actualContact != null)
                                {
                                    if (firstname != (string)actualContact["firstname"])
                                    {
                                        contact["firstname"] = firstname;
                                        isChanged = true;
                                    }
                                    if (lastname != (string)actualContact["lastname"])
                                    {
                                        contact["lastname"] = lastname;
                                        isChanged = true;
                                    }

                                    if (email != (string)actualContact["emailaddress1"])
                                    {
                                        contact["emailaddress1"] = email;
                                        isChanged = true;
                                    }
                                    if (phone != (string)actualContact["telephone1"])
                                    {
                                        contact["telephone1"] = phone;
                                        isChanged = true;
                                    }

                                    if (isChanged)
                                        organizationService.Update(contact);
                                }
                            }
                            catch (Exception)
                            {

                                contact["firstname"] = firstname;
                                contact["lastname"] = lastname;
                                contact["emailaddress1"] = email;
                                contact["telephone1"] = email;

                                organizationService.Create(contact);
                            }
                        }
                        else
                        {
                            var createdcontact = new Entity("contact", contactId);
                            createdcontact["firstname"] = firstname;
                            createdcontact["lastname"] = lastname;
                            createdcontact["emailaddress1"] = email;
                            createdcontact["telephone1"] = email;

                            organizationService.Create(createdcontact);
                        }
                    }
                }
            }
        }
        private string ReadExcelCell(Cell cell, WorkbookPart workbookPart)
        {
            var cellValue = cell.CellValue;
            var text = cellValue == null ? cell.InnerText : cellValue.Text;
            if (cell.DataType != null && cell.DataType == CellValues.SharedString)
            {
                text = workbookPart.SharedStringTablePart.SharedStringTable
                    .Elements<SharedStringItem>().ElementAt(
                        Convert.ToInt32(cell.CellValue.Text)).InnerText;
            }
            return (text ?? string.Empty).Trim();
        }
    }
}