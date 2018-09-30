import { SolarController } from "./solar";
import { GardenController } from "./garden";
import * as angular from "angular";

angular.module('home', [])
.service('authInterceptor', ['$q', function($q) {
    this.responseError = (response) => {
        if (response.status === 401) {
            // External login
            window.location.replace("/app/login.html?redirect=/app/index.html#/");
        }
        return $q.reject(response);
    };
}])
.config(['$httpProvider', function($httpProvider) {
    $httpProvider.interceptors.push('authInterceptor');
}])
.controller('solarCtrl', ['$http', '$q', SolarController])
.controller('gardenCtrl', ['$http', '$q', GardenController]);



