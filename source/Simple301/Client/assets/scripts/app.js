app.requires.push('ngTable');

/*
* SIMPLE301 CONTROLLER
* -----------------------------------------------------
* Main Simple 301 controller used to render out the Simple 301 content section
*/
angular.module("umbraco").controller("Simple301Controller", function ($scope, $filter, Simple301Api, ngTableParams) {

    //Property to display error messages
    $scope.errorMessage = '';
    //App state
    $scope.initialLoad = false;
    $scope.cacheCleared = false;

    /*
    * Refresh the table. Uses $scope.redirects for data
    */
    $scope.refreshTable = function () {
        //If we aren't set up yet, return
        if (!$scope.tableParams) return;

        $scope.tableParams.total($scope.redirects.length);
        $scope.tableParams.reload();
    }

    /*
    * Handles clearing the cache by
    * calling to get all redirects again
    */
    $scope.clearCache = function () {
        $scope.fetchRedirects();
        $scope.cacheCleared = true;
    }

    /*
    * Handles fetching all redirects from the server.
    */
    $scope.fetchRedirects = function () {
        return Simple301Api.getAll().then($scope.onRecieveAllRedirectsResponse.bind(this));
    };

    /*
    * Response handler for requesting all redirects
    */
    $scope.onRecieveAllRedirectsResponse = function (response) {
        //Somethign went wrong. Error out
        if (!response || !response.data) {
            $scope.errorMessage = "Error fetching redirects from server";
            return;
        }

        //We received data. Continue
        $scope.redirects = response.data;
        $scope.refreshTable();
    }

    /*
    * Handles adding a new redirect to the redirect collection.
    * Sends request off to API.
    */
    $scope.addRedirect = function (redirect) {
        Simple301Api.add(redirect.IsRegex, redirect.OldUrl, redirect.NewUrl, redirect.Notes)
            .then($scope.onAddRedirectResponse.bind(this));
    };

    /*
    * Handles the Add Redirect response from the API. Checks
    * for errors and updates table.
    */
    $scope.onAddRedirectResponse = function (response) {
        //Check for error
        if (!response || !response.data) {
            $scope.errorMessage = "Error sending request to add a new redirect.";
            return;
        }

        //Handle success from API
        if (response.data.Success) {
            $scope.errorMessage = '';
            $scope.redirects.push(response.data.NewRedirect);
            $scope.refreshTable();
        }
        else {
            $scope.errorMessage = response.data.Message;
        }
    }

    /*
    * Handles sending a redirect to the API to as a reference for
    * updating the redirects collection server side.
    */
    $scope.updateRedirect = function (redirect) {
        Simple301Api.update(redirect).then($scope.onUpdateRedirectResponse.bind(this, redirect));
    }

    /*
    * Handler for receiving a response from the Update Redirect API call
    * Will update the table with the returned, updated redirect
    */
    $scope.onUpdateRedirectResponse = function (redirect, response) {
        //Check for error
        if (!response || !response.data) {
            $scope.errorMessage = "Error sending request to update a redirect.";
            return;
        }

        if (response.data.Success) {
            $scope.errorMessage = '';
            redirect.LastUpdated = response.data.UpdatedRedirect.LastUpdated;
            redirect.$edit = false;
        }
        else {
            $scope.errorMessage = response.data.Message;
        }
    }

    /*
    * Handles the delete request to delete a redirect.
    * Calls the Delete API method passing in the redirect ID
    */
    $scope.deleteRedirect = function (redirect) {
        if (confirm("Are you sure you want to delete this redirect?")) {
            Simple301Api.remove(redirect.Id).then($scope.onDeleteRedirectResponse.bind(this, redirect));
        }
    }

    /*
    * Handles the DeleteRedirect response from the API. If successful,
    * remove the redirect from the table.
    */
    $scope.onDeleteRedirectResponse = function (redirect, response) {
        //Check for error
        if (!response || !response.data) {
            $scope.errorMessage = "Error sending request to delete a redirect.";
            return;
        }

        //Remove the item from the table. Splice redirect array
        if (response.data.Success) {
            $scope.errorMessage = '';
            var index = $scope.redirects.indexOf(redirect);
            if (index > -1) {
                $scope.redirects.splice(index, 1);
                $scope.tableParams.total($scope.redirects.length);
                $scope.tableParams.reload();
            }

        }
        else {
            $scope.errorMessage = response.data.errorMessage;
        }
    }

    /*
    * Clears the global error message
    */
    $scope.clearErrorMessage = function () {
        $scope.errorMessage = '';
    }

    /*
    * Defines a new ngTable. 
    */
    $scope.tableParams = new ngTableParams({
        page: 1,            // show first page
        count: 10,          // count per page
        sorting: {
            LastUpdated: 'desc'     // initial sorting
        },
        filter: {
            Message: ''       // initial filter
        },
        data: $scope.initialData
    }, {
        total: 0,
        getData: function ($defer, params) {

            //Do we have redirects yet?
            var data = $scope.redirects || [];

            //Do we have a search term set in the search box?
            //If so, filter the redirects for that text
            var searchTerm = params.filter().Search;
            var searchedData = searchTerm ?
                data.filter(function (redirect) {
                    return redirect.Notes.toLowerCase().indexOf(searchTerm.toLowerCase()) > -1 ||
                        redirect.OldUrl.toLowerCase().indexOf(searchTerm.toLowerCase()) > -1 ||
                        redirect.NewUrl.toLowerCase().indexOf(searchTerm.toLowerCase()) > -1
                }) : data;

            //Are we ordering the results?
            var orderedData = params.sorting() ?
                    $filter('orderBy')(searchedData, params.orderBy()) :
                    searchedData;

            //Set totals and page counts
            params.total(orderedData.length);
            var pagedResults = orderedData.slice((params.page() - 1) * params.count(), params.page() * params.count());

            //Cheat and add a blank redirect so the user can add a new redirect right from the table
            pagedResults.push({ Id: "-1", IsRegex: false, OldUrl: "", NewUrl: "", Notes: "", LastUpdated: "", $edit: true });
            $defer.resolve(pagedResults);
        }
    })

    /*
    * Initial load function to set loaded state
    */
    $scope.initLoad = function () {
        if (!$scope.initialLoad) {
            //Get the available log dates to view log entries for.
            $scope.fetchRedirects()
                .then(function () { $scope.initialLoad = true; });
        }
    }

    $(function () {
        $scope.$tab = $('a:contains("Manage Redirects")');

        //If we have a tab, set the click handler so we only
        //load the content on tab click. 
        if ($scope.$tab && $scope.$tab.length > 0) {
            $scope.$tab.on('click', $scope.initLoad.bind(this));
        }
        else {
            $scope.initLoad();
        }
    });

});

/*
* SIMPLE301 API
* -----------------------------------------------------
* Resource to handle making requests to the backoffice API to handle CRUD operations
* for redirect management
*/
angular.module("umbraco.resources").factory("Simple301Api", function ($http) {
    return {
        //Get all redirects from the server
        getAll: function () {
            return $http.get("backoffice/Simple301/RedirectApi/GetAll");
        },
        //Send data to add a new redirect
        add: function (isRegex, oldUrl, newUrl, notes) {
            return $http.post("backoffice/Simple301/RedirectApi/Add", JSON.stringify({ isRegex: isRegex, oldUrl: oldUrl, newUrl: newUrl, notes: notes }));
        },
        //Send request to update an existing redirect
        update: function (redirect) {
            return $http.post("backoffice/Simple301/RedirectApi/Update", JSON.stringify({ redirect: redirect }));
        },
        //Remove / Delete an existing redirect
        remove: function (id) {
            return $http.delete("backoffice/Simple301/RedirectApi/Delete/" + id);
        }
    };
});
