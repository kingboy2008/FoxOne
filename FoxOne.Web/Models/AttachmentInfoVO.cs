using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FoxOne.Web
{
    /// <summary>
    /// 附件信息视图对象
    /// </summary>
    public class AttachmentInfoVO
    {
        /// <summary>
        /// 附件ID
        /// </summary>
        public string FileId { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 文件图标
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// 文件大小
        /// </summary>
        public string FileSize { get; set; }

        /// <summary>
        /// 上传者
        /// </summary>
        public string CreatorName { get; set; }

        /// <summary>
        /// 上传时间
        /// </summary>
        public string CreateTime { get; set; }

        /// <summary>
        /// 能否删除此附件
        /// </summary>
        public bool CanDelete { get; set; }

        /// <summary>
        /// 是否本地附件
        /// </summary>
        public bool IsLocalResource { get; set; }

    }
}