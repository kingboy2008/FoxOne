using FoxOne.Business;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FoxOne.Core;
using System.ComponentModel;
namespace FoxOne.Controls
{
    /// <summary>
    /// 翻页组件
    /// </summary>
    [DisplayName("翻页组件")]
    public class Pager : PageControlBase, ITargetId
    {
        public Pager()
        {
            PrePageSize = "10,20,50,100";
            DisplayIndexCount = 10;
            PageSize = 10;
            CurrentPageIndex = 1;
            CssClass = "data-pager";
        }

        /// <summary>
        /// 每页显示条数
        /// </summary>
        [DisplayName("每页显示记录")]
        public int PageSize { get; set; }

        /// <summary>
        /// 总记录数
        /// </summary>
        [DisplayName("总记录数")]
        [FormField(Editable = false)]
        public int RecordCount { get; set; }

        /// <summary>
        /// 当前显示页
        /// </summary>
        [DisplayName("当前显示页")]
        [FormField(Editable = false)]
        public int CurrentPageIndex { get; set; }

        /// <summary>
        /// 预定义每页显示记录
        /// </summary>
        [DisplayName("预定义每页显示记录")]
        public string PrePageSize { get; set; }

        /// <summary>
        /// 显示页码链接数
        /// </summary>
        [DisplayName("显示页码链接数")]
        public int DisplayIndexCount { get; set; }

        public void SetTarget(IList<IControl> components)
        {

        }

        public override string Render()
        {
            Attributes["pageCount"] = PageCount.ToString();
            return base.Render();
        }

        public int PageCount
        {
            get
            {
                int pageCount = RecordCount / PageSize;
                if (RecordCount % PageSize != 0)
                {
                    pageCount++;
                }
                return pageCount;
            }
        }

        public override string RenderContent()
        {
            if (PageCount == 1) return "共{0}条记录".FormatTo(RecordCount);
            int displayCount = Math.Min(PageCount, DisplayIndexCount);
            string pagerItemTemplate = TemplateGenerator.GetPagerItemTemplate();
            string pagerSizeTemplate = TemplateGenerator.GetPagerSizeTemplate();
            StringBuilder result = new StringBuilder();
            if (CurrentPageIndex > 1)
            {
                result.AppendFormat(pagerItemTemplate, 1, "", "首页");
                result.AppendFormat(pagerItemTemplate, "Pre", "", "上一页");
            }
            int startIndex = CurrentPageIndex, endIndex = displayCount;
            if(startIndex+DisplayIndexCount>PageCount)
            {
                endIndex = PageCount;
                startIndex = PageCount - DisplayIndexCount + 1;
            }
            else
            {
                endIndex = startIndex + DisplayIndexCount - 1;
            }
            if(startIndex<=0)
            {
                startIndex = 1;
            }
            for (int i = startIndex; i <= endIndex; i++)
            {
                result.AppendFormat(pagerItemTemplate, i, i == CurrentPageIndex ? "class=\"active\"" : "", i);
            }
            if (CurrentPageIndex < PageCount)
            {
                result.AppendFormat(pagerItemTemplate, "Next", "", "下一页");
                result.AppendFormat(pagerItemTemplate, PageCount, "", "末页");
            }
            string pageSizeSet = string.Empty;
            PrePageSize.Split(',').ForEach((a) =>
            {
                string active = string.Empty;
                if (a.Equals(PageSize.ToString()))
                {
                    active = "class=\"active\"";
                }
                pageSizeSet += pagerSizeTemplate.FormatTo(a, active, a);
            });
            return TemplateGenerator.GetPagerTemplate().FormatTo(result.ToString(), pageSizeSet, RecordCount);
        }

        [DisplayName("目标组件ID")]
        public string TargetControlId
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 翻页控件显示位置
    /// </summary>
    public enum PagerPosition
    {
        /// <summary>
        /// 表格上方
        /// </summary>
        [Description("表格上方")]
        Top,

        /// <summary>
        /// 表格下方
        /// </summary>
        [Description("表格下方")]
        Bottom,

        /// <summary>
        /// 上下方均显示
        /// </summary>
        [Description("上下方均显示")]
        Both
    }
}
