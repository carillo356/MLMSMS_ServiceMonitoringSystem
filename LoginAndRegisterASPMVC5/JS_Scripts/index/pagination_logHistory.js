// Pagination for Log History 


// define the number of items per page
var ITEMS_PAR_PAGE_LOGS = 5;

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

  // Get the modal element
    var logHistoryModal = document.getElementById("logHistory-modal");

    // Get the close button element
    var closeButton = logHistoryModal.querySelector(".close");

    // Add event listener to the close button
    closeButton.addEventListener("click", function() {
        // Hide the modal when close button is clicked
        logHistoryModal.style.display = "none";
  });


