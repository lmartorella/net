/// <reference path="typings/angular.d.ts" />

module home.app {

    interface IScope extends ng.IScope {
        works: string;
    }

    angular.module('home', []).controller('test', ['$scope', '$http', ($scope: IScope, $http: ng.IHttpService) => {
        $scope.works = "Loading...";
        $http.post('/HomeService.asmx/GetTechnologyData', null).then(value => {
            var ret = (<any>value.data).d;
            $scope.works = "Loaded. nodes: " + ret.NodeCount + ", devices: " + ret.DeviceCount;
        }, err => {
            $scope.works = "Error: " + err;
        });
    }]);

}