// Submit Form for Add Service
function submitForm() {
    var serviceNames = [];

    $('input[type="checkbox"]:checked').each(function () {
        serviceNames.push($(this).val());
    });

    // Wrap each AddService call in a promise and store them in an array
    var promises = serviceNames.map(function (serviceName) {
        return new Promise(function (resolve, reject) {
            AddService(serviceName, resolve, reject);
        });
    });
    // Wait for all promises to complete
    Promise.all(promises).then(function () {
        SynchServiceTB();
    });


    if (servicesAdded.length > 0) {
        var toast = new bootstrap.Toast(document.getElementById('liveToast'));
        var toastMessage = "You have added " + checkedCount + " service(s)";
        document.querySelector('.toast-body').innerHTML = toastMessage;
        // set background color to green if service/s has been added.
        toast._element.classList.remove("text-bg-danger");
        toast._element.classList.add("bg-success");

        toast.show();

        setTimeout(function () {
            toast.dispose();
        }, 2000);
    }
    $('#displayService-modal').modal('hide');
}

// Clear Field in Search Filter
$(function () {
    // Function to clear all input fields
    function clearFields() {
        $('#displayService-modal input[type="text"]').val('');
    }

    // Attach the clearFields function to the Cancel button click event
    $('#btnCancelService, #btnAddService').click(function () {
        clearFields();
    });
});

// Selecting Rows Dropdown for Service Table
const optionMenu2 = document.querySelector(".serviceRow"),
    select_btn2 = optionMenu2.querySelector(".btnRow"),
    options2 = optionMenu2.querySelectorAll(".optionRow"),
    sBtn_text2 = optionMenu2.querySelector(".sBtnRow");

select_btn2.addEventListener("click", () => optionMenu2.classList.toggle("active"));

options2.forEach(option => {
    option.addEventListener("click", () => {
        let selectedOption = option.querySelector(".optionRow-text").innerText;
        sBtn_text2.innerText = selectedOption;

        optionMenu2.classList.remove("active");
    });
});

// Selecting Rows Dropdown for Log History Table
const optionMenuLogHistory = document.querySelector(".logHistoryRow"),
    selectBtnLogHistory = optionMenuLogHistory.querySelector(".btnLogHistoryRow"),
    optionsLogHistory = optionMenuLogHistory.querySelectorAll(".option_LogHistory"),
    sBtnTextLogHistory = optionMenuLogHistory.querySelector(".sBtnLogHistoryRow");

selectBtnLogHistory.addEventListener("click", () => optionMenuLogHistory.classList.toggle("active"));

optionsLogHistory.forEach(option => {
    option.addEventListener("click", () => {
        let selectedOption = option.querySelector(".option_LogHistory-text").innerText;
        sBtnTextLogHistory.innerText = selectedOption;

        optionMenuLogHistory.classList.remove("active");
    });
});