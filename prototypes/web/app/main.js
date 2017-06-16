class SolarController {
    constructor($http) {
        this.$http = $http;

        $http.get('/r/imm').then(resp => {
            if (resp.status == 200) {
                this.pvData = resp.data;
                if (this.pvData.error) {
                    this.firstLine = `ERRORE: ${this.pvData.error}`;
                    this.firstLineClass = 'err';
                } else {
                    switch (this.pvData.mode) {
                        case undefined:
                        case 0:
                            this.firstLine = 'OFF';
                            this.firstLineClass = 'gray';
                            break;
                        case 1:
                            this.firstLine = `Potenza: ${this.pvData.currentW}W`;
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
                }
            } else {
                this.firstLine = 'HTTP ERROR: ' + resp.statusText;
                this.firstLineClass = 'err';
            }
        });
    }

    drawChart() {
        this.$http.get('/r/powToday').then(resp => {
            if (resp.status == 200 && Array.isArray(resp.data)) {
                Plotly.plot(document.getElementById('chart-today'), {
                    data: [{
                        x: resp.data.map(s => s.ts),
                        y: resp.data.map(s => s.value),
                        mode: 'lines',
                        name: 'Potenza',
                        type: 'scatter'
                    }]
                })
            }
        });
    }
}

angular.module('solar', []).controller('solarCtrl', ['$http', SolarController]);
