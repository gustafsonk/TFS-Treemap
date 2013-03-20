(function ($) {
    // register namespace
    $.extend(true, window, {
        "Slick": {
            "Editors": {
                "Select": SelectCellEditor
            }
        }
    });

    // (I doubt some of these functions work 'correctly')
    function SelectCellEditor(args) {
        var $select;
        var defaultValue;
        var scope = this;

        this.init = function () {
            var overrideOptions = args.item[args.column.id + "OptionsOverride"];
            var optionNames;
            if (overrideOptions !== undefined) {
                optionNames = overrideOptions.split(',');
            }
            else if (args.column.options) {
                optionNames = args.column.options.split(',');
            }
            else {
                throw "SelectCellEditor: Column is missing options for dropdown.";
            }

            var options = "";
            for (var index in optionNames) {
                var optionName = optionNames[index];
                options += "<OPTION value='" + optionName + "'>" + optionName + "</OPTION>";
            }
            $select = $("<SELECT tabIndex='0' id='edit-select'>" + options + "</SELECT>");
            $select.appendTo(args.container);
            $select.focus();
        };

        this.destroy = function () {
            $select.remove();
        };

        this.focus = function () {
            $select.focus();
        };

        this.loadValue = function (item) {
            defaultValue = item[args.column.field];
            $select.val(defaultValue);
        };

        this.serializeValue = function () {
            return $select.val();
        };

        this.applyValue = function (item, state) {
            item[args.column.field] = state;
        };

        this.isValueChanged = function () {
            return ($select.val() != defaultValue);
        };

        this.validate = function () {
            return {
                valid: true,
                msg: null
            };
        };

        this.init();
    }
})(jQuery);