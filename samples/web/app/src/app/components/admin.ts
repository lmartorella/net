import { HttpClient } from '@angular/common/http';
import { Component, ElementRef, ViewChild } from '@angular/core';
import { res } from '../services/resources';
import { checkXhr, config } from '../services/xhr';

@Component({
  selector: 'app-admin',
  templateUrl: './admin.html'
})
export class AdminComponent {
    @ViewChild('file') public fileElement!: ElementRef<HTMLInputElement>;
    public readonly res: { [key: string]: string };

    constructor(private readonly http: HttpClient) {
        this.res = res as unknown as { [key: string]: string };
    }

    public haltMain() {
        checkXhr<unknown>(this.http.get(config.baseUrl + '/svc/halt/server', { responseType: "text" })).then(data => {
            alert(data);
        }).catch(err => {
            alert(err.message);
        });
    }

    public startMain() {
        checkXhr<unknown>(this.http.get(config.baseUrl + '/svc/start/server', { responseType: "text" })).then(data => {
            alert(data);
        }).catch(err => {
            alert(err.message);
        });
    }

    public restartSolar() {
        checkXhr<unknown>(this.http.get(config.baseUrl + '/svc/restart/solar', { responseType: "text" })).then(data => {
            alert(data);
        }).catch(err => {
            alert(err.message);
        });
    }

    public restartGarden() {
        checkXhr<unknown>(this.http.get(config.baseUrl + '/svc/restart/garden', { responseType: "text" })).then(data => {
            alert(data);
        }).catch(err => {
            alert(err.message);
        });
    }

    public uploadGardenConfig() {
        var req = new XMLHttpRequest();
        req.open("PUT", "/svc/gardenCfg");
        req.setRequestHeader("Content-type", "application/octect-stream");
        req.onload = () => {
            alert('Done');
        };
        req.send(this.fileElement.nativeElement.files![0]);
      }
}