using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FoxOne.Core;
using System.ComponentModel;
using System.Web;
using FoxOne.Business;
namespace FoxOne.Controls
{

    /// <summary>
    /// KV文本框
    /// </summary>
    [DisplayName("KV文本框")]
    public class TextValueTextBox : FormControlBase
    {
        public TextValueTextBox()
        {
            DialogWidth = 800;
            DialogHeight = 400;
        }

        protected override string TagName
        {
            get { return "div"; }
        }
        public string TextID { get; set; }

        [DisplayName("选择器名称")]
        public string SelectType { get; set; }

        [DisplayName("是否多选")]
        public bool IsMulitle { get; set; }

        public ShowType ShowType { get; set; }

        public int DialogWidth { get; set; }

        public int DialogHeight { get; set; }

        private string GetDisplayText()
        {
            var texts = new List<string>();
            if (!Value.IsNullOrEmpty() && !SelectType.IsNullOrEmpty())
            {
                var page = PageBuilder.BuildPage(SelectType);
                if (page != null && page.Controls.Count > 0)
                {
                    IFieldConverter listDs = null;
                    page.Controls.ForEach((o) =>
                    {
                        if (o is IListDataSourceControl)
                        {
                            listDs = (o as IListDataSourceControl).DataSource as IFieldConverter;
                            return;
                        }
                        else if (o is ICascadeDataSourceControl)
                        {
                            listDs = (o as ICascadeDataSourceControl).DataSource as IFieldConverter;
                            return;
                        }
                    });
                    if (listDs != null)
                    {
                        if (IsMulitle)
                        {
                            foreach (var v in Value.Split(','))
                            {
                                texts.Add(listDs.Converter(Id, v, null).ToString());
                            }
                        }
                        else
                        {
                            texts.Add(listDs.Converter(Id, Value, null).ToString());
                        }
                    }
                }
            }
            string text = Value;
            if (texts.Count > 0)
            {
                text = string.Join(",", texts.ToArray());
            }
            return text;
        }

        public override string Render()
        {
            if (Visiable)
            {
                AddAttributes();
                if (TextID.IsNullOrEmpty())
                {
                    TextID = "{0}_Text".FormatTo(Id);
                }

                var textBox = new TextBox() { Id = TextID, Name = TextID, Value = GetDisplayText() };
                if (!Attributes.IsNullOrEmpty())
                {
                    foreach (var attr in Attributes)
                    {
                        textBox.Attributes[attr.Key] = attr.Value;
                    }
                }
                textBox.Attributes["readonly"] = "readonly";
                textBox.Attributes["data-selector"] = SelectType;
                textBox.Attributes["data-showtype"] = ShowType.ToString();
                textBox.Attributes["data-multiple"] = IsMulitle.ToString().ToLower();
                textBox.Attributes["data-target"] = Id;
                textBox.Attributes["data-dialogheight"] = DialogHeight.ToString();
                textBox.Attributes["data-dialogwidth"] = DialogWidth.ToString();
                
                var hidden = new HiddenField() { Id = Id, Name = Id, Value = Value, Validator = Validator };
                string result = hidden.Render() + textBox.Render();
                return ContainerTemplate.FormatTo(Id, Label, result, Description);
            }
            return string.Empty;
        }

        public override string MobileValue
        {
            get
            {
                return GetDisplayText();
            }
        }

        public override string RenderMobile()
        {
            //return base.RenderMobile();
            return $"<div class=\"weui-cell {MobileControlName}\"><div class=\"weui-cell__hd\"><label class=\"weui-label\">{Label}：</label></div><div class=\"weui-cell__bd\"><input id='{Id}' name='{Name}'  type='hidden' value='{Value}' /><input id='{Id}_Text' name='{Name}_Text'  type='text'  readonly='readonly' value='{MobileValue}' /></div></div>";
        }
    }

    public enum ShowType
    {
        SlideDown,
        Modal
    }
}
