/// <reference path="../jquery-1.8.2.js" />
/// <reference path="../common.js" />

$("#DepartmentListTree").bind("nodeClick", function (e) {
    var setting = foxOne.setting("DepartmentListTable");
    setting.ParentId = e.id;
    foxOne.refresh("DepartmentListTable");
});
$("body").on("click", ".dept-role-add", function (e) {
    var p = $(this);
    var deptname = p.attr("deptname");
    $.modal({
        title: '为部门【' + deptname + '】新增角色',
        url: '/Page/RoleEdit?DepartmentId=' + p.attr("deptid") + '&_FORM_VIEW_MODE=Insert',
        width: 700,
        height: 400,
        overlayClose: true,
        onClose: function (res) {
            if (res) {
                foxOne.refresh("DepartmentListTable");
            }
        }
    });
});

$("body").on("click", ".dept-role", function (e) {
    var a = $(e.target);
    if (a.is("a")) {
        var roleId = a.attr("roleid");
        var deptname = a.attr("deptname");
        var rolename = a.attr("rolename");
        $.modal({
            title: '为部门【' + deptname + '】的角色【' + rolename + '】分配用户',
            url: '/Page/UserSelector?RoleId=' + roleId,
            width: 900,
            height: 600,
            overlayClose: true,
            onClose: function (res) {
                if (res) {
                    foxOne.refresh("DepartmentListTable");
                }
            }
        });
    }
});
$("body").on("click", ".role-user", function (e) {
    var span = $(e.target);
    var roleId = span.attr("roleid");
    var userId = span.attr("userid");
    if (roleId == '' || userId == '') return;
    if (span.is("span")) {
        if (confirm("您确认删除该角色用户吗？")) {
            foxOne.dataService("/Entity/UserRole", { UserId: userId, RoleId: roleId, Add: false }, function (res) {
                if (res) {
                    foxOne.refresh("DepartmentListTable");
                }
            });
        }
    }
});

$("body").on("click", ".app-item", function (e) {
    var div = $(this);
    var url = div.attr("url");
    var appId = div.attr("id");
    if (url != "") {
        $.modal({
            url: '/Workflow/Batch/' + appId, width: 950, height: 600, overlayClose: true, onClose: function (res) {
                if (res) {
                    foxOne.refresh("WorkItemListTable");
                    foxOne.refresh("WorkItemListRepeater");
                }
            }
        });
    }
    else {
        var setting = foxOne.setting("WorkItemListTable");
        setting.ApplicationId = appId;
        setting[foxOne.pageId] = 1;
        foxOne.refresh("WorkItemListTable");
    }
});


$("#supplier").bind("change", function () {
    if ($(this).val() != '') {
        foxOne.dataService("sqlid:crud.oasupplier.get", { id: $(this).val() }, function (res) {
            if (res && res.length > 0) {
                var supplier = res[0];
                var idCtrlMap = ['Bank-accountbankname', 'BankAccount-account', 'Provinces-province', 'City-city', 'ContactPhone-phone', 'ContactMail-mail'];
                for (var i = 0; i < idCtrlMap.length; i++) {
                    var temp = idCtrlMap[i].split('-');
                    $("#" + temp[1]).val(supplier[temp[0]]);
                }
            }
        });
    }
});


$(".app-item").bind("click", function () {
    var url = $(this).attr("url");
    if (url != "") {
        $.modal({
            url: '/Page/' + url, width: 950, height: 600, overlayClose: true, onClose: function (res) {
                if (res) {
                    foxOne.refresh("WorkItemListTable");
                }
            }
        });
    }
    else {
        var setting = foxOne.setting("WorkItemListTable");
        setting.ApplicationId = $(this).attr("id");
        setting[foxOne.pageId] = 1;
        foxOne.refresh("WorkItemListTable");
    }
});


function batchAgree(a) {
    var table = $("div[Widget='Table']");
    var ids = [];
    table.find(":checked").not("[checkAll]").each(function () {
        var tr = $(this).closest("tr").data();
        var id = tr.key
        if (id && id != '') {
            ids.push(id);
        }
    });
    if (ids.length <= 0) {
        foxOne.alert("请至少选择一条记录！");
        return;
    }
    agree(ids.join(','), a);
}

function agree(id, r) {
    foxOne.dataService("/Workflow/BatchRun", { id: id, agree: r }, function (res) {
        if (res) {
            foxOne.refresh("form_leaveapprovalListTable");
        }
    });
}

$(function () {
    onWindowScroll();
    $("#DDUserListRepeater").scroll(onWindowScroll);
});

var handler;
function onWindowScroll() {
    if (handler) {
        window.clearTimeout(handler);
    }
    handler = window.setTimeout(function () {
        if ($("[src1]").length == 0) {
            $("#DDUserListRepeater").unbind("scroll");
            return;
        }
        $("[src1]").each(function () {
            var scrollTopValue = 0;
            if (document.documentElement && document.documentElement.scrollTop) {
                scrollTopValue = document.documentElement.scrollTop;
            }
            else if (document.body) {
                scrollTopValue = document.body.scrollTop;
            }
            if ($(this).position().top < $(window).height() + scrollTopValue && $(this).attr("src1") != '') {
                $(this).attr("src", $(this).attr("src1"));
                $(this).removeAttr("src1");
            }
        });
    }, 100);
}

function syncFromDD() {
    foxOne.dataService("/DD/SyncCurrentUser", {}, function (res) {
        if (res) {
            foxOne.alert("同步完成！");
            foxOne.refresh("DDUserListRepeater");
        }
    }, "POST", true);
}