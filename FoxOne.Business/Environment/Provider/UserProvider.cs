using FoxOne.Business.Security;
using FoxOne.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FoxOne.Business.Environment
{
    public class UserProvider : IEnvironmentProvider
    {
        public string Prefix
        {
            get
            {
                return "User";
            }
        }

        public object Resolve(string name)
        {
            if (name == "RoleName")
            {
                return string.Join("|", Sec.User.Roles.Select(o => o.RoleType.Name));
            }
            if(name == "IsSuperAdmin")
            {
                return Sec.IsSuperAdmin;
            }
            object value = Sec.User;
            FastType fastType = null;
            if (name.IndexOf(".") > 0)
            {
                string[] propAtt = name.Split('.');
                foreach (var item in propAtt)
                {
                    fastType = FastType.Get(value.GetType());
                    var getter = fastType.GetGetter(item);
                    if (getter != null)
                    {
                        value = getter.GetValue(value);
                    }
                    else
                    {
                        throw new FoxOneException("Express_Not_Found", name);
                    }
                }
                return value;
            }
            else
            {
                fastType = FastType.Get(value.GetType());
                var getter = fastType.GetGetter(name);
                if (getter != null)
                {
                    return getter.GetValue(Sec.User);
                }
                else
                {
                    return Sec.User.Properties.TryGetValue(name, out value) ? value : null;
                }
            }
        }
    }
}
