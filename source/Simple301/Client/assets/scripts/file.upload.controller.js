// This example makes use of the ng-file-upload directive by Danial Farid https://github.com/danialfarid/ng-file-upload
// It needs to be injected into the Angular app.
// You could probably use the native HTML5 file upload API if you didn't want to rely on any external directives.

angular.module("umbraco").controller('FileUploadController', function ($scope, $rootScope, fileApiService) {

    /*-------------------------------------------------------------------
     * Initialization Methods
     * ------------------------------------------------------------------*/

    /**
     * @ngdoc method
     * @name init
     * @function
     * 
     * @description - Called when the $scope is initalized.
     */
    $scope.init = function () {
        $scope.setVariables();
    };

    /**
     * @ngdoc method
     * @name setVariables
     * @function
     * 
     * @description - Sets the initial states of the $scope variables.
     */
    $scope.setVariables = function () {
        $scope.file = false;
        $scope.isUploading = false;
    };

    /*-------------------------------------------------------------------
     * Event Handler Methods
     *-------------------------------------------------------------------*/

    /**
     * @ngdoc method
     * @name acceptSelectedFile
     * @function
     * 
     * @param {array of file} files - One or more files selected by the HTML5 File Upload API.
     * @description - Get the file selected and store it in scope. This current example restricts the upload to a single file, so only take the first.
     */
    $scope.acceptSelectedFile = function (files) {
        if (files.length > 0) {
            $scope.file = files[0];
        }
    };

    /*-------------------------------------------------------------------
     * Helper Methods
     * ------------------------------------------------------------------*/

    /**
     * @ngdoc method
     * @name uploadFile
     * @function
     * 
     * @description - Uploads a file to the backend.
     */
    $scope.uploadFile = function () {
        if (!$scope.isUploading) {
            if ($scope.file) {
                $scope.isUploading = true;
                var promise = fileApiService.uploadFileToServer($scope.file);
                promise.then(function (response) {
                    if (response) {
                        $scope.feedback = {
                            success: true,
                            message: response.Message,
                            failedRedirects: response.FailedRedirects
                        }

                        // broadcast an event which the main Simple301 controller listens for
                        $rootScope.$emit('reloadTable');

                        console.info('Bulk import success: ' + response.Message);
                    }
                    $scope.isUploading = false;
                }, function (reason) {
                    $scope.feedback = {
                        error: true,
                        message: reason.message
                    }
                    console.info("Bulk import failed. " + reason.message);
                    $scope.isUploading = false;
                });
            } else {
                $scope.feedback = {
                    error: true,
                    message: "Please select a file to import."
                }
                console.info("Must select a file to import.");
                $scope.isUploading = false;
            }
        }
    };

    /*
    * Clears the feedback
    */
    $scope.clearFeedback = function () {
        $scope.feedback = null;
    }


    $scope.init();

});

angular.module("umbraco.resources").factory("fileApiService", function ($http) {

    var fileApiFactory = {};

    /**
     * @ngdoc method
     * @name importFile
     * @function
     * 
     * @param {file} file - File object acquired via File Upload API.
     * @description - Upload a file to the server.
     */
    fileApiFactory.uploadFileToServer = function (file) {
        var request = {
            file: file
        };
        return $http({
            method: 'POST',
            url: "backoffice/Simple301/FileUploadApi/UploadFileToServer",
            // If using Angular version <1.3, use Content-Type: false.
            // Otherwise, use Content-Type: undefined
            headers: { 'Content-Type': undefined },
            transformRequest: function (data) {
                var formData = new FormData();
                formData.append("file", data.file);
                return formData;
            },
            data: request
        }).then(function (response) {
            if (response) {
                return response.data;
            } else {
                return false;
            }
        });
    };

    return fileApiFactory;

});