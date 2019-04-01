using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FoxOne.Core;
using System.ComponentModel;

namespace FoxOne.Controls
{
    [DisplayName("图片域")]
    public class ImageField : FormControlBase
    {
        protected override string TagName
        {
            get
            {
                return "img";
            }
        }

        [DisplayName("高度")]
        public string Height { get; set; }


        [DisplayName("路径格式")]
        public string PathFormat { get; set; }

        internal override void AddAttributes()
        {
            if (!Height.IsNullOrEmpty())
            {
                if (Attributes.ContainsKey("style"))
                    Attributes["style"] += "height:{0}px;".FormatTo(Height);
                else
                    Attributes["style"] = "height:{0}px;".FormatTo(Height);
            }
            if(PathFormat.IsNotNullOrEmpty())
            {
                Value = PathFormat.FormatTo(Value);
            }
            Attributes["src"] = Value;
            base.AddAttributes();
        }
    }
}
