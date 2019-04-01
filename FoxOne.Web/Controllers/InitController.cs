using FoxOne.Business;
using FoxOne.Business.Security;
using FoxOne.Controls;
using FoxOne.Core;
using FoxOne.Data;
using FoxOne.Data.Mapping;
using FoxOne.Workflow.DataAccess;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Xml.Serialization;
using System.Transactions;

namespace FoxOne.Web.Controllers
{
    public class InitController : BaseController
    {
        private static IList<Type> types = TypeHelper.GetAllImpl<IAutoCreateTable>();

        private static IList<Type> syncTypes = new List<Type>() { typeof(ComponentEntity), typeof(CRUDEntity), typeof(PageEntity), typeof(LayoutEntity),typeof(PageRelateEntity), typeof(DataDictionary), typeof(Permission), typeof(WorkflowDefinition), typeof(WorkflowApplication) };

        [CustomUnAuthorize]
        public ActionResult Index()
        {
            if (SysConfig.IsProductEnv)
            {
                throw new PageNotFoundException();
            }
            var allEntity = new List<SelectListItem>();
            foreach (Type item in TypeHelper.GetAllSubType<EntityBase>())
            {
                allEntity.Add(new SelectListItem()
                {
                    Selected = false,
                    Text = item.GetDisplayName(),
                    Value = item.FullName
                });
            }
            ViewData["AllEntity"] = allEntity;
            return View();
        }

        [CustomUnAuthorize]
        public ActionResult HomeIndex(string id)
        {
            if (SysConfig.IsProductEnv)
            {
                throw new Exception("不允许此操作");
            }
            var user = DBContext<IUser>.Instance.Where(o => o.Name == id || o.LoginId == id || o.MobilePhone == id || o.Id == id);
            if (user.Count() > 1)
            {
                throw new Exception("找到多个相同用户");
            }
            if (user.Count() == 0)
            {
                throw new Exception("没有找到相关用户");
            }
            FormsAuthentication.SetAuthCookie(user.FirstOrDefault().LoginId, false);
            if (Request.Cookies[SSOController.SSO_COOKIE_KEY] != null)
            {
                return RedirectToAction("Redirect", "SSO");
            }
            return RedirectToAction("Index", "Home");
        }

        [CustomUnAuthorize]
        [HttpPost]
        public JsonResult CreateTable(string id)
        {
            if (SysConfig.IsProductEnv)
            {
                throw new Exception("不允许此操作");
            }
            if (id.IsNullOrEmpty())
            {
                types.ForEach(o =>
                {
                    Dao.Get().CreateTable(o, true);
                });
            }
            else
            {
                Dao.Get().CreateTable(TypeHelper.GetType(id), true);
            }
            TableMapper.RefreshTableCache();
            return Json(true, JsonRequestBehavior.AllowGet);
        }


        [CustomUnAuthorize]
        [HttpPost]
        public JsonResult ClearTable()
        {
            if (SysConfig.IsProductEnv)
            {
                throw new Exception("不允许此操作");
            }
            types.ForEach(o =>
            {
                Dao.Get().Delete(o, null);
            });
            return Json(true, JsonRequestBehavior.AllowGet);
        }

        [CustomUnAuthorize]
        [HttpPost]
        public JsonResult InitData()
        {
            if (SysConfig.IsProductEnv)
            {
                throw new Exception("不允许此操作");
            }
            var dirInfo = new DirectoryInfo(Server.MapPath("~/InitData"));
            if (!dirInfo.Exists)
            {
                throw new Exception("找不到初始数据，文件夹InitData不存在");
            }
            var files = dirInfo.GetFiles("*.xml", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                ImportDataFromXMLFile(file);
            }
            return Json(true, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 备份所有实体数据
        /// </summary>
        /// <returns></returns>
        public JsonResult Out()
        {
            var dirInfo = Server.MapPath("~/InitData");
            if (!Directory.Exists(dirInfo))
            {
                Directory.CreateDirectory(dirInfo);
            }
            var allTypes = TypeHelper.GetAllSubType<EntityBase>();
            string fileName = string.Empty;
            foreach (var type in allTypes)
            {
                ExportDataToXMLFile(type, dirInfo);
            }
            return Json(true, JsonRequestBehavior.AllowGet);
        }

        private void ImportDataFromXMLFile(FileInfo file)
        {
            var type = typeof(List<>);
            var targetType = TypeHelper.GetType(file.Name.Replace(file.Extension, ""));
            if (targetType == null) return;
            Dao.Get().Delete(targetType, null);
            type = type.MakeGenericType(targetType);
            var serializer = new XmlSerializer(type);
            var result = serializer.Deserialize(file.OpenRead()) as IEnumerable;
            foreach (var item in result)
            {
                Dao.Get().Insert(item);
            }
        }

        private void ExportDataToXMLFile(Type type, string dirInfo)
        {
            var t = typeof(List<>);
            string fileName = type.FullName;
            t = t.MakeGenericType(type);
            var instance = Activator.CreateInstance(t);
            var serializer = new XmlSerializer(t);
            Dao.Get().Select(type).ForEach(o =>
            {
                t.InvokeMember("Add", System.Reflection.BindingFlags.Default | System.Reflection.BindingFlags.InvokeMethod, null, instance, new object[] { o });
            });
            using (var stream = System.IO.File.Create(Path.Combine(dirInfo, fileName + ".xml")))
            {
                serializer.Serialize(stream, instance);
            }
        }

        /// <summary>
        /// 备份配置数据
        /// </summary>
        public JsonResult BackupConfigData()
        {
            if (!Sec.IsSuperAdmin)
            {
                throw new Exception("没有权限");
            }
            var dirInfo = Server.MapPath("~/ConfigData");
            if (!Directory.Exists(dirInfo))
            {
                Directory.CreateDirectory(dirInfo);
            }
            else
            {
                //如果已经有备份的配置数据，则另存为带当前时间的文件夹
                Directory.Move(dirInfo, Server.MapPath("~/ConfigData" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")));
                Directory.CreateDirectory(dirInfo);
            }
            string fileName = string.Empty;
            foreach (var type in syncTypes)
            {
                ExportDataToXMLFile(type, dirInfo);
            }
            return Json(true);
        }

        /// <summary>
        /// 恢复备份数据
        /// </summary>
        public JsonResult RecoveryConfigData()
        {
            if (!Sec.IsSuperAdmin)
            {
                throw new Exception("没有权限");
            }
            var dirInfo = new DirectoryInfo(Server.MapPath("~/ConfigData"));
            if (!dirInfo.Exists)
            {
                throw new Exception("不能恢复数据，文件夹ConfigData不存在");
            }
            var files = dirInfo.GetFiles("*.xml", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                ImportDataFromXMLFile(file);
            }
            return Json(true);
        }

        /// <summary>
        /// 同步测试环境与生产环境的配置数据
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonResult AsyncTestProduct(string from, string to)
        {
            if (!Sec.IsSuperAdmin)
            {
                throw new Exception("没有权限");
            }
            if (from.IsNullOrEmpty() || to.IsNullOrEmpty() || from.Equals(to, StringComparison.OrdinalIgnoreCase))
            {
                return Json(false);
            }
            //先备份现有的配置数据
            BackupConfigData();

            var testEnv = Dao.Get(from);
            var productEnv = Dao.Get(to);
            foreach (var type in syncTypes)
            {
                var testData = testEnv.Select(type);
                productEnv.Delete(type, null);
                foreach (var item in testData)
                {
                    productEnv.Insert(item);
                }
            }
            DBContext<PageEntity>.ClearCache();
            DBContext<ComponentEntity>.ClearCache();
            DBContext<CRUDEntity>.ClearCache();
            DBContext<Permission>.ClearCache();
            DBContext<WorkflowDefinition>.ClearCache();
            DBContext<DataDictionary>.ClearCache();
            return Json(true);
        }

        public ActionResult Publish()
        {
            return View();
        }

        /// <summary>
        /// 重刷WBS数据
        /// </summary>
        /// <returns></returns>
        public JsonResult RefreshWBS()
        {
            using (TransactionScope tran = new TransactionScope())
            {
                var root = DBContext<IDepartment>.Instance.FirstOrDefault(c => c.Name.Equals("猎狐科技"));
                ChangeWBS(root);
                tran.Complete();
            }
            return Json(true, JsonRequestBehavior.AllowGet);
        }

        private void ChangeWBS(IDepartment dep)
        {
            if (!dep.Childrens.IsNullOrEmpty())
            {
                foreach (var child in dep.Childrens)
                {
                    bool isUpdate = false;
                    //长度不一致并不是以父级WBS为开头的、子部门中WBS重复的情况
                    if (child.WBS.Length - dep.WBS.Length != 3 || !child.WBS.StartsWith(dep.WBS) ||
                        dep.Childrens.Where(c => c.WBS.Equals(child.WBS)).Count() > 1)
                    {
                        var maxWBS = dep.Childrens.Where(c => c.WBS.StartsWith(dep.WBS) && c.WBS.Length - dep.WBS.Length == 3).Max(c => c.WBS);
                        if (maxWBS.IsNullOrEmpty())
                        {
                            child.WBS = dep.WBS + "001";
                        }
                        else
                        {
                            int num = maxWBS.Substring(maxWBS.Length - 3).ConvertTo<int>() + 1;
                            child.WBS = dep.WBS + num.ToString().PadLeft(3, '0');
                        }
                        isUpdate = true;
                    }
                    //修正Level
                    if (child.Level != dep.Level + 1)
                    {
                        child.Level = dep.Level + 1;
                        isUpdate = true;
                    }
                    if (isUpdate)
                    {
                        DBContext<IDepartment>.Update(child);
                    }
                    ChangeWBS(child);
                }
            }
        }

    }
}
