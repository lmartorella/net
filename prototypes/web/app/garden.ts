interface IGardenCfgResponse {
    zones: string[];
}

interface IGardenStartStopResponse {
    error: string;
}

class GardenController {
    
    public message: string;
    public error: string;
    private zones: { name: string, time: number }[];
    
    constructor(private $http: ng.IHttpService, private $q: ng.IQService) {
        this.zones = [];
        //document.title = "Garden " + String.fromCharCode(0x1F33B);

        // Fetch zones
        this.$http.get<IGardenCfgResponse>("/r/gardenCfg").then(resp => {
            if (resp.status == 200) {
                this.zones = resp.data.zones.map(zone => ({ name: zone, time: 0 }));
            } else {
                this.error = "Cannot fetch cfg";
            }
        }, err => {
            this.error = "Cannot fetch cfg: " + err.statusText;
        });
    }

    stop() {
        this.$http.post<IGardenStartStopResponse>("/r/gardenStop", "").then(resp => {
            if (resp.status == 200) {
                if (resp.data.error) {
                    this.error = "ERROR: " + resp.data.error;
                } else {
                    this.message = "Fermato!";
                }
            } else {
                this.error = "Cannot stop garden!";
            }
        }, err => {
            this.error = "Cannot stop garden: " + err.statusText;
        });
    }

    start() {
        var body = this.zones.map(zone => new Number(zone.time));
        this.$http.post<IGardenStartStopResponse>("/r/gardenStart", JSON.stringify(body)).then(resp => {
            if (resp.status == 200) {
                if (resp.data.error) {
                    this.error = "ERROR: " + resp.data.error;
                } else {
                    this.message = "Avviato!";
                }
            } else {
                this.error = "Cannot start garden!";
            }
        }, err => {
            this.error = "Cannot start garden: " + err.statusText;
        });
    }
}

angular.module('solar', []).controller('gardenCtrl', ['$http', '$q', GardenController])

.service('authInterceptor', ['$q', function($q) {
    var service = this;
    service.responseError = function(response) {
        if (response.status === 401) {
            window.location.pathname = "/login";
        }
        return $q.reject(response);
    };
}])
.config(['$httpProvider', function($httpProvider) {
    $httpProvider.interceptors.push('authInterceptor');
}])