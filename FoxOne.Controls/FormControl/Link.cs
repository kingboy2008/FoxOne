using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FoxOne.Core;
namespace FoxOne.Controls
{
    public class Link : FormControlBase
    {
        protected override string TagName
        {
            get { return "a"; }
        }

        public Link()
        {
            CssClass = "btn btn-default";
        }

        public string Url { get; set; }

        public string Text { get; set; }

        public bool AppendUploadUrl { get; set; }

        internal override void AddAttributes()
        {
            base.AddAttributes();
            string url = Url.IsNullOrEmpty() ? Value : Url;
            if (url.IsNotNullOrEmpty() && AppendUploadUrl)
            {
                url = "../" + url;
            }
            Attributes["href"] = url;
            Attributes["target"] = "_blank";
            Attributes["style"] = "color:red;text-decoration:underline";
        }

        protected override string RenderInner()
        {
            return Text.IsNullOrEmpty() ? Value : Text;
        }
    }
}
