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