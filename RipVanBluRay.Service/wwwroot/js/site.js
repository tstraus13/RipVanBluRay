let timer;

function CD_START(id) {

    $('#' + id + '-cdrom-image').removeAttr('style');

    clearTimeout(timer);

    $('#' + id + '-cdrom-image').removeClass("rotate-middle");
    $('#' + id + '-cdrom-image').removeClass("rotate-end");

    $('#' + id + '-cdrom-image').addClass("rotate-start");

    var timer = setTimeout(function () { CD_MIDDLE(id)}, 4000);

}

function CD_MIDDLE(id) {

    clearTimeout(timer);

    $('#' + id + '-cdrom-image').removeClass("rotate-start");
    $('#' + id + '-cdrom-image').removeClass("rotate-end");

    $('#' + id + '-cdrom-image').addClass("rotate-middle");

    //var timer = setTimeout(CD_END(id), 8000);

}

function CD_END(id) {

    $('#' + id + '-cdrom-image').removeAttr('style');

    //clearTimeout(timer);

    $('#' + id + '-cdrom-image').removeClass("rotate-middle");
    $('#' + id + '-cdrom-image').removeClass("rotate-start");

    $('#' + id + '-cdrom-image').addClass("rotate-end");

    //var timer = setTimeout(CD_START, 4000);

}