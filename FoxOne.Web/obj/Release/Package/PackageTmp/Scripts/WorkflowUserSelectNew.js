/// <reference path="jquery-1.8.2.js" />
/// <reference path="common.js" />
(function (window, $) {
    var foxOne = window.foxOne;
    var workflow = function () { };
    workflow.prototype = {
        _STEP_LIST_DIV: 'stepListDiv',
        startParameter: { AppCode: '', InstanceName: '', DataLocator: '', ImportLevel: 0, SecurityLevel: 0 },
        runParameter: { IsSimulate: '0', Command: '', InstanceId: '', ItemId: 0, OpinionContent: '', OpinionArea: 0, UserChoice: [] },
        getNextStepUrl: '/Workflow/GetNextStep',
        startUrl: '/Workflow/Start',
        saveUrl: '/Workflow/Save',
        execUrl: '/Workflow/ExecCommand',
        successUrl: '/Workflow/FlowSuccess',
        start: function (appCode, instanceName, dataLocator) {
            var that = this;
            that.startParameter.AppCode = appCode;
            that.startParameter.InstanceName = instanceName;
            that.startParameter.DataLocator = dataLocator;
            foxOne.dataService(that.startUrl, that.startParameter, function (res) {
                that.runParameter.InstanceId = res;
                that.runParameter.ItemId = 1;
            });
        },
        getNewUserChoice: function () {
            return { StepName: '', Id: '', Name: '', DepartmentId: '' };
        },
        success: function () {
            var that = this;
            if (that.runParameter.IsSimulate == '1') {
                window.location.href = "/Workflow/AutoRun/" + that.runParameter.InstanceId;
            }
            else {
                window.location.href = that.successUrl + "?InstanceId=" + that.runParameter.InstanceId + "&ItemId=" + that.runParameter.ItemId;
            }
        },
        getNextStep: function () {
            var that = this;
            if (that.runParameter.InstanceId == '' || that.runParameter.ItemId == 0) {
                foxOne.alert("实例号和工作项号不能为空！");
                return;
            }
            foxOne.dataService(that.getNextStepUrl, that.runParameter, function (nextStep) {
                var canPostback = false;
                if (nextStep == null || nextStep.length == 0) {
                    foxOne.alert("无可用迁移！");
                    return;
                }
                if (nextStep != null && nextStep[0].StepName == "自动发送") {
                    that.success();
                }
                else {
                    $("#" + that._STEP_LIST_DIV).html("");
                    var initSelected = false;
                    $.each(nextStep, function (index, i) {
                        if (i.NeedUser) {
                            i.Icon = "activity";
                        }
                        else {
                            i.Icon = "end";
                        }
                        if (!i.Users || i.Users.length <= 0) {
                            i.UserItem = "<div class='no-user-item'>该步骤没有可选用户</div>";
                        }
                        else
                        {
                            i.UserItem = foxOne.modelListBinder(i.Users, $("#userItemTemplate").html());
                        }
                        if (i.LabelDescription == null) {
                            i.LabelDescription = i.Label;
                        }

                        if (!initSelected) {
                            if (i.Users && i.Users.length > 0) {
                                i.Selected = "step-item-selected";
                                initSelected = true;
                            }
                        }
                        else {
                            i.Selected = "";
                        }
                        var stepList = foxOne.modelViewBinder(i, $("#stepTemplate").html());
                        $("#" + that._STEP_LIST_DIV).append(stepList);
                    });
                    $.modalInner($(".user-select"), true, function () { },850, 550, window.top, false);
                }
            });
        },
        exec: function (command) {
            var that = this;
            if (that.runParameter.InstanceId == '' || that.runParameter.ItemId == 0) {
                foxOne.alert("实例号和工作项号不能为空！");
                return;
            }
            that.runParameter.Command = command;
            foxOne.dataService(that.execUrl, JSON.stringify(that.runParameter), function (data) {
                if (data == true) {
                    that.success();
                }
            }, "post", true, "application/json");
        },
        run: function () {
            try {
                var that = this;
                if (that.runParameter.InstanceId == '' || that.runParameter.ItemId == 0) {
                    foxOne.alert("实例号和工作项号不能为空！");
                    return;
                }
                that.getUserChoice();
                that.runParameter.Command = "run";
                if (that.runParameter.UserChoice.length > 0) {
                    foxOne.dataService(that.execUrl, that.runParameter, function (data) {
                        if (data == true) {
                            that.success();
                        }
                    }, "post", true);
                }
                else {
                    foxOne.alert("没有选中用户");
                }
            } catch (e) {
                foxOne.alert(e);
            }
        },
        getUserChoice: function () {
            var that = this;
            that.runParameter.OpinionArea = 0;
            that.runParameter.UserChoice = [];
            var selectedStep = $(".step-item-selected");
            if (selectedStep.length == 0) {
                throw "请至少选择一个步骤！";
            }
            selectedStep.each(function () {
                var step = $(this);
                if (step.attr("needuser") == "true") {
                    var selectUser = step.find(".user-item-selected");
                    if (selectUser.length <= 0) {
                        throw "步骤[" + step.attr("alias") + "]需要选择参与者！";
                    }
                    else {
                        selectUser.each(function () {
                            var choice = that.getNewUserChoice();
                            var node = $(this);
                            choice.StepName = step.attr("step");
                            choice.Id = node.attr("userid");
                            choice.Name = node.attr("name");
                            choice.DepartmentId = node.attr("deptid");
                            that.runParameter.UserChoice.push(choice);
                        });

                    }
                }
                else {
                    var choice = that.getNewUserChoice();
                    choice.StepName = $(this).attr("step");
                    that.runParameter.UserChoice.push(choice);
                }
            });
            if (selectedStep.attr("needUser") == "true" && selectedStep.find(".user-item-selected").length <= 0) {
                throw "步骤[" + selectedStep.attr("alias") + "]需要选择参与者！";
            }
        }
    }
    foxOne.workflow = new workflow();
})(window, jQuery);

$(function () {
    $("body").on("click", ".step-item", function (e) {
        if ($(this).hasClass("step-item-selected")) {
            $(".user-item-selected").removeClass("user-item-selected");
            $(".step-item-selected").removeClass("step-item-selected");
        }
        else {
            $(".user-item-selected").removeClass("user-item-selected");
            $(".step-item-selected").removeClass("step-item-selected");
            $(this).addClass("step-item-selected");
            var userItems = $(this).find(".user-item");
            if (userItems.length > 0) {
                userItems.eq(0).addClass("user-item-selected");
            }
        }
    });
    $("body").on("click", ".user-item", function (e) {
        $(".user-item-selected").removeClass("user-item-selected");
        $(this).addClass("user-item-selected");
        e.stopPropagation();
    });
    $("#btnOK").bind("click", function () {
        foxOne.workflow.run();
    });
    $("#btnCancel").bind("click", function () {
        $.closeModal(false);
    });
});