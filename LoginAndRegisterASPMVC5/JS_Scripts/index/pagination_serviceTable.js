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



// Pagination Design for Service Table


// selecting required element
const serviceElement = document.querySelector(".paginationService ul");
let serviceTotalPages = 20;
let servicePage = 1;
//calling function with passing parameters and adding inside element which is ul tag
serviceElement.innerHTML = createPaginationService(serviceTotalPages, servicePage);
function createPaginationService(serviceTotalPages, servicePage) {
    let serviceLiTag = '';
    let serviceActive;
    let serviceBeforePage = servicePage - 1;
    let serviceAfterPage = servicePage + 1;
    if (servicePage > 1) { //show the next button if the page value is greater than 1
        serviceLiTag += `<li class="btn prev" onclick="createPaginationService(serviceTotalPages, ${servicePage - 1})"><span><i class="fas fa-angle-left"></i> Prev</span></li>`;
    }
    if (servicePage > 2) { //if page value is less than 2 then add 1 after the previous button
        serviceLiTag += `<li class="first numb" onclick="createPaginationService(serviceTotalPages, 1)"><span>1</span></li>`;
        if (servicePage > 3) { //if page value is greater than 3 then add this (...) after the first li or page
            serviceLiTag += `<li class="dots"><span>...</span></li>`;
        }
    }
    // how many pages or li show before the current li
    if (servicePage == serviceTotalPages) {
        serviceBeforePage = serviceBeforePage - 2;
    } else if (servicePage == serviceTotalPages - 1) {
        serviceBeforePage = serviceBeforePage - 1;
    }
    // how many pages or li show after the current li
    if (servicePage == 1) {
        serviceAfterPage = serviceAfterPage + 2;
    } else if (servicePage == 2) {
        serviceAfterPage = serviceAfterPage + 1;
    }
    for (var plengthService = serviceBeforePage; plengthService <= serviceAfterPage; plengthService++) {
        if (plengthService > serviceTotalPages) { //if plength is greater than totalPage length then continue
            continue;
        }
        if (plengthService == 0) { //if plength is 0 than add +1 in plength value
            plengthService = plengthService + 1;
        }
        if (servicePage == plengthService) { //if page is equal to plength than assign active string in the active variable
            serviceActive = "active";
        } else { //else leave empty to the active variable
            serviceActive = "";
        }
        serviceLiTag += `<li class="numb ${serviceActive}" onclick="createPaginationService(serviceTotalPages, ${plengthService})"><span>${plengthService}</span></li>`;
    }
    if (servicePage < serviceTotalPages - 1) { //if page value is less than totalPage value by -1 then show the last li or page
        if (servicePage < serviceTotalPages - 2) { //if page value is less than totalPage value by -2 then add this (...) before the last li or page
            serviceLiTag += `<li class="dots"><span>...</span></li>`;
        }
        serviceLiTag += `<li class="last numb" onclick="createPaginationService(serviceTotalPages, ${serviceTotalPages})"><span>${serviceTotalPages}</span></li>`;
    }
    if (servicePage < serviceTotalPages) { //show the next button if the page value is less than totalPage(20)
        serviceLiTag += `<li class="btn next" onclick="createPaginationService(serviceTotalPages, ${servicePage + 1})"><span>Next <i class="fas fa-angle-right"></i></span></li>`;
    }
    serviceElement.innerHTML = serviceLiTag; //add li tag inside ul tag
    return serviceLiTag; //reurn the li tag
}