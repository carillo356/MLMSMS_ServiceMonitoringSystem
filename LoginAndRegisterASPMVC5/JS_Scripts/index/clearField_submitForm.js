// Submit Form for Add Service
function submitForm() {
    var serviceNames = [];

    $('input[type="checkbox"]:checked').each(function () {
        serviceNames.push($(this).val());
    });

    AddService(serviceNames);

    var checkedCount = $('input[type="checkbox"]:checked').length;
    var servicesAdded = [];

    $('input[type="checkbox"]:checked').each(function () {
        var serviceName = $(this).val();
        AddService(serviceName);
        servicesAdded.push(serviceName);
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