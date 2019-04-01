using FoxOne.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FoxOne.Core;
using System.Web.Mvc;
using System.ComponentModel;
using System.Web.Script.Serialization;
namespace FoxOne.Controls
{

    /// <summary>
    /// 下拉框
    /// </summary>
    [DisplayName("下拉框")]
    public class DropDownList : KeyValueControlBase
    {

        public DropDownList() : base()
        {
            AppendEmptyOption = true;
            EmptyOptionText = "==请选择==";
        }

        protected override string TagName
        {
            get { return "select"; }
        }

        protected override string RenderInner()
        {
            StringBuilder content = new StringBuilder();
            var items = GetData();
            string optionTemplate = "<option value=\"{0}\" {1} >{2}</option>";
            foreach (var item in items)
            {
                content.AppendLine(optionTemplate.FormatTo(item.Value, item.Checked ? "selected=\"selected\"" : "", item.Text));
            }
            return content.ToString();
        }

        public override string MobileValue
        {
            get
            {
                string result = string.Empty;
                if (!Value.IsNullOrEmpty())
                {
                    var items = GetData();
                    if (!items.IsNullOrEmpty())
                    {
                        items.ForEach((o) =>
                        {
                            if (Value.Split(',').Contains(o.Value, StringComparer.OrdinalIgnoreCase))
                            {
                                result += "," + o.Text;
                            }
                        });
                    }
                }
                return result == string.Empty ? result : result.Substring(1);
            }
        }

        public override string RenderMobile()
        {
            //return base.RenderMobile();
            //return
            var items = GetData();
            List<string> itemStrList = new List<string>();
            var mValue = MobileValue;
            var displayText = mValue;
            foreach (var item in items)
            {
                itemStrList.Add("{{\"key\":\"{0}\",\"value\":\"{1}\"}}".FormatTo( item.Text, item.Value));
                if (item.Value == mValue)
                {
                    displayText = item.Text;
                }
            }
            return $"<div class=\"weui-cell {MobileControlName}\">" +
                        $"<div class=\"weui-cell__hd\">" +
                            $"<label class=\"weui-label\">{Label}：</label>" +
                        $"</div>" +
                        $"<div class=\"weui-cell__bd\" data-SelectItem='[{ string.Join(",",itemStrList) }]'>" +
                            $"<div class=\"select\">{displayText}</div>" +
                            $"<input id='{Id}' name='{Name}' type='hidden' value='{mValue}' />" +
                        $"</div>" +
                $"</div>";

        }

        public override string MobileControlName
        {
            get
            {
                return "select-type";
            }
        }
    }
}
