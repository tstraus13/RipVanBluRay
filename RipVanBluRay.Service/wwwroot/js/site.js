var timer;

$(document).ready(function () {

    CD_START();

});

function CD_START() {

    $('#cdrom').removeAttr('style');

    clearTimeout(timer);

    $('#cdrom').removeClass("rotate-middle");
    $('#cdrom').removeClass("rotate-end");

    $('#cdrom').addClass("rotate-start");

    var timer = setTimeout(CD_MIDDLE, 4000);

}

function CD_MIDDLE() {

    clearTimeout(timer);

    $('#cdrom').removeClass("rotate-start");
    $('#cdrom').removeClass("rotate-end");

    $('#cdrom').addClass("rotate-middle");

    var timer = setTimeout(CD_END, 8000);

}

function CD_END() {

    $('#cdrom').removeAttr('style');

    clearTimeout(timer);

    $('#cdrom').removeClass("rotate-middle");
    $('#cdrom').removeClass("rotate-start");

    $('#cdrom').addClass("rotate-end");

    var timer = setTimeout(CD_START, 4000);

}