// Pagination for Service Table


// define the number of items per page
const ITEMS_PER_PAGE = 5;

// get the table body element
const tableBody = document.querySelector('#serviceTable tbody');

// get the pagination links
const paginationLinks = document.querySelectorAll('.paginationService ul');

// define a function to show the items for the selected page
function showPage(pageNumber) {
    // calculate the start and end indexes of the items to show
    const startIndex = (pageNumber - 1) * ITEMS_PER_PAGE;
    const endIndex = startIndex + ITEMS_PER_PAGE;

    // hide all rows in the table
    const rows = tableBody.querySelectorAll('tr');
    rows.forEach(row => {
        row.style.display = 'none';
    });

    // show the rows for the current page
    for (let i = startIndex; i < endIndex && i < rows.length; i++) {
        rows[i].style.display = '';
    }
}

// handle the click event of the pagination links
paginationLinks.forEach(link => {
    link.addEventListener('click', event => {
        event.preventDefault();
        // get the selected page number from the link's text
        const pageNumber = parseInt(link.innerText);

        // show the items for the selected page
        showPage(pageNumber);

        // mark the selected page as active
        paginationLinks.forEach(link => {
            link.parentElement.classList.remove('active');
        });
        link.parentElement.classList.add('active');
    });
});

function RealTimeTable() {
    $.ajax({
        url: "/Home/RealTimeTable",
        type: "GET",
        dataType: 'json',
        success: function (result) {
            $("#serviceTable tbody").empty();
            // Loop through the data and append each row to the table
            result.forEach(function (Data) {
                var limit = 999;
                if (Data.ServiceStatus === "Running")
                    statusText = '<span style="color:green">Running</span>'
                else if (Data.ServiceStatus === "Stopped")
                    statusText = '<span style="color:red">Stopped</span>'
                else
                    statusText = "";

                var row = "<tr data-toggle='modal' data-target='#service-modal'>";
                row += "<td onclick='handleRowClick(\"" + Data.ServiceName + "\", \"" + limit + "\")'>" + Data.ServiceName + "</td>";
                row += "<td onclick='handleRowClick(\"" + Data.ServiceName + "\", \"" + limit + "\")'>" + Data.LastStart + "</td>";
                row += "<td onclick='handleRowClick(\"" + Data.ServiceName + "\", \"" + limit + "\")'>" + Data.LastEventLog + "</td>";
                row += "<td onclick='handleRowClick(\"" + Data.ServiceName + "\", \"" + limit + "\")'>" + statusText + "</span></td>";
                row += "<td onclick='handleRowClick(\"" + Data.ServiceName + "\", \"" + limit + "\")'>" + Data.HostName + "</td>";
                row += "<td>";

                if (Data.ServiceStatus === "Stopped") {
                    row += "<button class='action-button' style='margin-right: 5px;'; id='btnRun' onclick='handleServiceAction(\"" + Data.ServiceName + "\", \"start\")'>Run</button>";
                } else if (Data.ServiceStatus === "Running") {
                    row += "<button class='action-button' style='display:none;' id='btnRun' onclick='handleServiceAction(\"" + Data.ServiceName + "\", \"start\")'>Run</button>";
                    row += "<button class='action-button' id='btnStop' onclick='handleServiceAction(\"" + Data.ServiceName + "\", \"stop\")'>Stop</button>";
                    row += "<button class='action-button ms-2' id='btnRestart' onclick='handleServiceAction(\"" + Data.ServiceName + "\", \"restart\")'>Restart</button>";
                } row += "<button class='action-button' id='btnDelete' onclick='RemoveAddedService(\"" + Data.ServiceName + "\", \"delete\")'>Delete</button>";
                row += "</td></tr>";
                $("#serviceTable tbody").append(row);
            });
        },
        error: function () {
            alert("Failed to refresh services.");
        }
    });
}
