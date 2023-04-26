// Pagination for Admin User Table

let totalUserCount;
let indexPage = 1;
let pagination = document.querySelector('.pagination');
let totalPages;

let ITEMS_PER_PAGE = 5;

// define the number of items per page
$(document).ready(function () {
    Synch();
});

function Synch() {
    RealTimeUsersTable()
        .then(function () {
            showPage(indexPage);
        })
        .catch(function (error) {
            alert(error);
        });
}

// get the table body element
const tableBody = document.querySelector('#usersTable tbody');

// get the pagination links
const paginationLinks = document.querySelectorAll('.pagination li');

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
    generatePageNumbers();
}

function RealTimeUsersTable() {
    return new Promise(function (resolve, reject) {
        /*Refresh the table with the updated data*/
        $.ajax({
            url: "/Home/RealTimeUsersTable",
            type: "GET",
            dataType: 'json',
            success: function (result) {
                $("#usersTable tbody").empty();
                // Loop through the data and append each row to the table
                result.forEach(function (Data) {
                    const Role = (Data.IsAdmin === true) ? '<span><i class="bi bi-person-gear"></i></span>' : '<span><i class="bi bi-person"></i></span>';

                    var row = "<tr data-toggle='modal' data-target='#service-modal'>";
                    row += "<td>" + Data.FirstName + "</td>";
                    row += "<td>" + Data.LastName + "</td>";
                    row += "<td>" + Data.Email + "</td>";
                    row += "<td>" + Role + "</td>";
                    row += "<td>";
                    if (Data.Email_Notification == true) {
                        row += "<button onclick='setNotification(\"" + Data.IdUser + "\", \"" + Data.FirstName + "\", \"" + Data.LastName + "\", \"off\")' class='user-button btn-green' id='btnSet' data-bs-toggle='tooltip' data-bs-placement='top' title='Set Email On'>";
                        row += "<i class='bi bi-envelope-check-fill'></i>";
                        row += "</button>";
                    } else {
                        row += "<button onclick='setNotification(\"" + Data.IdUser + "\", \"" + Data.FirstName + "\", \"" + Data.LastName + "\", \"on\")' class='user-button btn-red' id='btnSet' data-bs-toggle='tooltip' data-bs-placement='top' title='Set Email Off'>";
                        row += "<i class='bi bi-envelope-x-fill'></i>";
                        row += "</button>";
                    }

                    row += "<button onclick='editUser(\"" + Data.IdUser + "\", \"" + Data.FirstName + "\", \"" + Data.LastName + "\", \"" + Data.Email + "\", \"" + Data.IsAdmin + "\", \"update\")' class='user-button' data-bs-toggle='modal' data-bs-target='#editUsers-modal' data-bs-toggle='tooltip' data-bs-placement='top' title='Edit User'>";
                    row += "<i class='bi bi-pencil-square'></i>";
                    row += "</button>";

                    row += "<button onclick='DeleteUser(\"" + Data.IdUser + "\", \"" + Data.FirstName + "\", \"" + Data.LastName + "\", \"delete\")' class='user-button' id='btnDelete' data-bs-toggle='tooltip' data-bs-placement='top' title='Delete User'>";
                    row += "<i class='bi bi-trash3-fill'></i>";
                    row += "</button>";

                    row += "</td></tr>";
                    $("#usersTable tbody").append(row);
                });
                totalUserCount = result.length;
                resolve();
            },
            error: function () {
                reject("Failed to refresh users.");
            }
        });
    });
}

// Set up the event listener for options
$('.options_AdminRow li').click(function () {
    var optionText = $(this).find('.option_Admin-text').text();
    switch (optionText) {
        case "All rows":
            ITEMS_PER_PAGE = totalUserCount;
            break;
        default:
            ITEMS_PER_PAGE = parseInt(optionText);
            break;
    }
    Synch(); // show the first page of the updated table
});

function setNotification(IdUser, FirstName, LastName, command) {
    $.ajax({
        type: 'POST',
        url: '/Home/UpdateEmailNotification',
        data: { IdUser: IdUser },
        success: function () {
            $(document).ready(function () {
                RealTimeUsersTable().then(function () {
                    showPage(indexPage);
                });
            });

            if (command === "on") {
                commandText = "RECEIVE";
            } else if (command === "off") {
                commandText = "NOT RECEIVE";
            }

            var toastElement = document.createElement('div');
            toastElement.setAttribute('class', 'toast hide toast-stack');
            toastElement.setAttribute('role', 'alert');
            toastElement.setAttribute('aria-live', 'assertive');
            toastElement.setAttribute('aria-atomic', 'true');

            var toastBody = document.createElement('div');
            toastBody.setAttribute('class', 'toast-body');
            toastElement.appendChild(toastBody);

            var toastWrapper = document.getElementById('toast-wrapper');
            toastWrapper.appendChild(toastElement);

            var toast = new bootstrap.Toast(toastElement);
            var toastMessage = "User " + "' " + FirstName + " " + LastName + " '" + " WILL " + commandText + " email notifications.";
            toastBody.innerHTML = toastMessage;

            if (commandText === "NOT RECEIVE") {
                // set background color to red if commandText is Stopped
                toast._element.classList.remove("bg-success");
                toast._element.classList.add("bg-danger");
            } else {
                // set background color to green for any other commandText
                toast._element.classList.remove("bg-danger");
                toast._element.classList.add("bg-success");
            }

            toast.show();

            setTimeout(function () {
                toast.dispose();
            }, 4000);
        },
        error: function (xhr, status, error) {
            alert(error);
        }
    });
}

function generatePageNumbers() {
    totalPages = Math.ceil(totalUserCount / ITEMS_PER_PAGE);
    pagination.innerHTML = '';

    let maxVisiblePages = 5;
    let startPage = indexPage - Math.floor(maxVisiblePages / 2);
    let endPage = startPage + maxVisiblePages - 1;

    if (startPage < 1) {
        startPage = 1;
        endPage = Math.min(totalPages, startPage + maxVisiblePages - 1);
    }

    if (endPage > totalPages) {
        endPage = totalPages;
        startPage = Math.max(1, endPage - maxVisiblePages + 1);
    }

    if (indexPage > 1) {
        pagination.innerHTML = `<li class="page-item" id="previous-link"><button type="button" class="page-link">Previous</button></li>`;
    }

    for (let i = startPage; i <= endPage; i++) {
        // Check if the current iteration is the active page
        const isActive = i === indexPage;

        pagination.innerHTML += `<li class="page-item${isActive ? " active" : ""}"><button type="button" class="page-link" data-page="${i}">${i}</button></li>`;
    }

    if (indexPage < totalPages) {
        pagination.innerHTML += `<li class="page-item" id="next-link"><button type="button" class="page-link">Next</button></li>`;
    }

    if (indexPage < totalPages) {
        document.getElementById("next-link").addEventListener("click", function () {
            indexPage = indexPage + 1;
            showPage(indexPage);
            generatePageNumbers();
        });
    }

    if (indexPage > 1) {
        document.getElementById("previous-link").addEventListener("click", function () {
            indexPage = indexPage - 1;
            showPage(indexPage);
            generatePageNumbers();
        });
    }

    // Add click event listener to the page number elements
    document.querySelectorAll(".pagination .page-link").forEach(function (pageLink) {
        const pageNumber = parseInt(pageLink.getAttribute("data-page"));
        if (pageNumber) {
            pageLink.addEventListener("click", function () {
                indexPage = pageNumber;
                showPage(indexPage);
                generatePageNumbers();
            });
        }
        document.getElementById('current-page').textContent = indexPage;
        document.getElementById('total-pages').textContent = totalPages;
    })
}