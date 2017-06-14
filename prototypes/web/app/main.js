class SolarController {
    constructor($http) {
        $http.get('/data').then(resp => {
            if (resp.status == 200) {
            this.pvData = resp.data.data;
            switch (this.pvData.mode) {
                case undefined:
                case 0:
                    this.firstLine = 'OFF';
                    this.firstLineClass = 'gray';
                    break;
                case 1:
                    this.firstLine = `Potenza attuale: ${this.pvData.currentW}W`;
                    break;
                case 2:
                    this.firstLine = `ERRORE: ${this.pvData.fault}`;
                    this.firstLineClass = 'err';
                    break;
                default:
                    this.firstLine = `Errore: modalità sconosciuta: ${this.pvData.mode}`;
                    this.firstLineClass = 'unknown';
                    break;
                }
            } else {
                this.firstLine = 'HTTP ERROR: ' + resp.statusText;
                this.firstLineClass = 'err';
            }
        });
    }
}
angular.module('solar', []).controller('solarCtrl', ['$http', SolarController]);
