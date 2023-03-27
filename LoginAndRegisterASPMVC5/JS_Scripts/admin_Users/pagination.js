//Pagination for Users Table

// define the number of items per page
 
let ITEMS_PER_PAGE = 5;


    // Set up the event listener for options
    $('.options_AdminRow li').click(function () {
        var optionText = $(this).find('.option_Admin-text').text();
        switch (optionText) {
            case "All rows":
                ITEMS_PER_PAGE = -1;
                break;
            default:
                ITEMS_PER_PAGE = parseInt(optionText);
                break;
        }
        showPage(1); // show the first page of the updated table
    });


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
}


//Event Listener for Selecting no. of Rows



// /*handle the click event of the pagination links*/
//paginationLinks.forEach(link => {
//    link.addEventListener('click', event => {
//        event.preventDefault();
//        // get the selected page number from the link's text
//        pageNumber = parseInt(link.innerText);

//        // show the items for the selected page
//        showPage(pageNumber);

//        // mark the selected page as active
//        paginationLinks.forEach(link => {
//            link.parentElement.classList.remove('active');
//        });
//        link.parentElement.classList.add('active');
//    });


//// show the first page by default
//$(document).ready(function () {
//    //RealTimeUsersTable().then(function () {
//    //    showPage(1);
//    /*    });*/
//    Synch();
//});


$(document).ready(function () {
    Synch();
});


function Synch() {
    RealTimeUsersTable()
        .then(function () {
            showPage(1); // call showPage after reloading the table
        })
        .catch(function (error) {
            alert(error);
        });
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
                    const Role = (Data.IsAdmin === true) ? '<span>Admin</span>' : '<span>Non-Admin</span>';

                    var row = "<tr data-toggle='modal' data-target='#service-modal'>";
                    row += "<td>" + Data.FirstName + "</td>";
                    row += "<td>" + Data.LastName + "</td>";
                    row += "<td>" + Data.Email + "</td>";
                    row += "<td>" + Role + "</td>";
                    row += "<td>";
                    if (Data.Email_Notification == true) {
                        row += "<button onclick='setNotification(\"" + Data.idUser + "\", \"" + Data.FirstName + "\", \"" + Data.LastName + "\", \"off\")' class='user-button btn-green' id='btnSet' data-bs-toggle='tooltip' data-bs-placement='top' title='Set Email On'>";
                        row += "<i class='bi bi-envelope-check-fill'></i>";
                        row += "</button>";
                    } else {
                        row += "<button onclick='setNotification(\"" + Data.idUser + "\", \"" + Data.FirstName + "\", \"" + Data.LastName + "\", \"on\")' class='user-button btn-red' id='btnSet' data-bs-toggle='tooltip' data-bs-placement='top' title='Set Email Off'>";
                        row += "<i class='bi bi-envelope-x-fill'></i>";
                        row += "</button>";
                    }

                    row += "<button onclick='editUser(\"" + Data.idUser + "\", \"" + Data.FirstName + "\", \"" + Data.LastName + "\", \"" + Data.Email + "\", \"" + Data.IsAdmin + "\", \"update\")' class='user-button' data-bs-toggle='modal' data-bs-target='#editUsers-modal'>";
                    row += "<i class='bi bi-pencil-square'></i>";
                    row += "</button>";

                    row += "<button onclick='DeleteUser(\"" + Data.idUser + "\", \"" + Data.FirstName + "\", \"" + Data.LastName + "\", \"delete\")' class='user-button' id='btnSet' data-bs-toggle='tooltip' data-bs-placement='top' title='Delete'>";
                    row += "<i class='bi bi-trash3-fill'></i>";
                    row += "</button>";

                    row += "</td></tr>";
                    $("#usersTable tbody").append(row);
                });
                resolve();
            },
            error: function () {
                reject("Failed to refresh users.");
            }
        });
    });
}

function setNotification(idUser, FirstName, LastName, command) {
    $.ajax({
        type: 'POST',
        url: '/Home/UpdateEmailNotification',
        data: { idUser: idUser },
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

            var toast = new bootstrap.Toast(document.getElementById('liveToast'));
            var toastMessage = "User " + "' " + FirstName + " " + LastName + " '" + " WILL " + commandText + " email notifications.";
            document.querySelector('.toast-body').innerHTML = toastMessage;
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
      

//let currentPage = 1;
//const itemsPerPage = 5;
//const totalPages = Math.ceil(totalItems / itemsPerPage);

//function renderPagination() {
//    let paginationHTML = '';
//    if (currentPage === 1) {
//        paginationHTML += '<li class="page-item disabled"><a class="page-link" href="#">Prev</a></li>';
//    } else {
//        paginationHTML += '<li class="page-item"><a class="page-link" href="#" onclick="prevPage()">Prev</a></li>';
//    }

//    for (let i = 1; i <= totalPages; i++) {
//        if (i === currentPage) {
//            paginationHTML += '<li class="page-item active"><a class="page-link" href="#" onclick="goToPage(' + i + ')">' + i + '</a></li>';
//        } else {
//            paginationHTML += '<li class="page-item"><a class="page-link" href="#" onclick="goToPage(' + i + ')">' + i + '</a></li>';
//        }
//    }

//    if (currentPage === totalPages) {
//        paginationHTML += '<li class="page-item disabled"><a class="page-link" href="#">Next</a></li>';
//    } else {
//        paginationHTML += '<li class="page-item"><a class="page-link" href="#" onclick="nextPage()">Next</a></li>';
//    }

//    document.getElementById('pagination').innerHTML = paginationHTML;
//}

//function goToPage(page) {
//    currentPage = page;
//    renderPagination();
//    showPage(page);
//}

//function nextPage() {
//    if (currentPage < totalPages) {
//        currentPage++;
//        renderPagination();
//        showPage(currentPage);
//    }
//}

//function prevPage() {
//    if (currentPage > 1) {
//        currentPage--;
//        renderPagination();
//        showPage(currentPage);
//    }
//}

//renderPagination();
//showPage(currentPage);

//const pagination = document.querySelector('.pagination');
//const links = pagination.querySelectorAll('.page-link');

//function updatePagination(start) {
//    for (let i = 0; i < links.length; i++) {
//        if (i === 0 || i === links.length - 1 || (i >= start && i < start + 5)) {
//            links[i].style.display = 'inline-block';
//        } else {
//            links[i].style.display = 'none';
//        }
//    }
//}

//updatePagination(1);

//pagination.addEventListener('click', event => {
//    event.preventDefault();
//    if (event.target.classList.contains('page-link')) {
//        const current = pagination.querySelector('.active');
//        if (event.target.innerText === 'Next') {
//            if (current.nextElementSibling) {
//                current.nextElementSibling.classList.add('active');
//                current.classList.remove('active');
//                updatePagination(parseInt(current.nextElementSibling.innerText) - 1);
//            }
//        } else if (event.target.innerText === 'Previous') {
//            if (current.previousElementSibling) {
//                current.previousElementSibling.classList.add('active');
//                current.classList.remove('active');
//                updatePagination(parseInt(current.previousElementSibling.innerText) - 1);
//            }
//        } else {
//            current.classList.remove('active');
//            event.target.parentElement.classList.add('active');
//            updatePagination(parseInt(event.target.innerText) - 1);
//        }
//    }
//});


//const paginationLinks = document.querySelectorAll('.pagination .page-item a');
//const previousLink = document.querySelector('#previous-link');
//const nextLink = document.querySelector('#next-link');
//let currentPage = 1;

//// initialize pagination links
//paginationLinks.forEach((link, index) => {
//    if (index > 0 && index < 6) {
//        link.textContent = index + 1;
//    } else {
//        link.parentElement.style.display = 'none';
//    }
//});

//previousLink.addEventListener('click', () => {
//    if (currentPage > 1) {
//        currentPage--;
//        updatePaginationLinks();
//    }
//});

//nextLink.addEventListener('click', () => {
//    currentPage++;
//    updatePaginationLinks();
//});

//function updatePaginationLinks() {
//    // update page numbers
//    paginationLinks.forEach((link, index) => {
//        if (index > 0 && index < 6) {
//            link.textContent = currentPage + (index - 1);
//        }
//    });

//    // show/hide links based on current page
//    if (currentPage === 1) {
//        previousLink.style.display = 'none';
//        nextLink.style.display = '';
//    } else if (currentPage === 6) {
//        previousLink.style.display = '';
//        nextLink.style.display = 'none';
//    } else {
//        previousLink.style.display = '';
//        nextLink.style.display = '';
//    }

//    // mark current page as active
//    paginationLinks.forEach(link => {
//        if (link.textContent === currentPage.toString()) {
//            link.parentElement.classList.add('active');
//        } else {
//            link.parentElement.classList.remove('active');
//        }
//    });
//}

//// initialize pagination on page load
//updatePaginationLinks();


////Pagination Design for Admin Users Table

//// selecting required element
//const element = document.querySelector(".pagination ul");
//let totalPages = 20;
//let page = 1;
////calling function with passing parameters and adding inside element which is ul tag
//element.innerHTML = createPagination(totalPages, page);
//function createPagination(totalPages, page) {
//    let liTag = '';
//    let active;
//    let beforePage = page - 1;
//    let afterPage = page + 1;
//    if (page > 1) { //show the next button if the page value is greater than 1
//        liTag += `<li class="btn prev" onclick="createPagination(totalPages, ${page - 1})"><span><i class="fas fa-angle-left"></i> Prev</span></li>`;
//    }
//    if (page > 2) { //if page value is less than 2 then add 1 after the previous button
//        liTag += `<li class="first numb" onclick="createPagination(totalPages, 1)"><span>1</span></li>`;
//        if (page > 3) { //if page value is greater than 3 then add this (...) after the first li or page
//            liTag += `<li class="dots"><span>...</span></li>`;
//        }
//    }
//    // how many pages or li show before the current li
//    if (page == totalPages) {
//        beforePage = beforePage - 2;
//    } else if (page == totalPages - 1) {
//        beforePage = beforePage - 1;
//    }
//    // how many pages or li show after the current li
//    if (page == 1) {
//        afterPage = afterPage + 2;
//    } else if (page == 2) {
//        afterPage = afterPage + 1;
//    }
//    for (var plength = beforePage; plength <= afterPage; plength++) {
//        if (plength > totalPages) { //if plength is greater than totalPage length then continue
//            continue;
//        }
//        if (plength == 0) { //if plength is 0 than add +1 in plength value
//            plength = plength + 1;
//        }
//        if (page == plength) { //if page is equal to plength than assign active string in the active variable
//            active = "active";
//        } else { //else leave empty to the active variable
//            active = "";
//        }
//        liTag += `<li class="numb ${active}" onclick="createPagination(totalPages, ${plength})"><span>${plength}</span></li>`;
//    }
//    if (page < totalPages - 1) { //if page value is less than totalPage value by -1 then show the last li or page
//        if (page < totalPages - 2) { //if page value is less than totalPage value by -2 then add this (...) before the last li or page
//            liTag += `<li class="dots"><span>...</span></li>`;
//        }
//        liTag += `<li class="last numb" onclick="createPagination(totalPages, ${totalPages})"><span>${totalPages}</span></li>`;
//    }
//    if (page < totalPages) { //show the next button if the page value is less than totalPage(20)
//        liTag += `<li class="btn next" onclick="createPagination(totalPages, ${page + 1})"><span>Next <i class="fas fa-angle-right"></i></span></li>`;
//    }
//    element.innerHTML = liTag; //add li tag inside ul tag
//    return liTag; //reurn the li tag
//}