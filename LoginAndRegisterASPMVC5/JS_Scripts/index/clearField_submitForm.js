// Submit Form for Add Service
function submitForm() {
    var serviceName = $('input[name="ServiceName"]').val();
    AddService(serviceName);
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