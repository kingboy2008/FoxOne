/// <reference path="../jquery-1.8.2.js" />
/// <reference path="../jquery.form.js" />
/// <reference path="../common.js" />
(function (window, $) {
    var foxOne = window.foxOne;
    $(window.document).on("submit", "[defaultForm]", function (e) {
        var _this = e.target;
        if ($.validation) {
            var validateInfo = $.validation.validate(_this);
            if (validateInfo.isError) {
                var ee = $.Event("form.validateError", validateInfo);
                $(_this).trigger(ee);
                foxOne.alert(ee.errorInfo);
                return false;
            }
        }
        var form = $(_this);
        form.find("[disabled]").removeAttr("disabled");
        var widget = form.closest("[widget]");
        var param = {};
        param[foxOne.ctrlId] = widget.attr("id");
        param[foxOne.pageId] = widget.attr("pageId");
        var url = foxOne.buildUrl(form.attr('action'), param);
        form.ajaxSubmit({
            type: 'post',
            url: url,
            success: function (response) {
                if (response.Result) {
                    if (response.NoAuthority) {
                        foxOne.alert(response.ErrorMessage);
                    }
                    else {
                        if (response.LoginTimeOut) {
                            foxOne.alert("登录超时，请重新登录");
                        }
                        else {
                            var res = response.Data;
                            try {
                                var afterSubmit = $.Event("form.afterSubmit", { d: res });
                                form.trigger(afterSubmit);
                                if (window.top && window.top.onDialogClose && window.top.onDialogClose.length > 0) {
                                    window.top.onDialogClose.pop()(res);
                                }
                            } catch (e) {
                                foxOne.alert(e);
                            }
                        }
                    }
                }
                else {
                    foxOne.alert(response.ErrorMessage);
                }
            },
            error: function (xhr, text, error) {
                foxOne.alert(xhr.responseText);
            }
        });
        return false;
    });

    $("[defaultForm]").find("input[type='text'],select,textarea").each(function () {
        $(this).attr("original-value", $(this).val());
    });
})(window, jQuery);