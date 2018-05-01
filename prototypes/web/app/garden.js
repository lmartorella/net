class GardenController {
    constructor($http, $q) {
        this.$http = $http;
        this.$q = $q;
        this.zones = [];
        document.title = "Garden " + String.fromCharCode(0x1F33B);

        // Fetch zones
        this.$http.get("/r/gardenCfg").then(resp => {
            if (resp.status == 200) {
                this.zones = resp.data.zones.map(zone => ({ name: zone, time: 0 }));
            } else {
                this.error = "Cannot fetch cfg";
            }
        }, err => {
            this.error = "Cannot fetch cfg: " + err.statusText;
        });
    }

    start() {
        console.log(JSON.stringify(this.zones));
        var body = this.zones.map(zone => new Number(zone.time));
        this.$http.post("/r/gardenStart", JSON.stringify(body)).then(resp => {
            if (resp.status == 200) {
                this.message = "Avviato!";
            } else {
                this.error = "Cannot start garden!";
            }
        }, err => {
            this.error = "Cannot start garden: " + err.statusText;
        });
    }
}

angular.module('solar', []).controller('gardenCtrl', ['$http', '$q', GardenController]);
