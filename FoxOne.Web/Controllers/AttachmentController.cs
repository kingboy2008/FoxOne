using FoxOne._3VJ;
using FoxOne.Business;
using FoxOne.Business.Environment;
using FoxOne.Business.Security;
using FoxOne.Controls;
using FoxOne.Core;
using FoxOne.Data;
using FoxOne.Data.Attributes;
using FoxOne.Workflow.Business;
using FoxOne.Workflow.DataAccess;
using FoxOne.Workflow.Kernel;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using FoxOne.Data.Util;
using System.Transactions;

namespace FoxOne.Web.Controllers
{
    public class AttachmentController : BaseController
    {
        public ActionResult Index(string id)
        {

            if (id.IsNullOrEmpty())
            {
                throw new PageNotFoundException();
            }
            bool canModify = false;
            if (!id.Equals("Common"))
            {
                WorkflowHelper helper = new WorkflowHelper(Sec.User);
                helper.OpenWorkflow(id);
                if (helper.FlowInstance.FlowTag < FlowStatus.Finished && helper.FlowInstance.WorkItems.Any(o => o.Status < WorkItemStatus.Finished && o.PartUserId.Equals(Sec.User.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    canModify = true;
                }
            }
            else
            {
                canModify = Sec.Provider.HasPermission("AttachmentUpload");
            }
            var workItemTable = new Table();
            workItemTable.AutoGenerateColum = false;
            workItemTable.AllowPaging = false;
            workItemTable.Columns.Add(new TableColumn() { ColumnName = "文件名", FieldName = "FileName", TextAlign = CellTextAlign.Center });
            workItemTable.Columns.Add(new TableColumn() { ColumnName = "上传者", FieldName = "CreatorId", TextAlign = CellTextAlign.Center, ColumnConverter = new EntityDataSource() { EntityType = typeof(User) } });
            workItemTable.Columns.Add(new TableColumn() { ColumnName = "上传时间", FieldName = "CreateTime", TextAlign = CellTextAlign.Center, DataFormatString = "{0:yyyy-MM-dd HH:mm}" });
            workItemTable.Columns.Add(new TableColumn() { ColumnName = "文件类型", FieldName = "FileType", TextAlign = CellTextAlign.Center });
            workItemTable.Columns.Add(new TableColumn() { ColumnName = "文件大小", FieldName = "FileSize", DataFormatString = "{0}（字节）", TextAlign = CellTextAlign.Center });
            workItemTable.Buttons.Add(new TableButton() { Id = "btnDownload", Href = "/Attachment/Download/{0}", Target = TableButtonTarget.Blank, CssClass = "btn btn-default btn-sm", Name = "下载", DataFields = "Id" });
            if (canModify)
            {
                workItemTable.Buttons.Add(new TableButton() { Id = "btnDeleteA", CssClass = "btn btn-danger btn-sm", Name = "删除", OnClick = "return confirm('您确定要删除该附件吗？');", Href = "/Attachment/Delete/{0}", DataFields = "Id", TableButtonType = TableButtonType.TableRow, Filter = new StaticDataFilter() { ColumnName = "CreatorId", Operator = typeof(NotEqualOperation).FullName, Value = "$User.Id$" } });
            }
            workItemTable.DataSource = new EntityDataSource() { EntityType = typeof(AttachmentEntity), DataFilter = new StaticDataFilter() { ColumnName = "RelateId", Value = id, Operator = typeof(EqualsOperation).FullName } };
            ViewData["Table"] = workItemTable;
            ViewData["RelateId"] = id;
            ViewData["CanUpload"] = canModify;
            return View();
        }

        public ActionResult AttendanceData()
        {
            return View();
        }

        private IList<IDictionary<string, object>> ReadDataFromAccess(string filePath, DateTime updateTime)
        {
            System.Data.OleDb.OleDbConnection conn = new System.Data.OleDb.OleDbConnection("Provider=Microsoft.Jet.OleDb.4.0;Data Source=" + filePath);
            System.Data.OleDb.OleDbCommand command = new System.Data.OleDb.OleDbCommand("select UserId,CheckTime,CheckType from CHECKINOUT where CheckTime > @CheckTime", conn);
            command.CommandType = System.Data.CommandType.Text;
            command.Parameters.Add(new System.Data.OleDb.OleDbParameter() { ParameterName = "@CheckTime", Value = updateTime.ToString("yyyy-MM-dd 00:00:00") });
            conn.Open();
            using (var reader = command.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
            {
                return reader.ReadDictionaries();
            }
        }

        public ActionResult UploadAttendanceData(HttpPostedFileBase file, string LastUpdateTime)
        {
            if (file != null)
            {

                string fileName = file.FileName;
                string ext = System.IO.Path.GetExtension(fileName).ToLower();
                if (ext.Equals(".mdb"))
                {
                    DateTime updateTime = LastUpdateTime.IsNullOrEmpty() ? DateTime.Now.AddDays(-1) : DateTime.Parse(LastUpdateTime);
                    var filePath = UploadHelper.Upload(file, "uploadFiles", "kq", false);
                    filePath = Server.MapPath("~/uploadFiles/kq.mdb");
                    var dicts = ReadDataFromAccess(filePath, updateTime);
                    if (!dicts.IsNullOrEmpty())
                    {
                        using (var tran = new TransactionScope())
                        {
                            Dao.Get().Delete<UserKq>().Where(o => o.CheckTime > updateTime).Execute();
                            var entities = dicts.ToEntities<UserKq>();
                            foreach (var item in entities)
                            {
                                item.Id = Utility.GetGuid();
                                Dao.Get().Insert(item);
                            }
                            tran.Complete();
                        }
                    }
                }
                else
                {
                    throw new Exception("只允许上传ACCESS文件");
                }
            }
            return RedirectToAction("AttendanceData");
        }

        public ActionResult Delete(string id)
        {
            var attachment = DBContext<AttachmentEntity>.Instance.Get(id);
            if (attachment.CreatorId.Equals(Sec.User.Id, StringComparison.OrdinalIgnoreCase) || Sec.IsSuperAdmin)
            {
                DBContext<AttachmentEntity>.Delete(id);
            }
            else
            {
                throw new FoxOneException("您没有执行此操作的权限！");
            }
            return RedirectToAction("Index", new { id = attachment.RelateId });
        }

        public ActionResult Upload(HttpPostedFileBase file, string relateId)
        {
            if (file != null)
            {
                if (!relateId.IsNullOrEmpty())
                {
                    string fileName = file.FileName;
                    string ext = System.IO.Path.GetExtension(fileName).ToLower();
                    fileName = fileName.Replace(ext, "") + DateTime.Now.ToString("_yyyy_MM_dd_HH_mm_ss");
                    var filePath = UploadHelper.Upload(file, "uploadFiles", fileName, true);
                    DBContext<AttachmentEntity>.Insert(new AttachmentEntity()
                    {
                        Id = Utility.GetGuid(),
                        CreateTime = DateTime.Now,
                        CreatorId = Sec.User.Id,
                        FileName = file.FileName,
                        FilePath = filePath,
                        FileSize = file.ContentLength,
                        FileType = System.IO.Path.GetExtension(filePath),
                        RentId = 1,
                        RelateId = relateId
                    });
                }
            }
            return RedirectToAction("Index", new { id = relateId });
        }

        public FileResult Download(string id)
        {
            var attachment = DBContext<AttachmentEntity>.Get(id);
            if (attachment == null)
            {
                throw new PageNotFoundException();
            }
            //return File(System.IO.Path.Combine(SysConfig.AppSettings["FileSystem"], attachment.FilePath), "application/octet-stream", attachment.FileName);
            return File(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, attachment.FilePath), "application/octet-stream", attachment.FileName);
        }


    }


}