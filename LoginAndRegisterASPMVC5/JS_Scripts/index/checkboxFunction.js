

//$('.btn-primary').on('click', function () {
//    $('input[type="checkbox"]:checked').each(function () {
//        AddService($(this).val());
//    });
//    $('#displayService-modal').modal('hide');
//});

//$('.btn-primary').on('click', function () {
//    var checkedCount = $('input[type="checkbox"]:checked').length;
//    var servicesAdded = [];

//    $('input[type="checkbox"]:checked').each(function () {
//        var serviceName = $(this).val();
//        AddService(serviceName);
//        servicesAdded.push(serviceName);
//    });

//    if (servicesAdded.length > 0) {
//        var toast = new bootstrap.Toast(document.getElementById('liveToast'));
//        var toastMessage = "You have added " + checkedCount + " service(s)";
//        document.querySelector('.toast-body').innerHTML = toastMessage;
//        // set background color to green if service/s has been added.
//        toast._element.classList.remove("text-bg-danger");
//        toast._element.classList.add("bg-success");

//        toast.show();

//        setTimeout(function () {
//            toast.dispose();
//        }, 2000);
//    }

//    $('#displayService-modal').modal('hide');
//});