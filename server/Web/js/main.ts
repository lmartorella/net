/// <reference path="typings/angular.d.ts" />

module home.app {

    interface IScope extends ng.IScope {
        works: string;
    }

    angular.module('home', []).controller('test', ['$scope', '$http', ($scope: IScope, $http: ng.IHttpService) => {
        $scope.works = "Loading...";
        $http.post('/HomeService.asmx/GetData', null).then(value => {
            $scope.works = "Loaded: " + (<any>value.data).d.Ret;
        });
    }]);

}