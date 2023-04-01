//Pagination for Users Table

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
