using FoxOne.Controls;
using FoxOne.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.IO;
using System.Transactions;
using FoxOne.Business;
using System.Web;
using FoxOne.Data;

namespace FoxOne.Web.Controllers
{
    public class EntityController : BaseController
    {
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult Edit()
        {
            IDictionary<string, object> data = Request.Form.ToDictionary();
            string key = Request.Form[NamingCenter.PARAM_KEY_NAME];
            string formViewMode = Request.Form[NamingCenter.PARAM_FORM_VIEW_MODE];
            var ds = GetFormService();
            using (TransactionScope tran = new TransactionScope())
            {
                if(HttpContext.Request.Files.Count>0)
                {
                    foreach (string fileKey in HttpContext.Request.Files.AllKeys)
                    {
                        var file = HttpContext.Request.Files[fileKey];
                        string fileName = file.FileName;
                        string ext = System.IO.Path.GetExtension(fileName).ToLower();
                        fileName = fileName.Replace(ext, "") + DateTime.Now.ToString("_yyyy_MM_dd_HH_mm_ss");
                        var filePath = UploadHelper.Upload(file, "uploadFiles", fileName, true);
                        data[fileKey] = filePath;
                    }
                }
                int effectCount = 0;
                if (formViewMode.Equals(FormMode.Edit.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    effectCount = ds.Update(key, data);
                }
                else
                {
                    effectCount = ds.Insert(data);
                }
                tran.Complete();
                return Json(effectCount > 0);
            }
        }

        private IFormService GetFormService()
        {
            string pageId = Request.Params[NamingCenter.PARAM_PAGE_ID];
            string ctrlId = Request.Params[NamingCenter.PARAM_CTRL_ID];
            var page = PageBuilder.BuildPage(pageId);
            if (page == null)
            {
                throw new FoxOneException("Page_Not_Found");
            }
            var control = page.FindControl(ctrlId);
            IFormService ds = null;
            var form = control as Form;
            if (form == null)
            {
                var table = control as Table;
                if (table == null)
                {
                    throw new FoxOneException("Ctrl_Not_Found");
                }
                else
                {
                    ds = table.DataSource as IFormService;
                }
            }
            else
            {
                ds = form.FormService as IFormService;
            }
            if (ds == null)
            {
                throw new FoxOneException("DataSource_Need_To_Be_IFormSerevice");
            }
            return ds;
        }

        [HttpPost]
        public JsonResult Delete()
        {
            string key = Request.Form[NamingCenter.PARAM_KEY_NAME];
            var ds = GetFormService();
            int effectCount = 0;
            bool result = false;
            using (TransactionScope tran = new TransactionScope())
            {
                if (key.IndexOf(",") > 0)
                {
                    var keys = key.Split(',');
                    foreach (var k in keys)
                    {
                        effectCount += ds.Delete(k);
                    }
                    result = effectCount == keys.Length;
                }
                else
                {
                    effectCount = ds.Delete(key);
                    result = effectCount > 0;
                }
                tran.Complete();
                return Json(result);
            }
        }

        [HttpPost]
        public JsonResult GenerateCRUD()
        {
            string on = "on";
            string IsCRUD = Request.Form["IsCRUD"];
            string CRUDName = Request.Form["CRUDName"];
            string IsList = Request.Form["IsList"];
            string ListName = Request.Form["ListName"];
            string IsEdit = Request.Form["IsEdit"];
            string EditName = Request.Form["EditName"];
            string tableName = Request.Form["TableName"];
            string pageTitle = Request.Form["PageTitle"];
            var pageGenerator = new PageGenerator()
            {
                CRUDName = CRUDName,
                EditPageName = EditName,
                ListPageName = ListName,
                TableName = tableName,
                PageTitle = pageTitle
            };
            pageGenerator.AddCRUD();
            if (IsList == on)
            {
                pageGenerator.AddListPage();
            }
            if (IsEdit == on)
            {
                pageGenerator.AddEditPage();
            }
            return Json(true);
        }

        public FileResult ExportToExcel()
        {
            string ctrlId = Request[NamingCenter.PARAM_CTRL_ID];
            string pageId = Request[NamingCenter.PARAM_PAGE_ID];
            var Table = PageBuilder.BuildPage(pageId).FindControl(ctrlId) as Table;
            string templateName = Request.QueryString["TemplateFile"];
            int ingoreRow = Request.QueryString["ingoreRow"].ConvertTo<int>();
            string fileName = string.Format("{0}-{1}.xls", Table.Id, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"));
            if (!Request.QueryString["fileName"].IsNullOrEmpty())
            {
                fileName = Request.QueryString["fileName"];
            }
            if (!fileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
            {
                fileName = "{0}.xls".FormatTo(fileName);
            }
            int rowSpanColumnIndex = 0;
            if (!Request.QueryString["megerColumn"].IsNullOrEmpty())
            {
                rowSpanColumnIndex = Request.QueryString["megerColumn"].ConvertTo<int>();
            }
            int freezeColumn = 0;
            if (!Request.QueryString["freezeColumn"].IsNullOrEmpty())
            {
                freezeColumn = Request.QueryString["freezeColumn"].ConvertTo<int>();
            }
            string templatePath = templateName.IsNullOrEmpty() ? string.Empty : Server.MapPath("~/App_Config/ExcelTemplate/{0}".FormatTo(templateName));
            var workbook = new ExcelHelper().ExportToExcel(Table, freezeColumn, rowSpanColumnIndex, templatePath, ingoreRow);
            using (MemoryStream ms = new MemoryStream())
            {
                workbook.Write(ms);
                return File(ms.GetBuffer(), "application/vnd.ms-excel", fileName);
            }
        }

        public JsonResult UserRole(string UserId, string RoleId, bool Add)
        {
            bool result = false;
            if(UserId.IsNullOrEmpty() || RoleId.IsNullOrEmpty())
            {
                throw new FoxOneException("UnValid UserId Or RoleId");
            }
            if (Add)
            {
                if (DBContext<IUserRole>.Instance.Count(o => o.RoleId.Equals(RoleId) && o.UserId.Equals(UserId)) > 0)
                {
                    throw new FoxOneException("当前用户拥有该角色");
                }
                else
                {
                    result = DBContext<IUserRole>.Insert(new UserRole()
                    {
                        Id = Utility.GetGuid(),
                        RentId = 1,
                        RoleId = RoleId,
                        Status = DefaultStatus.Enabled.ToString(),
                        UserId = UserId
                    });
                }
            }
            else
            {
                var entity = DBContext<IUserRole>.Instance.FirstOrDefault(o => o.UserId.Equals(UserId, StringComparison.OrdinalIgnoreCase) && o.RoleId.Equals(RoleId, StringComparison.OrdinalIgnoreCase));
                if(entity!=null)
                {
                    result = DBContext<IUserRole>.Delete(entity);
                }
            }
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateInput(false)]
        public JsonResult UserProfile()
        {
            var data = Request.Form.ToDictionary().ToEntity<User>();
            string key = Request.Form[NamingCenter.PARAM_KEY_NAME];

            var user = DBContext<IUser>.Instance.Get(key);
            if(user!=null)
            {
                //user.Name = data.Name;
                user.Birthdate = data.Birthdate;
                user.QQ = data.QQ;
                user.LastUpdateTime = DateTime.Now;
                user.Sex = data.Sex;
                user.Identity = data.Identity;
                user.WorkNumber = data.WorkNumber;
                if(!data.Properties.IsNullOrEmpty())
                {
                    foreach (var item in data.Properties.Keys)
                    {
                        user.Properties[item] = data.Properties[item];
                    }
                }
                if (SysConfig.IsProductEnv)
                {
                    if (data.Password.IsNotNullOrEmpty())
                    {
                        new MailUserService().UpdateUser(new MailUser() { DepartmentId = user.Department.Code, Mobile = user.MobilePhone, Name = user.Name, Password = data.Password, UserId = user.Mail });
                    }
                }
                if(Dao.Get().Update(user)>0)
                {
                    (user as IExtProperty).SetProperty();
                    Logger.Info("System:{0}:【{1}】修改个人信息，IP：{2}，修改的信息有：{3}", user.Id, user.Name, Utility.GetWebClientIp(), JSONHelper.Serialize(user));
                    return Json(true);
                }
            }
            return Json(false);
        }

        [HttpPost]
        public JsonResult RefreshTable()
        {
            FoxOne.Data.Mapping.TableMapper.RefreshTableCache();
            return Json(true);
        }
    }
}
