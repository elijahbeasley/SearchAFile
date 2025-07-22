document.addEventListener("DOMContentLoaded", function (event) {

    /*===== LINK ACTIVE =====*/
    const linkColor = document.querySelectorAll('.nav_link');
    //const linkColor = document.querySelectorAll('.nav_link:not([href]="")');

    function colorLink() {

        if (!$(this).hasClass('cus-collapse')
            && linkColor) {

            linkColor.forEach(l => l.classList.remove('cus-active'));
            linkColor.forEach(l => l.classList.remove('cus-active-secondary'));
            this.classList.add('cus-active');

            if ($(this).parent().hasClass('collapse')) {

                $(this).parent().prev().addClass('cus-active-secondary');
            }
        }
    }
    linkColor.forEach(l => l.addEventListener('click', colorLink));

    if (getCookie('SearchAFile_Menu_Maintain') == undefined
        || getCookie('SearchAFile_Menu_Maintain') == null) {

        setCookie('SearchAFile_Menu_MaintainCarat', '');
        setCookie('SearchAFile_Menu_Maintain', '');
    }

    if (getCookie('SearchAFile_Menu_Reports') == undefined
        || getCookie('SearchAFile_Menu_Reports') == null) {

        setCookie('SearchAFile_Menu_ReportsCarat', '');
        setCookie('SearchAFile_Menu_Reports', '');
    }
});

// Initialize bootstrap popovers.
$(function () {
    $('[data-bs-toggle="popover"]').popover()
})

// Initialize bootatrap tool tips.
$(function () {
    $('[data-bs-toggle="tooltip"]').tooltip()
})

// Get parameters from query strings.
function getParameterByName(name, url = window.location.href) {

    name = name.replace(/[\[\]]/g, '\\$&');

    let regex = new RegExp('[?&]' + name + '(=([^&#]*)|&|#|$)'), results = regex.exec(url);

    if (!results) return null;
    if (!results[2]) return '';
    return decodeURIComponent(results[2].replace(/\+/g, ' '));
}

// BEGIN ***** For the page loading icon, ***** BEGIN

let setCookie = function (name, value, expiracy) {

    let exdate = new Date();

    exdate.setTime(exdate.getTime() + expiracy * 500);

    let c_value = escape(value) + ((expiracy == null) ? "" : "; expires=" + exdate.toUTCString());

    document.cookie = name + "=" + c_value + '; path=/';
};

let getCookie = function (name) {

    let i, x, y, ARRcookies = document.cookie.split(";");

    for (i = 0; i < ARRcookies.length; i++) {

        x = ARRcookies[i].substr(0, ARRcookies[i].indexOf("="));
        y = ARRcookies[i].substr(ARRcookies[i].indexOf("=") + 1);
        x = x.replace(/^\s+|\s+$/g, "");

        if (x == name) {

            return y ? decodeURI(unescape(y.replace(/\+/g, ' '))) : y; //;//unescape(decodeURI(y));
        }
    }
};

$('.cus-show-loading').click(function () {

    $('#divLoading').show();

    setCookie('show-loading', 0, 100); //Expiration could be anything... As long as we reset the value
    setTimeout(checkDownloadCookie, 500); //Initiate the loop to check the cookie.
});

let downloadTimeout;

let checkDownloadCookie = function () {

    if (getCookie("show-loading") == 1) {

        setCookie("show-loading", "false", 100); //Expiration could be anything... As long as we reset the value

        $('#divLoading').hide();
    }
    else {

        downloadTimeout = setTimeout(checkDownloadCookie, 500); //Re-run this function in 1 second.
    }
};
// END ***** For the page loading icon, ***** END

let PageIsLoaded = true;

function StartLoading(divID) {

    if (!PageIsLoaded) {

        $('#' + divID).block({ overlayCSS: { 'cursor': 'unset', 'opacity': '0.2' }, message: $('#imgLoading'), css: { 'background-color': 'transparent', 'border': 'none', 'cursor': 'unset' } });
    }
}

function StartLoadingModal(divID) {

    $('#' + divID).block({ overlayCSS: { 'background-color': 'gainsboro', 'border-radius': '0.2rem', 'cursor': 'unset', 'opacity': '1', 'height': '100%' }, message: $('#imgLoading'), css: { 'background-color': 'transparent', 'border': 'none', 'cursor': 'unset' } });
}

function StopLoading(divID) {

    PageIsLoaded = true;
    $('#' + divID).unblock();
}

function StopLoadingAll() {

    PageIsLoaded = true;
    $('.blockUI').each(function () {

        $(this).parent().unblock();
    });
}

const PageURL = window.location.protocol + '//' + window.location.host + window.location.pathname;

$(document).ready(function () {

    if (document.location.href.includes('?')) {

        window.history.replaceState({}, document.title, PageURL);
    }

    if ($("#StartupJavaScript").text() != "") { // Yes.

        // Execute the javascript function(s).
        eval($("#StartupJavaScript").text());
    }

    SetMenuHighlight();

    $("#body").animate({ opacity: "1" }, 0);
});

function SetMenuHighlight() {

    if (document.getElementById('divMenuContent') != null) {

        let objElementList = document.getElementById('divMenuContent').querySelectorAll('a');

        for (let i = 0, l = objElementList.length; i < l; i++) {

            if (objElementList[i].classList.contains('nav_link')
                && PageURL.toLowerCase() == objElementList[i].href.toLowerCase()) {

                objElementList[i].classList.add('cus-active');

                if ($(objElementList[i]).parent().hasClass('collapse')) {

                    $(objElementList[i]).parent().prev().addClass('cus-active-secondary');
                }
            }
        }
    }
}

(function (makeToast) {
    makeToast.toast = function (objToast) {
        makeToast("#toast-container").length ||
            (makeToast("body").prepend('<div id="toast-container" aria-live="polite" aria-atomic="true" class="fixed-top d-flex justify-content-center cus-mobile cus-toast-container" style="height: 0; z-index: 1100;"></div>'),
                makeToast("#toast-container").append('<div id="toast-wrapper" class="toast-container cus-toast-wrapper mx-2" style="max-width: 1024px; min-width: 300px;"></div>'));

        let timestamp = Date.now();
        let toastDivID = "toastDiv" + timestamp;
        let toastID = "toast" + timestamp;
        let toastHeaderID = "toastHeader" + timestamp;
        let toastBodyID = "toastBody" + timestamp;
        let intervalID = "interval" + timestamp;
        let minutesID = "minutes" + timestamp;
        let subtitleID = "subtitle" + timestamp;
        let scriptID = "script" + timestamp;

        let backgroundColor = "";
        let textColor = "";
        let subtitleColor = "text-muted";
        let iconClass = "";

        // Type
        let toastType = "info";
        if (objToast.type !== "" || objToast.type != null) { toastType = objToast.type.toLowerCase(); }
        // Header
        let headerText = "Notice!";
        if (objToast.title !== "") { headerText = objToast.title; }
        // Body
        let toastBody = "";
        if (objToast.content !== "") { toastBody = objToast.content; }
        // Delay
        let toastDelay = 7000;
        if (objToast.delay !== "") { toastDelay = objToast.delay; }
        // Autohide
        let toastAutohide = true;
        if (objToast.autohide !== "") { toastAutohide = objToast.autohide; }

        switch (toastType) {
            case "info":
            case "":
                backgroundColor = "bg-info";
                iconClass = "fas fa-info-circle";
                break;
            case "success":
                backgroundColor = "bg-success";
                iconClass = "fas fa-check-circle";
                break;
            case "warning":
                backgroundColor = "bg-warning";
                iconClass = "fas fa-exclamation-circle";
                break;
            case "danger":
                backgroundColor = "bg-danger"
                iconClass = "fas fa-xmark-circle";
        }

        objToast = '' +
            '<div id="' + toastDivID + '">' +
            '   <div id="' + toastID + '" class="toast cus-toast mt-2 w-auto" role="alert" aria-live="assertive" aria-atomic="true" data-bs-delay="' + toastDelay + '" data-bs-autohide="' + toastAutohide + '" style="border: none; width: 100%; z-index: 1100;">' +
            '       <div id="' + toastHeaderID + '" class="toast-header align-items-center cus-no-select text-white ' + backgroundColor + " " + textColor + '" style="line-height: 1rem;">' +
            '           <i class="' + iconClass + ' text-white pe-2"></i>' +
            '           <strong class="me-auto">' + headerText + '</strong>';
        if (toastAutohide == false) {
            objToast += '           <small class="' + subtitleColor + ' ps-2"><span id="' + minutesID + '" hidden>0</span><span id="' + subtitleID + '" class="text-white" style="white-space: nowrap;">less than a minute ago</span></small>';
        }

        objToast += '' +
            '           <button type="button" class="btn close ms-2 me-0 my-0 p-0" data-bs-dismiss="toast" aria-label="Close">' +
            '               <i class="fa-solid fa-xmark cus-text-white" title="Close" style="font-size: large;"></i>' +
            '           </button>' +
            '       </div>' +
            '       <div id="' + toastBodyID + '" class="toast-body bg-white" style="border-radius: 0 0 0.2rem 0.2rem; line-height: 1.25rem; overflow-y: auto;">' +
            '           <label>' + toastBody + '</label>' +
            '       </div>' +
            '   </div>' +
            '   <script>' +
            '       if (window.screen.width <= 1024) {' +
            '           document.getElementById("' + toastBodyID + '").style.maxHeight = document.getElementById("header").offsetHeight - document.getElementById("' + toastHeaderID + '").offsetHeight - 5 + "px"; ' +
            '       } ' +
            '       else { ' +
            '           document.getElementById("' + toastBodyID + '").style.maxHeight = window.screen.height - document.getElementById("' + toastHeaderID + '").offsetHeight - 5 + "px"; ' +
            '       } ';

        if (toastAutohide == false) {
            objToast += '' +
                '      let ' + intervalID + ' = setInterval(function () { ' + scriptID + '(); }, 60000);' +
                '      function ' + scriptID + '() {' +
                '          if (document.getElementById("' + minutesID + '") != null) ' +
                '          { ' +
                '               let minute = document.getElementById("' + minutesID + '").innerText;' +
                '               minute = Number(minute) + 1;' +
                '               document.getElementById("' + minutesID + '").innerText = minute;' +
                '               if (minute == 1) {' +
                '                   document.getElementById("' + subtitleID + '").innerText = minute + " minute ago"' +
                '               }' +
                '               else {' +
                '                   document.getElementById("' + subtitleID + '").innerText = minute + " minutes ago"' +
                '               }' +
                '           } ' +
                '       }';
        }

        objToast += '' +
            // Method that runs when the toast is hidden.
            '       $("#' + toastID + '").on("hidden.bs.toast", function () {' +
            // Remove the entire div from the DOM.
            '           $("#' + toastDivID + '").remove();' +
            '      })' +
            '   <\/script>' +
            '</div>';

        makeToast("#toast-wrapper").prepend(objToast);
        makeToast("#toast-wrapper .toast:first").toast("show")
    }
})(jQuery);

(function (makeSnack) {
    makeSnack.snack = function (objSnack) {
        makeSnack("#toast-container").length ||
            (makeSnack("body").prepend('<div id="toast-container" aria-live="polite" aria-atomic="true" class="fixed-top d-flex justify-content-center cus-mobile cus-toast-container" style="height: 0; z-index: 1100;"></div>'),
                makeSnack("#toast-container").append('<div id="toast-wrapper" class="toast-container cus-toast-wrapper mx-2"></div>'));

        let timestamp = Date.now();
        let snackDivID = "snackDiv" + timestamp;
        let snackID = "snack" + timestamp;

        let backgroundColor = "";
        let textColor = "";
        let closeColor = "";
        let iconClass = "";

        // Type
        let snackType = "info";
        if (objSnack.type !== "" || objSnack.type != null) { snackType = objSnack.type.toLowerCase(); }
        // Header
        let headerText = "Notice!";
        if (objSnack.title !== "") { headerText = objSnack.title; }
        // Delay
        let snackDelay = 5000;
        if (objSnack.delay !== "") { snackDelay = objSnack.delay; }
        // Autohide
        let snackAutohide = true;
        if (objSnack.autohide !== "") { snackAutohide = objSnack.autohide; }

        switch (snackType) {
            case "info":
            case "":
                backgroundColor = "bg-info";
                iconClass = "fas fa-info-circle";
                break;
            case "success":
                backgroundColor = "bg-success";
                iconClass = "fas fa-check-circle";
                break;
            case "warning":
                backgroundColor = "bg-warning";
                iconClass = "fas fa-exclamation-circle";
                break;
            case "danger":
                backgroundColor = "bg-danger"
                iconClass = "fas fa-xmark-circle";
        }

        objSnack = '' +
            '<div id="' + snackDivID + '">' +
            '   <div id="' + snackID + '" class="toast cus-snack mt-2" role="alert" aria-live="assertive" aria-atomic="true" data-bs-delay="' + snackDelay + '" data-bs-autohide="' + snackAutohide + '" style="border: none; width: 100%; z-index: 1100;">' +
            '       <div class="toast-header align-items-center cus-no-select text-white ' + backgroundColor + " " + textColor + '" style="border: none; border-radius: 0.2rem; line-height: 1rem;">' +
            '           <i class="' + iconClass + ' text-white pe-2"></i>' +
            '           <strong class="me-auto">' + headerText + '</strong>' +
            '           <button type="button" class="btn close ms-2 me-0 my-0 p-0" data-bs-dismiss="toast" aria-label="Close">' +
            '               <i class="fa-solid fa-xmark cus-text-white" title="Close" style="font-size: large;"></i>' +
            '           </button>' +
            '       </div>' +
            '   </div>' +
            '   <script>' +
            // Method that runs when the snack is hidden.
            '       $("#' + snackID + '").on("hidden.bs.toast", function () {' +
            // Remove the entire div from the DOM.
            '           $("#' + snackDivID + '").remove();' +
            '      })' +
            '   <\/script>' +
            '</div>';

        makeSnack("#toast-wrapper").prepend(objSnack);
        makeSnack("#toast-wrapper .toast:first").toast("show")
    }
})(jQuery);

function ShowNotification(type, snackText, header, body, delay, autohide) {

    ShowToast(type, header, body);
    ShowSnack(type, snackText, delay, autohide);
}

function ShowToast(type, header, body, delay, autohide) {

    $.toast({
        type: type,
        title: header,
        content: body,
        delay: delay,
        autohide: autohide
    });
}

function ShowSnack(type, header, delay, autohide) {

    $.snack({
        type: type,
        title: header,
        delay: delay,
        autohide: autohide
    });
}

// Declare the timeout ID
let timeoutID;

function ShowMessage(strMessage, strColor, intDisapear, booScrollToTop) {

    // Check what color should be displayed.
    switch (strColor.toLowerCase()) {
        case 'primary':
            strColor = 'alert-primary';
            break;
        case 'secondary':
            strColor = 'alert-secondary';
            break;
        case 'success':
            strColor = 'alert-success';
            break;
        case 'danger':
            strColor = 'alert-danger';
            break;
        case 'warning':
            strColor = 'alert-warning';
            break;
        case 'info':
            strColor = 'alert-info';
            break;
        case 'green':
            strColor = 'alert-success';
            break;
        case 'red':
            strColor = 'alert-danger';
            break;
        case 'yellow':
            strColor = 'alert-warning';
            break;
        default:
            strColor = 'alert-info';
            break;
    }

    // Write the alert into the message div.
    document.getElementById("divMessage").innerHTML = '<div id="Alert" class="container mt-1 alert ' + strColor + ' alert-dismissible fade show fixed-top" role="alert">' + strMessage + '<button type="button" class="close" data-bs-dismiss="alert" aria-label="Close"><span aria-hidden="true">&times;</span></button></div>';

    // Clear out the the previous timeout.
    clearTimeout(timeoutID);

    // Is a disappearing time set?
    if (intDisapear > 0) { // Yes.
        // Set the timeout ID.
        timeoutID = setTimeout(function () { $("#Alert").alert('close'); }, intDisapear);
    }

    // Should the page scroll to the top?
    if (booScrollToTop === true) { // Yes.
        // Scroll to the top of the page.
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }
}

function ClearToast() {

    if (document.getElementById("toast-wrapper") != null) {

        let toastList = document.getElementById("toast-wrapper").querySelectorAll(".toast");

        toastList.forEach(function (toast) {

            // Remove the "fade" class from the toast.
            $('#' + toast.id).toast().removeClass('fade');

            // Remove the toast.
            $('#' + toast.id).toast('hide');
        });
    }
}

// BEGIN ***** Sort Table Data ***** BEGIN

let TableLastSortedColumn = 1;

// Sort Table
function SortTable() {

    let sortColumn = parseInt(arguments[0]);
    let type = arguments.length > 1 ? arguments[1] : 'T';
    let table = document.getElementById(arguments[2]);
    let dateformat = arguments.length > 3 ? arguments[3] : '';
    let tbody = table.getElementsByTagName("tbody")[0];
    let rows = tbody.getElementsByTagName("tr");
    let arrayOfRows = new Array();

    type = type.toUpperCase();
    dateformat = dateformat.toLowerCase();

    for (let i = 0, len = rows.length; i < len; i++) {

        arrayOfRows[i] = new Object;
        arrayOfRows[i].oldIndex = i;

        let celltext = rows[i].getElementsByTagName("td")[sortColumn].innerHTML.replace(/<[^>]*>/g, "");

        if (type == 'D') {

            arrayOfRows[i].value = GetDateSortingKey(dateformat, celltext);
        }
        else {

            let re = type == "N" ? /[^\.\-\+\d]/g : /[^a-zA-Z0-9]/g;

            arrayOfRows[i].value = celltext.replace(re, "").substr(0, 25).toLowerCase();
        }
    }

    // BEGIN ***** Added by EMB ***** BEGIN
    jQuery('#' + table.id + ' .cus-sort-icon').each(function () {

        if ($(this).css('color') == 'rgb(0, 123, 255)'
            || $(this).css('color') == 'rgb(0, 86, 179)') {

            TableLastSortedColumn = this.id.substr(5, this.id.length - 1);
        }

        if ($(this).hasClass('fa-sort-down')
            && sortColumn != TableLastSortedColumn) {

            $(this).removeClass('fa-sort-down');
            $(this).addClass('fa-sort-up');
        }

        $(this).css('color', 'gray');
    });

    let icon = $('#' + table.id + ' #iSort' + sortColumn);

    icon.css('color', 'unset');
    // END ***** Added by EMB ***** END

    if (sortColumn == TableLastSortedColumn) {

        // BEGIN ***** Added by EMB ***** BEGIN
        if (icon.hasClass('fa-sort-down')) {

            icon.removeClass('fa-sort-down');
            icon.addClass('fa-sort-up');
        }
        else {

            icon.removeClass('fa-sort-up');
            icon.addClass('fa-sort-down');
        }
        // END ***** Added by EMB ***** END

        arrayOfRows.reverse();
    }

    else {

        // BEGIN ***** Added by EMB ***** BEGIN
        if (icon.hasClass('fa-sort-down')) {

            icon.removeClass('fa-sort-down');
            icon.addClass('fa-sort-up');
        }
        // END ***** Added by EMB ***** END

        TableLastSortedColumn = sortColumn;

        switch (type) {
            case "N":
                arrayOfRows.sort(CompareRowOfNumbers);
                break;

            case "D":
                arrayOfRows.sort(CompareRowOfNumbers);
                break;
            default:
                arrayOfRows.sort(CompareRowOfText);
        }
    }
    let newTableBody = document.createElement("tbody");

    for (let i = 0, len = arrayOfRows.length; i < len; i++) {

        newTableBody.appendChild(rows[arrayOfRows[i].oldIndex].cloneNode(true));
    }

    table.replaceChild(newTableBody, tbody);
}

// Compare Row Of Text
function CompareRowOfText(a, b) {
    let aval = a.value;
    let bval = b.value;

    return (aval == bval ? 0 : (aval > bval ? 1 : -1));
}

// Compare Row Of Numbers
function CompareRowOfNumbers(a, b) {
    let aval = /\d/.test(a.value) ? parseFloat(a.value) : 0;
    let bval = /\d/.test(b.value) ? parseFloat(b.value) : 0;
    return (aval == bval ? 0 : (aval > bval ? 1 : -1));
}


// Get Date Sorting Key
function GetDateSortingKey(format, text) {
    if (format.length < 1) {

        return "";
    }

    format = format.toLowerCase();
    text = text.toLowerCase();
    text = text.replace(/^[^a-z0-9]*/, "");
    text = text.replace(/[^a-z0-9]*$/, "");

    if (text.length < 1) {

        return "";
    }

    text = text.replace(/[^a-z0-9]+/g, ",");

    let date = text.split(",");

    if (date.length < 3) {

        return "";
    }
    let d = 0, m = 0, y = 0;

    for (let i = 0; i < 3; i++) {

        let ts = format.substr(i, 1);

        if (ts == "d") {

            d = date[i];
        }
        else if (ts == "m") {

            m = date[i];
        }
        else if (ts == "y") {

            y = date[i];
        }
    }
    d = d.replace(/^0/, "");
    if (d < 10) {

        d = "0" + d;
    }
    if (/[a-z]/.test(m)) {

        m = m.substr(0, 3);

        switch (m) {
            case "jan":
                m = String(1);
                break;

            case "feb":
                m = String(2);
                break;

            case "mar":
                m = String(3);
                break;

            case "apr":
                m = String(4);
                break;

            case "may":
                m = String(5);
                break;

            case "jun":
                m = String(6);
                break;

            case "jul":
                m = String(7);
                break;

            case "aug":
                m = String(8);
                break;

            case "sep":
                m = String(9);
                break;

            case "oct":
                m = String(10);
                break;

            case "nov":
                m = String(11);
                break;

            case "dec":
                m = String(12);
                break;

            default:
                m = String(0);
        }
    }

    m = m.replace(/^0/, "");

    if (m < 10) {

        m = "0" + m;
    }
    y = parseInt(y);

    if (y < 100) {

        y = parseInt(y) + 2000;
    }

    return "" + String(y) + "" + String(m) + "" + String(d) + "";
}

// END ***** Sort Table Data ***** END

function GetRemainingTime(datRepeatTime) {

    let total = Date.parse(datRepeatTime) - Date.parse(new Date());
    let seconds = Math.floor((total / 1000) % 60);
    let minutes = Math.floor((total / 1000 / 60) % 60);
    let hours = Math.floor((total / (1000 * 60 * 60)) % 24);
    let days = Math.floor(total / (1000 * 60 * 60 * 24));

    return { total, days, hours, minutes, seconds };
}

function InitializeRepeatingFunction(strFunctionName, intDays, intHours, intMinutes, intSeconds) {

    let datRepeatTime = new Date(Date.parse(new Date()) + ((intDays * 24 * 60 * 60) + (intHours * 60 * 60) + (intMinutes * 60) + intSeconds) * 1000);

    function UpdateClock() {

        let arrRemainingTime = GetRemainingTime(datRepeatTime);

        if (arrRemainingTime.total <= 0) {

            clearInterval(objTimeInterval);

            eval(strFunctionName);

            InitializeRepeatingFunction(strFunctionName, intDays, intHours, intMinutes, intSeconds);
        }
    }

    UpdateClock();
    let objTimeInterval = setInterval(UpdateClock, 1000);
}

//function CreateHeightObserver(element, arrElements) {

//    let timestamp = Date.now();

//    alert(element);
//    alert(arrElements);

//    eval(
//        'const HeightObserver' + timestamp + ' = new ResizeObserver(entries => { ' +

//            'jQuery.each(' + arrElements + ', function () { ' +
//            '    $(this).css("height", $('+ element + ').css("height")) ' +
//            ' });' +
//        '}); ' +

//        'HeightObserver.observe(' + element + ');'
//    );
//}

// This code empowers all input tags having a placeholder and data-bs-slots attribute
document.addEventListener('DOMContentLoaded', () => {
    for (const el of document.querySelectorAll("[placeholder][data-bs-slots]")) {
        const pattern = el.getAttribute("placeholder"),
            slots = new Set(el.dataset.slots || "_"),
            prev = (j => Array.from(pattern, (c, i) => slots.has(c) ? j = i + 1 : j))(0),
            first = [...pattern].findIndex(c => slots.has(c)),
            accept = new RegExp(el.dataset.accept || "\\d", "g"),
            clean = input => {
                input = input.match(accept) || [];
                return Array.from(pattern, c =>
                    input[0] === c || slots.has(c) ? input.shift() || c : c
                );
            },
            format = () => {
                const [i, j] = [el.selectionStart, el.selectionEnd].map(i => {
                    i = clean(el.value.slice(0, i)).findIndex(c => slots.has(c));
                    return i < 0 ? prev[prev.length - 1] : back ? prev[i - 1] || first : i;
                });
                el.value = clean(el.value).join``;
                el.setSelectionRange(i, j);
                back = false;
            };
        let back = false;
        el.addEventListener("keydown", (e) => back = e.key === "Backspace");
        el.addEventListener("input", format);
        el.addEventListener("focus", format);
        el.addEventListener("blur", () => el.value === pattern && (el.value = ""));
    }
});

function OnKeyUpAfter(input, functionName, ms) {

    //setup before functions
    let typingTimer;                //timer identifier
    let doneTypingInterval = ms;  //time in ms 

    //on keyup, start the countdown
    input.on('keyup', function () {
        clearTimeout(typingTimer);
        typingTimer = setTimeout(functionName, doneTypingInterval);
    });

    //on keydown, clear the countdown 
    input.on('keydown', function () {
        clearTimeout(typingTimer);
    });
}

const entityMap = {

    '&': '&amp;',
    '<': '&lt;',
    '>': '&gt;',
    '"': '&quot;',
    "'": '&#39;',
    '/': '&#x2F;',
    '`': '&#x60;',
    '=': '&#x3D;'
};

function escapeHtml(string) {

    return String(string).replace(/[&<>"'`=\/]/g, function (s) {
        return entityMap[s];
    });
}