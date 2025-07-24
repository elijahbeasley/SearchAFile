(function (makeToast) {
    makeToast.toast = function (objToast) {
        makeToast("#toast-container").length ||
            (makeToast("body").prepend('<div id="toast-container" aria-live="polite" aria-atomic="true" class="fixed-top d-flex justify-content-center cus-mobile cus-toast-container mx-2" style="height: 0; z-index: 1100;"></div>'),
                makeToast("#toast-container").append('<div id="toast-wrapper" class="toast-container cus-toast-wrapper" style="min-width: 300px;"></div>'));

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
            (makeSnack("body").prepend('<div id="toast-container" aria-live="polite" aria-atomic="true" class="fixed-top d-flex justify-content-center cus-mobile cus-toast-container mx-2" style="height: 0; z-index: 1100;"></div>'),
                makeSnack("#toast-container").append('<div id="toast-wrapper" class="toast-container cus-toast-wrapper"></div>'));

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