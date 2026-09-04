$(document).ready(function () {

    tinymce.init({
        mode: "textareas",
        editor_selector: "mceTitleEng",
        statusbar: false,
        forced_root_block: false,
        setup: function (ed) {


            ed.on('init', function () {

                this.getDoc().body.style.fontFamily = 'Calibri';
                this.getDoc().body.style.fontSize = '12px';
            });
        },
        height: 100,
        plugins: [
            "advlist autolink autosave link image lists charmap print preview hr anchor pagebreak spellchecker",
            "searchreplace wordcount visualblocks visualchars code fullscreen insertdatetime media nonbreaking",
            "table  directionality emoticons template textcolor paste textcolor colorpicker textpattern "
        ],
        toolbar1: "fontselect fontsizeselect | subscript superscript | alignleft aligncenter alignright alignjustify | bullist numlist | outdent indent | undo redo | link image code",
        toolbar2: "bold italic underline   cut copy  | searchreplace | ltr rtl | forecolor backcolor| table",
        toolbar_items_size: 'small',

        menubar: false
    });

    tinymce.init({
        mode: "textareas",
        editor_selector: "mceTitleArb",
        statusbar: false,
        directionality: 'rtl',
        forced_root_block: false,
        setup: function (ed) {


            ed.on('init', function () {

                this.getDoc().body.style.fontFamily = 'Calibri';
                this.getDoc().body.style.fontSize = '12px';
            });
        },
        height: 100,
        plugins: [
            "advlist autolink autosave link image lists charmap print preview hr anchor pagebreak spellchecker",
            "searchreplace wordcount visualblocks visualchars code fullscreen insertdatetime media nonbreaking",
            "table  directionality emoticons template textcolor paste textcolor colorpicker textpattern "
        ],
        toolbar1: "fontselect fontsizeselect | subscript superscript | alignleft aligncenter alignright alignjustify | bullist numlist | outdent indent | undo redo | link image code",
        toolbar2: "bold italic underline   cut copy  | searchreplace | ltr rtl | forecolor backcolor| table",
        toolbar_items_size: 'small',

        menubar: false
    });

    tinymce.init({
        mode: "textareas",
        editor_selector: "mceDescrEng",
        statusbar: false,
        forced_root_block: false,
        setup: function (ed) {


            ed.on('init', function () {

                this.getDoc().body.style.fontFamily = 'Calibri';
                this.getDoc().body.style.fontSize = '12px';
            });
        },
        height: 200,
        //plugins: [
        //    "advlist autolink autosave link image lists charmap print preview hr anchor pagebreak spellchecker",
        //    "searchreplace wordcount visualblocks visualchars code fullscreen insertdatetime media nonbreaking",
        //    "table  directionality emoticons template textcolor paste textcolor colorpicker textpattern "
        //],
        //toolbar1: "fontselect fontsizeselect | subscript superscript | alignleft aligncenter alignright alignjustify | bullist numlist | outdent indent | undo redo | link image code",
        //toolbar2: "bold italic underline   cut copy  | searchreplace | ltr rtl | forecolor backcolor| table",
        plugins: [
            "advlist autolink autosave link image lists charmap print preview hr anchor pagebreak spellchecker",
            "searchreplace wordcount visualblocks visualchars code fullscreen insertdatetime nonbreaking",
            "table  directionality emoticons template textcolor textcolor colorpicker textpattern "
        ],
        toolbar1: "bullist numlist | bold italic underline | ltr rtl | forecolor backcolor| link code",
        toolbar_items_size: 'small',

        menubar: false
    });

    tinymce.init({
        mode: "textareas",
        editor_selector: "mceDescrArb",
        statusbar: false,
        directionality: 'rtl',
        forced_root_block: false,
        setup: function (ed) {


            ed.on('init', function () {

                this.getDoc().body.style.fontFamily = 'Calibri';
                this.getDoc().body.style.fontSize = '12px';
            });
        },
        height: 200,
        plugins: [
            "advlist autolink autosave link image lists charmap print preview hr anchor pagebreak spellchecker",
            "searchreplace wordcount visualblocks visualchars code fullscreen insertdatetime media nonbreaking",
            "table  directionality emoticons template textcolor paste textcolor colorpicker textpattern "
        ],
        //toolbar1: "fontselect fontsizeselect | subscript superscript | alignleft aligncenter alignright alignjustify | bullist numlist | outdent indent | undo redo | link image code",
        //toolbar2: "bold italic underline   cut copy  | searchreplace | ltr rtl | forecolor backcolor| table",
        toolbar1: "bullist numlist | bold italic underline | ltr rtl | forecolor backcolor| link code",
        toolbar_items_size: 'small',

        menubar: false
    });

});