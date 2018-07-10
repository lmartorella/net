interface IGardenStatusResponse {
    config: {
        zones: string[];
    };
    online: boolean;
    configured: boolean;
    flowData: { 
        totalMc: number;
        flowLMin: number;
    }
}

interface IGardenStartStopResponse {
    error: string;
}

class GardenController {
    
    public message: string;
    public error: string;
    private zones: { name: string, time: number }[];
    private status: string;
    public disableButton = true;
    public flow: { 
        totalMc: number;
        flowLMin: number;
    };

    constructor(private $http: ng.IHttpService, private $q: ng.IQService) {
        this.zones = [];
    
        // Fetch zones
        this.$http.get<IGardenStatusResponse>("/r/gardenStatus").then(resp => {
            if (resp.status == 200 && resp.data) {
                this.zones = resp.data.config && resp.data.config.zones.map(zone => ({ name: zone, time: 0 }));
                this.status =  resp.data.online ? 'Online' : (resp.data.config ? 'OFFLINE' : 'NOT CONFIGURED');
                this.flow = resp.data.flowData;
                this.disableButton = false;
            } else {
                this.error = "Cannot fetch cfg";
            }
        }, err => {
            this.error = "Cannot fetch cfg: " + err.statusText;
        });
    }

    stop() {
        this.disableButton = true;
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
        this.disableButton = true;
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
            window.location.replace("/app/login.html?redirect=/app/garden.html");
        }
        return $q.reject(response);
    };
}])
.config(['$httpProvider', function($httpProvider) {
    $httpProvider.interceptors.push('authInterceptor');
}])