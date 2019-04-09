using System;
using System.Linq;
using FoxOne.Core;
using FoxOne.Data;
using FoxOne.Business;
using FoxOne.Business.Environment;
using FoxOne.Workflow.Kernel;
using FoxOne.Workflow.Business;
using FoxOne.Workflow.DataAccess;
using System.Threading.Tasks;
using FoxOne.Business.DDSDK.Entity;
using FoxOne.Business.DDSDK.Service;
using FoxOne.Business.Security;
using FoxOne.Business.DDSDK;

namespace FoxOne.Web
{
    /// <summary>
    /// 注册中心
    /// </summary>
    public static class RegisterCenter
    {
        private static bool HasRegisterType = false;
        private static bool HasRegisterEntityEvent = false;
        private readonly static object obj = new object();

        /// <summary>
        /// 注册接口实现类
        /// </summary>
        public static void RegisterType()
        {
            if (!HasRegisterType)
            {
                lock (obj)
                {
                    if (!HasRegisterType)
                    {
                        ObjectHelper.RegisterType<ILangProvider, RLangProvider>();
                        ObjectHelper.RegisterType<ICache, HttpRuntimeCache>();
                        ObjectHelper.RegisterType<IEmailSender, SmtpEmailSender>();

                        ObjectHelper.RegisterType<IDepartment, Department>();
                        ObjectHelper.RegisterType<IUser, User>();
                        ObjectHelper.RegisterType<IRole, Role>();
                        ObjectHelper.RegisterType<IPermission, Permission>();
                        ObjectHelper.RegisterType<IRoleType, RoleType>();
                        ObjectHelper.RegisterType<IUserRole, UserRole>();
                        ObjectHelper.RegisterType<IRolePermission, RolePermission>();
                        ObjectHelper.RegisterType<IRoleTypePermission, RoleTypePermission>();
                        ObjectHelper.RegisterType<IDURPProperty, DURPProperty>();

                        ObjectHelper.RegisterType<ISqlParameters, EnvParameters>("Env");
                        ObjectHelper.RegisterType<ISqlParameters, HttpContextProvider>("HttpContext");

                        ObjectHelper.RegisterType<IWorkflow, BusinessWorkflow>();
                        ObjectHelper.RegisterType<IWorkflowBuilder, WorkflowBuilder>();
                        ObjectHelper.RegisterType<IWorkDay, WorkDay>();
                        ObjectHelper.RegisterType<ITransition, BusinessTransition>();
                        ObjectHelper.RegisterType<IWorkflowInstance, WorkflowInstance>();
                        ObjectHelper.RegisterType<IWorkflowItem, WorkflowItem>();
                        ObjectHelper.RegisterType<IWorkflowItem, WorkflowItemRead>("Read");
                        ObjectHelper.RegisterType<IWorkflowApplication, WorkflowApplication>();
                        ObjectHelper.RegisterType<IWorkflowDefinition, WorkflowDefinition>();
                        ObjectHelper.RegisterType<IWorkflowContext, WorkflowContext>();
                        ObjectHelper.RegisterType<IWorkflowChoice, WorkflowChoice>();
                        ObjectHelper.RegisterType<IWorkflowInstanceService, WorkflowInstanceService>();
                        ObjectHelper.RegisterType<IWorkDayService, WorkDayService>();

                        ObjectHelper.RegisterType(typeof(IService<>), typeof(CommonService<>));

                        HasRegisterType = true;
                    }
                }
            }
        }

        /// <summary>
        /// 注册实体增删改查前后触发的事件的处理逻辑
        /// </summary>
        public static void RegisterEntityEvent()
        {
            string mailDomail = MailUserService.Mail_Domain;
            if (!HasRegisterEntityEvent)
            {
                lock (obj)
                {
                    if (!HasRegisterEntityEvent)
                    {
                        EntityEventManager.RegisterEvent<IDepartment>(EventStep.Before, EventType.Insert, d =>
                        {
                            var o = d as IDepartment;
                            if (DBContext<IDepartment>.Instance.Where(k => k.Name.Equals(o.Name, StringComparison.OrdinalIgnoreCase)).Count() > 0)
                            {
                                throw new FoxOneException("Department_Name_Already_In_Use");
                            }
                            o.Alias = o.Name;
                            o.Level = o.Parent.Level + 1;
                            o.RentId = 1;
                            o.WBS = o.Parent.WBS + (o.Parent.Childrens.Count() + 1).ToString().PadLeft(3, '0');

                            if (SysConfig.IsProductEnv)
                            {
                                var dd_dept = new DDDepartmentInfo();
                                dd_dept.id = DDHelper.GetDDDepartId();
                                dd_dept.name = o.Name;
                                dd_dept.parentid = o.Parent.Code.ConvertTo<int>();
                                dd_dept.order = 100;
                                var ddId = DDDepartmentService.Create(dd_dept);
                                o.Code = ddId.ToString();

                                //var mailDept = new MailDepartment()
                                //{
                                //    Name = o.Name,
                                //    ParentId = dd_dept.parentid.ToString(),
                                //    DepartmentId = ddId.ToString()
                                //};
                                //new MailUserService().CreateDept(mailDept);
                            }
                            return true;
                        });

                        EntityEventManager.RegisterEvent<IDepartment>(EventStep.Before, EventType.Update, d =>
                        {
                            var o = d as IDepartment;
                            if (SysConfig.IsProductEnv)
                            {
                                var dd_dept = new DDDepartmentInfo();
                                dd_dept.name = o.Name;
                                dd_dept.parentid = o.Parent.Code.ConvertTo<int>();
                                dd_dept.id = o.Code.ConvertTo<int>();
                                DDDepartmentService.Update(dd_dept);

                                //var mailDept = new MailDepartment()
                                //{
                                //    Name = o.Name,
                                //    ParentId = dd_dept.parentid.ToString(),
                                //    DepartmentId = dd_dept.id.ToString()
                                //};
                                //new MailUserService().UpdateDept(mailDept);
                            }
                            return true;
                        });

                        EntityEventManager.RegisterEvent<IDepartment>(EventStep.Before, EventType.Delete, d =>
                        {
                            
                            var o = d as IDepartment;

                            //有子部门或用户的部门不能删除
                            if(!o.Childrens.IsNullOrEmpty() || !o.Member.IsNullOrEmpty())
                            {
                                throw new FoxOneException("Can_Not_Delete_Department_With_Children_Or_Member");
                            }

                            if (SysConfig.IsProductEnv)
                            {
                                //new MailUserService().DeleteDept(o.Code);
                                DDDepartmentService.Delete(o.Code.ConvertTo<int>());
                            }

                           
                            //o.Member.ForEach(k =>
                            //{
                            //    DBContext<IUser>.Delete(k);
                            //});

                            o.Roles.ForEach(k =>
                            {
                                DBContext<IRole>.Delete(k);
                            });

                            //o.Childrens.ForEach(k =>
                            //{
                            //    DBContext<IDepartment>.Delete(k);
                            //});
                            return true;
                        });

                        EntityEventManager.RegisterEvent<IUser>(EventStep.Before, EventType.Delete, d =>
                        {
                            var o = d as IUser;
                            if (SysConfig.IsProductEnv)
                            {
                                if (o.Mail.IsNotNullOrEmpty() && o.Mail.EndsWith(mailDomail))
                                {
                                    //new MailUserService().DeleteUser(o.Mail);
                                }
                                try
                                {
                                    DDUserService.Delete(o.Code);
                                }
                                catch { }
                            }
                            var userRoles = DBContext<IUserRole>.Instance.Where(j => j.UserId.Equals(o.Id, StringComparison.OrdinalIgnoreCase));
                            userRoles.ForEach(k =>
                            {
                                DBContext<IUserRole>.Delete(k);
                            });
                            return true;
                        });

                        EntityEventManager.RegisterEvent<IUser>(EventStep.Before, EventType.Update, d =>
                        {
                            var o = d as IUser;
                            if (DBContext<IUser>.Instance.Count(j => (j.LoginId == o.LoginId || j.MobilePhone == o.MobilePhone) && j.Id != o.Id) > 0)
                            {
                                throw new FoxOneException("LoginId_Or_MobilePhone_Alerady_Exist");
                            }
                            if (SysConfig.IsProductEnv)
                            {
                                if (o.Status.Equals(DefaultStatus.Disabled.ToString(), StringComparison.OrdinalIgnoreCase))
                                {
                                    try
                                    {
                                        if (o.Mail.IsNotNullOrEmpty() && o.Mail.EndsWith(mailDomail))
                                        {
                                            //new MailUserService().DeleteUser(o.Mail);
                                        }
                                        DDUserService.Delete(o.Code);
                                    }
                                    catch { }
                                }
                                else
                                {
                                    int deptId = o.Department.Code.ConvertTo<int>();
                                    var mailUser = new MailUser() { DepartmentId = deptId.ToString(), Mobile = o.MobilePhone, Name = o.Name, UserId = o.LoginId + mailDomail };
                                    if (o.Mail.IsNullOrEmpty())
                                    {
                                        //new MailUserService().CreateUser(mailUser);
                                        o.Mail = o.LoginId + mailDomail;
                                    }
                                    else
                                    {
                                        //new MailUserService().UpdateUser(mailUser);
                                    }
                                    var dduser = new DDUserCreateInfo
                                    {
                                        mobile = o.MobilePhone,
                                        name = o.Name,
                                        department = new int[] { deptId },
                                        userid = o.Code,
                                        email = o.Mail
                                    };
                                    DDUserService.Update(dduser);
                                }
                            }
                            return true;
                        });

                        EntityEventManager.RegisterEvent<IUser>(EventStep.Before, EventType.Insert, d =>
                        {
                            var o = d as IUser;
                            int i = 1;
                            if (o.LoginId.IsNullOrEmpty())
                            {
                                try
                                {
                                    string loginId = ChineseCode.GetAccount(o.Name);
                                    string pinyin = loginId;
                                    while (Dao.Get().Query<User>().Count(c => c.LoginId == loginId) > 0)
                                    {
                                        loginId = pinyin + i;
                                        i++;
                                    }
                                    o.LoginId = loginId;
                                    o.Password = Sec.Provider.EncryptPassword(pinyin + "@123456");
                                }
                                catch (Exception)
                                {
                                    throw new FoxOneException("无法自动生成账号，请手动填写");
                                }
                            }
                            else
                            {
                                if (DBContext<IUser>.Instance.Where(k => k.LoginId.Equals(o.LoginId, StringComparison.OrdinalIgnoreCase) || k.MobilePhone == o.MobilePhone).Count() > 0)
                                {
                                    throw new FoxOneException("LoginId_Or_MobilePhone_Alerady_Exist");
                                }
                            }
                            if (SysConfig.IsProductEnv)
                            {

                                int deptId = o.Department.Code.ConvertTo<int>();
                                if (o.Mail.IsNullOrEmpty())
                                {
                                    var mailUser = new MailUser() { DepartmentId = deptId.ToString(), Mobile = o.MobilePhone, Name = o.Name, UserId = o.LoginId + mailDomail };
                                    //if (new MailUserService().CreateUser(mailUser))
                                    //{
                                    //    o.Mail = o.LoginId + mailDomail;
                                    //}
                                }
                                var dduser = new DDUserCreateInfo()
                                {
                                    mobile = o.MobilePhone,
                                    name = o.Name,
                                    department = new int[] { deptId },
                                    email = o.Mail
                                };
                                dduser.userid = DDUserService.Create(dduser);
                                o.Code = dduser.userid;
                            }
                            return true;
                        });

                        EntityEventManager.RegisterEvent<IRole>(EventStep.Before, EventType.Delete, d =>
                        {
                            var o = d as IRole;
                            var userRoles = DBContext<IUserRole>.Instance.Where(j => j.RoleId.Equals(o.Id, StringComparison.OrdinalIgnoreCase));
                            userRoles.ForEach(k =>
                            {
                                DBContext<IUserRole>.Delete(k);
                            });
                            var permissions = DBContext<IRolePermission>.Instance.Where(j => j.RoleId.Equals(o.Id, StringComparison.OrdinalIgnoreCase));
                            permissions.ForEach(k =>
                            {
                                DBContext<IRolePermission>.Delete(k);
                            });
                            return true;
                        });


                        EntityEventManager.RegisterEvent<IRole>(EventStep.Before, EventType.Insert, d =>
                        {
                            var o = d as IRole;
                            var roles = DBContext<IRole>.Instance.Where(j => j.RoleTypeId.Equals(o.RoleTypeId, StringComparison.OrdinalIgnoreCase)
                                && j.DepartmentId.Equals(o.DepartmentId, StringComparison.OrdinalIgnoreCase));
                            if (roles.Count() > 0)
                            {
                                throw new FoxOneException("Role_Alerady_Exist");
                            }
                            return true;
                        });

                        EntityEventManager.RegisterEvent<IRoleType>(EventStep.Before, EventType.Delete, d =>
                        {
                            var o = d as IRoleType;
                            o.Roles.ForEach(k =>
                            {
                                DBContext<IRole>.Delete(k);
                            });
                            var permissions = DBContext<IRoleTypePermission>.Instance.Where(j => j.RoleTypeId.Equals(o.Id, StringComparison.OrdinalIgnoreCase));
                            permissions.ForEach(k =>
                            {
                                DBContext<IRoleTypePermission>.Delete(k);
                            });
                            return true;
                        });

                        EntityEventManager.RegisterEvent<IPermission>(EventStep.Before, EventType.Insert, d =>
                        {
                            var o = d as IPermission;
                            var temp = DBContext<IPermission>.Instance.FirstOrDefault(j => j.Code.Equals(o.Code, StringComparison.OrdinalIgnoreCase));
                            if (temp != null)
                            {
                                throw new FoxOneException("Permission_Code_Exist");
                            }
                            return true;
                        });

                        EntityEventManager.RegisterEvent<IPermission>(EventStep.Before, EventType.Delete, d =>
                        {
                            var o = d as IPermission;
                            var roleTypePermission = DBContext<IRoleTypePermission>.Instance.Where(j => j.PermissionId.Equals(o.Id, StringComparison.OrdinalIgnoreCase));
                            roleTypePermission.ForEach(k =>
                            {
                                DBContext<IRoleTypePermission>.Delete(k);
                            });

                            var rolePermission = DBContext<IRolePermission>.Instance.Where(j => j.PermissionId.Equals(o.Id, StringComparison.OrdinalIgnoreCase));
                            rolePermission.ForEach(k =>
                            {
                                DBContext<IRolePermission>.Delete(k);
                            });
                            return true;
                        });

                        EntityEventManager.RegisterEvent<PageEntity>(EventStep.Before, EventType.Delete, d =>
                        {
                            var o = d as PageEntity;
                            var components = DBContext<ComponentEntity>.Instance.Where(i => i.PageId.Equals(o.Id, StringComparison.OrdinalIgnoreCase));
                            if (!components.IsNullOrEmpty())
                            {
                                components.ForEach(k =>
                                {
                                    DBContext<ComponentEntity>.Delete(k);
                                });
                            }
                            var pageFile = DBContext<PageLayoutFileEntity>.Instance.Where(i => i.PageOrLayoutId.Equals(o.Id, StringComparison.OrdinalIgnoreCase)).ToList();
                            if (!pageFile.IsNullOrEmpty())
                            {
                                pageFile.ForEach(k =>
                                {
                                    DBContext<PageLayoutFileEntity>.Delete(k);
                                });
                            }
                            return true;
                        });

                        EntityEventManager.RegisterEvent<IWorkflowApplication>(EventStep.Before, EventType.Delete, d =>
                        {
                            var o = d as IWorkflowApplication;
                            if (DBContext<IWorkflowInstance>.Instance.Count(c => c.ApplicationId == o.Id) > 0)
                            {
                                throw new FoxOneException("当前流程应用有关联的实例，不能删除！");
                            }
                            return true;
                        });

                        EntityEventManager.RegisterEvent<IWorkflowDefinition>(EventStep.Before, EventType.Delete, d =>
                        {
                            var o = d as IWorkflowDefinition;
                            if (DBContext<IWorkflowApplication>.Instance.Count(c => c.WorkflowId == o.Id) > 0)
                            {
                                throw new FoxOneException("当前流程定义有关联的流程应用，不能删除！");
                            }
                            var components = DBContext<ComponentEntity>.Instance.Where(i => i.PageId.Equals(o.Id, StringComparison.OrdinalIgnoreCase));
                            if (!components.IsNullOrEmpty())
                            {
                                components.ForEach(k =>
                                {
                                    DBContext<ComponentEntity>.Delete(k);
                                });
                            }
                            return true;
                        });
                        WorkflowEventManager.RegisterWorkItemEvent(EventStep.After, ItemActionType.Insert, (instance, workitem) =>
                        {
                            if (workitem.PasserUserId==null || workitem.PasserUserId.Equals(workitem.PartUserId, StringComparison.OrdinalIgnoreCase))
                            {
                                return;
                            }
                            Task.Factory.StartNew(() =>
                            {
                                if (SysConfig.IsProductEnv)
                                {
                                    if (instance.FlowTag == FlowStatus.Running && workitem.Status < WorkItemStatus.Finished && workitem.PartUserId.IsNotNullOrEmpty())
                                    {
                                        var user = DBContext<IUser>.Instance.Get(workitem.PartUserId);
                                        if (user != null)
                                        {
                                            //string message = string.Format("OA：{0}", instance.InstanceName);
                                            //if (user.MobilePhone.IsNotNullOrEmpty())
                                            //{
                                            //    bool result = Swj.Sms.SuppliersHelper.MSMHelper.SendSMS(user.MobilePhone, message, Swj.Sms.SuppliersHelper.MSMType.ChuangLan, Swj.Sms.SuppliersHelper.MSType.Identifying);
                                            //}

                                            //if (user.Mail.IsNotNullOrEmpty())
                                            //{
                                            //    message += ",请登陆http://oa.3weijia.com办理";
                                            //    MailHelper.SendMail(user.Mail, "您有新的OA审批单", message);
                                            //    Logger.GetLogger("Workflow").InfoFormat("{0}:已发送邮件通知:{1}", instance.Id, user.Name);
                                            //}
                                        }
                                    }
                                }
                            });
                        });
                        HasRegisterEntityEvent = true;
                    }
                }
            }

        }
    }
}