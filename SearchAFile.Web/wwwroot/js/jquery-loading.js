let PageIsLoaded = true;

function StartLoading(divID) {

    if (!PageIsLoaded) {

        $(divID).block({ overlayCSS: { 'cursor': 'unset', 'opacity': '0.2' }, message: $('#imgLoading'), css: { 'background-color': 'transparent', 'border': 'none', 'cursor': 'unset' } });
    }
}

function StartLoadingModal(divID) {

    $(divID).block({ overlayCSS: { 'background-color': 'gainsboro', 'border-radius': '0.2rem', 'cursor': 'unset', 'opacity': '1', 'height': '100%' }, message: $('#imgLoading'), css: { 'background-color': 'transparent', 'border': 'none', 'cursor': 'unset' } });
}

function StopLoading(divID) {

    PageIsLoaded = true;
    $(divID).unblock();
}

function StopLoadingAll() {

    PageIsLoaded = true;
    $('.blockUI').each(function () {

        $(this).parent().unblock();
    });
}