﻿$(function () {
    $("#addAnother").click(function () {
        $.get('/Recipes/ExcludeIngredientEntryRow', function (template) {
            $("#excludeingredientEditor").append(template);

            var newNameField = ($('[data-url]', template).attr('id'));

            $("#" + newNameField).autocomplete({
                minLength: 0,
                source: function (request, response) {
                    var url = $(this.element).data('url');

                    $.getJSON(url, { term: request.term }, function (data) {
                        response(data);
                    });
                }
            });
        })

    .fail(function () {
        alert("error");
    });
    });
});

$(function () {
    $("#addAnotherWanted").click(function () {
        $.get('/Recipes/WantedIngredientEntryRow', function (template) {
            $("#wantedingredientEditor").append(template);

            var newNameField = ($('[data-url]', template).attr('id'));

            $("#" + newNameField).autocomplete({
                minLength: 0,
                source: function (request, response) {
                    var url = $(this.element).data('url');

                    $.getJSON(url, { term: request.term }, function (data) {
                        response(data);
                    });
                }
            });
        })
        .fail(function () {
            alert("error");
        });
    });
});


$(function () {
    $('#frm').submit(function (e) {
        var excludeIngred = $('#excludeingredientEditor li INPUT[type = "text"]');
        var wantedIngred = $('#wantedingredientEditor li INPUT[type = "text"]');
        if (excludeIngred.length > 0) {
            excludeIngred.each(function () {
                if ($(this).val().trim() == "") {
                    alert("Nie może istnieć wykluczony składnik bez nazwy. Dodaj nazwę lub usuń niepotrzebne pole.");
                    e.preventDefault();
                    return false;
                }
                return false;
            });

        }

        if (wantedIngred.length > 0) {
            wantedIngred.each(function () {
                if ($(this).val().trim() == "") {
                    alert("Nie może istnieć pożądany składnik bez nazwy. Dodaj nazwę lub usuń niepotrzebne pole.");
                    e.preventDefault();
                    return false;
                }
                return false;
            });

        }
    });
});

