class SolarController {
    constructor($http, $q) {
        this.$http = $http;
        this.$q = $q;

        this.$http.get('/r/imm').then(resp => {
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

    drawDays(count) {
        let el = document.getElementById('chart');
        for (let i = 0; i < el.childNodes.length; i++) {
            el.removeChild(el.childNodes[i]);
        }

        // Fetch the last 4 days
        var promises = [];
        for (let day = -count + 1; day <= 0; day++) {
            promises.push(this.$http.get('/r/powToday?day=' + day).then(resp => {
                if (resp.status == 200 && Array.isArray(resp.data) && resp.data.length) {
                    return {
                        x: resp.data.map(s => s.ts),
                        y: resp.data.map(s => s.value),
                        mode: 'lines',
                        name: 'T' + (day === 0 ? '' : day.toString()),
                        type: 'scatter'
                    };
                }
            }));
        }

        function sortCat(series) {
            var ret = series.reduce((times, serie) => {
                return serie.x.reduce((times, v) => {
                    times[v] = true;
                    return times;             
                }, times);
            }, { });
            return Object.getOwnPropertyNames(ret).sort();
        }

        this.$q.all(promises).then(res => {
            var series = res.filter(r => !!r);
            Plotly.newPlot(el, series, {
                xaxis: {
                    categoryarray: sortCat(series)
                }
            });
        });
    }

    goGarden() {
        window.location.pathname = "/app/garden.html";
    }
}

angular.module('solar', []).controller('solarCtrl', ['$http', '$q', SolarController])

.service('authInterceptor', ['$q', function($q) {
    var service = this;
    service.responseError = function(response) {
        if (response.status === 401) {
            window.location = "/login";
        }
        return $q.reject(response);
    };
}])
.config(['$httpProvider', function($httpProvider) {
    $httpProvider.interceptors.push('authInterceptor');
}])