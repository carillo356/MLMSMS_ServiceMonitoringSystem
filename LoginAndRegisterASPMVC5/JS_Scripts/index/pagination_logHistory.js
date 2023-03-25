// Pagination for Log History 


// define the number of items per page
var ITEMS_PAR_PAGE_LOGS;

// get the table body element
const logstableBody = document.querySelector('#popupTable tbody');

// get the pagination links
const logspaginationLinks = document.querySelectorAll('.paginationLogs ul');

// define a function to show the items for the selected page
function logsshowPage(logs_pageNumber) {
    // calculate the start and end indexes of the items to show
    const logsstartIndex = (logs_pageNumber - 1) * ITEMS_PAR_PAGE_LOGS;
    const logsendIndex = logsstartIndex + ITEMS_PAR_PAGE_LOGS;

    // hide all rows in the table
    const logs_rows = logstableBody.querySelectorAll('tr');
    logs_rows.forEach(logs_row => {
        logs_row.style.display = 'none';
    });

    // show the rows for the current page
    for (let i = logsstartIndex; i < logsendIndex && i < logs_rows.length; i++) {
        logs_rows[i].style.display = '';
    }
}

var logs_pageNumber = 1;
// handle the click event of the pagination links
logspaginationLinks.forEach(link => {
    link.addEventListener('click', event => {
        event.preventDefault();
        // get the selected page number from the link's text
        logs_pageNumber = parseInt(link.innerText);

        // show the items for the selected page
        logsshowPage(logs_pageNumber);

        // mark the selected page as active
        logspaginationLinks.forEach(link => {
            link.parentElement.classList.remove('active');
        });
        link.parentElement.classList.add('active');
    });
});

// Log History Modal
function logHistory(serviceName/*, limit*/) {
    //Viewlogs, Gets all the records of a service
    $.ajax({
        url: "/Home/GetServiceLogsTB",
        type: "POST",
        data: { serviceName: serviceName },
        success: function (data) {
            // Get the table body element
            var tbody = $('#popupTable tbody');
            tbody.empty();

            // Create a string containing all the rows
            var rows = '';
            data.forEach(function (service) {
                // Add a CSS class to the td element based on the value of ServiceStatus
                var statusColorClass = service.ServiceStatus === 'Running' ? 'text-success' : 'text-danger';

                rows += '<tr>' +
                    '<td>' + service.LastStart + '</td>' +
                    '<td>' + service.LastEventLog + '</td>' +
                    '<td class="status">' +
                    '<span class="' + statusColorClass + '">' + service.ServiceStatus + '</span>' +
                    '</td>' +
                    '<td>' + service.HostName + '</td>' +
                    '</tr>';
            });

            // Append the rows to the table in a single operation
            tbody.append(rows);
            activeServiceName = serviceName;

            $('#logHistory-modal .modal-servicelogs .service_name').text(serviceName);

            //// Show the popup dialog
            //ITEMS_PAR_PAGE_LOGS = limit;
            //logsshowPage(1);
            $('#logHistory-modal').css('display', 'block');

        },
        error: function () {
            alert("Failed to retrieve records for " + serviceName);
        }
    });
}



// Pagination Design for Log History Table


// selecting required element
const logsElement = document.querySelector(".paginationLogs ul");
let logsTotalPages = 20;
let logsPage = 1;
//calling function with passing parameters and adding inside element which is ul tag
logsElement.innerHTML = createPaginationLogs(logsTotalPages, logsPage);
function createPaginationLogs(logsTotalPages, logsPage) {
    let logsLiTag = '';
    let logsActive;
    let logsBeforePage = logsPage - 1;
    let logsAfterPage = logsPage + 1;
    if (logsPage > 1) { //show the next button if the page value is greater than 1
        logsLiTag += `<li class="btn prev" onclick="createPaginationLogs(logsTotalPages, ${logsPage - 1})"><span><i class="fas fa-angle-left"></i> Prev</span></li>`;
    }
    if (logsPage > 2) { //if page value is less than 2 then add 1 after the previous button
        logsLiTag += `<li class="first numb" onclick="createPaginationLogs(logsTotalPages, 1)"><span>1</span></li>`;
        if (logsPage > 3) { //if page value is greater than 3 then add this (...) after the first li or page
            logsLiTag += `<li class="dots"><span>...</span></li>`;
        }
    }
    // how many pages or li show before the current li
    if (logsPage == logsTotalPages) {
        logsBeforePage = logsBeforePage - 2;
    } else if (logsPage == logsTotalPages - 1) {
        logsBeforePage = logsBeforePage - 1;
    }
    // how many pages or li show after the current li
    if (logsPage == 1) {
        logsAfterPage = logsAfterPage + 2;
    } else if (logsPage == 2) {
        logsAfterPage = logsAfterPage + 1;
    }
    for (var plengthLogs = logsBeforePage; plengthLogs <= logsAfterPage; plengthLogs++) {
        if (plengthLogs > logsTotalPages) { //if plength is greater than totalPage length then continue
            continue;
        }
        if (plengthLogs == 0) { //if plength is 0 than add +1 in plength value
            plengthLogs = plengthLogs + 1;
        }
        if (logsPage == plengthLogs) { //if page is equal to plength than assign active string in the active variable
            logsActive = "active";
        } else { //else leave empty to the active variable
            logsActive = "";
        }
        logsLiTag += `<li class="numb ${logsActive}" onclick="createPaginationLogs(logsTotalPages, ${plengthLogs})"><span>${plengthLogs}</span></li>`;
    }
    if (logsPage < logsTotalPages - 1) { //if page value is less than totalPage value by -1 then show the last li or page
        if (logsPage < logsTotalPages - 2) { //if page value is less than totalPage value by -2 then add this (...) before the last li or page
            logsLiTag += `<li class="dots"><span>...</span></li>`;
        }
        logsLiTag += `<li class="last numb" onclick="createPaginationLogs(logsTotalPages, ${logsTotalPages})"><span>${logsTotalPages}</span></li>`;
    }
    if (logsPage < logsTotalPages) { //show the next button if the page value is less than totalPage(20)
        logsLiTag += `<li class="btn next" onclick="createPaginationLogs(logsTotalPages, ${logsPage + 1})"><span>Next <i class="fas fa-angle-right"></i></span></li>`;
    }
    logsElement.innerHTML = logsLiTag; //add li tag inside ul tag
    return liTag; //reurn the li tag
}