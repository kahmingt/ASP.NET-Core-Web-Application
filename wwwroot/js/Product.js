// Global variable declaration

$(document).ready(function () {
    console.log("Welcome");
    alert(WhichCrudAction);
});


/**
 * Load modal '#ModalDetails' in 'View' mode when button '#ButtonView' was clicked.
 */
$(document).on("click", '#ButtonView', function (event) {
    let ProductID = $(this).closest("tr").find('#ProductID').val();
    console.log("onClick ButtonView - ProductID: [" + ProductID + "]");

    if ($.isNumeric(ProductID)) {
        whichCRUDAction = "R";
        LoadBootstrapModal(ProductID, "View");
    }
    else {
        alert("Invalid/unknown Product id!");
        console.error("Invalid/unknown Product id!");
        event.preventDefault();
    }
});

/**
 * Load modal '#ModalDetails' in 'Edit' mode when button '#ButtonCreate' was clicked.
 */
$(document).on("click", '#ModalDetails_ButtonCreate', function (event) {
    LoadBootstrapModal(null, "Edit");
});

/**
 * Load modal '#ModalDetails' in 'Edit' mode when button '#ButtonEdit' was clicked.
 */
$(document).on("click", '#ModalDetails_ButtonEdit', function (event) {
    let ProductID = $('#ModalDetails').find('#ProductID').val();
    console.log("onClick ButtonEdit - ProductID: [" + ProductID + "]");

    // Load Bootstrap modal
    if ($.isNumeric(ProductID)) {
        LoadBootstrapModal(ProductID, "Edit");
    }
    else {
        alert("Invalid/unknown Product id!");
        console.error("Invalid/unknown Product id!");
        event.preventDefault();
    }
});

/**
 * Load modal '#ModalDetails' in 'View' mode when button '#ButtonCancel' was clicked.
 */
$(document).on("click", '#ModalDetails_ButtonCancel', function (event) {
    let ProductID = $('#ModalDetails').find('#ProductID').val();
    console.log("onClick ButtonCancel - ProductID: [" + ProductID + "]");

    // Load Bootstrap modal
    if ($.isNumeric(ProductID) && ProductID > 0) {
        LoadBootstrapModal(ProductID, "View");
    }
    else {
        $('#ModalDetails').modal('toggle'); 
    }
});

/**
 * Load modal '#ModalDelete' when button '#ButtonDelete' was clicked.
 */
$(document).on("click", '#ModalDetails_ButtonDelete', function (event) {
    // Set changes
    $('#ModalDetails').css("opacity", "0.75");
    $('#ModalDelete').css("top", "5px");
    $('#ModalDelete').modal('show');
});

/**
 * Load modal '#ModalDetails' in 'View' mode when button '#ButtonCancel' was clicked.
 */
$(document).on("click", '#ModalDelete_ButtonCancel', function (event) {
    // Reset changes
    $('#ModalDetails').css("opacity", "1");
});


function LoadBootstrapModal(Id, Mode) {
    let Object = $('#ModalDetails');

    // Load Bootstrap modal
    $.when(
        $.ajax({
            type: "GET",
            url: "/Product/PartialViewDetails",
            contentType: "application/x-www-form-urlencoded; charset=UTF-8",
            data: { id: Id, mode: Mode },
            dataType: "html",
            cache: false,
            success: function (response) {
                console.log("[HTTPGet] PartialViewDetails success!");

                // Add response to modal body
                Object.find('.modal-body').html(response);
                Object.modal('show');
            },
            error: function (xhr, status, thrownError) {
                alert("Status code : " + xhr.status);
                alert(thrownError);
            }
        })
    )
    .done(function () {

        ToggleData(Mode);

        if ($.isNumeric(Id) && Id > 0) {
            $('#ModalDetails_ButtonSubmit').prop('value', 'ButtonUpdate');
        }
        else {
            $('#ModalDetails_ButtonSubmit').prop('value', 'ButtonCreate');
        }
        $('#ModalDetails_ButtonSubmit').attr('formaction', "/Product/Index?id=" + Id + "&mode=Edit");
    });
}

function ToggleData(mode) {
    if (mode == "View") {
        console.log("View mode");
        // buttons
        $('#ModalDetails').find('button[id=ModalDetails_ButtonEdit]').show();
        $('#ModalDetails').find('button[id=ModalDetails_ButtonDelete]').show();
        $('#ModalDetails').find('button[id=ModalDetails_ButtonSubmit]').hide();
        $('#ModalDetails').find('button[id=ModalDetails_ButtonCancel]').hide();

    }
    if (mode == "Edit") {
        console.log("Edit mode");
        //buttons
        $('#ModalDetails').find('button[id=ModalDetails_ButtonEdit]').hide();
        $('#ModalDetails').find('button[id=ModalDetails_ButtonDelete]').hide();
        $('#ModalDetails').find('button[id=ModalDetails_ButtonSubmit]').show();
        $('#ModalDetails').find('button[id=ModalDetails_ButtonCancel]').show();
    }
}


$(document).on("click", '#ModalDetails_ButtonSubmit', function (event) {
    $('#ModalDetails').find('button[id=ModalDetails_ButtonEdit]').hide();


});

$(document).on("click", '#ModalDelete_ButtonDelete', function (event) {
    //var buttonDeleteValue = $('#ModalDelete').find('button[id=ModalDelete_ButtonDelete]').val();
    var buttonDeleteValue = this.val();
    console.log("buttonDeleteValue [" + buttonDeleteValue + "]");
    whichCRUDAction = "D";
});

//https://stackoverflow.com/questions/20073568/fire-javascript-function-after-partial-view-is-rendered-in-c-sharp-mvc