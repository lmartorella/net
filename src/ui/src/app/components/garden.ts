import { Component, OnInit } from "@angular/core";
import { res, format } from "../services/resources";
import moment from "moment";
import { HttpClient } from "@angular/common/http";
import { checkXhr, config } from "../services/xhr";

moment.locale("it-IT");

interface IConfig {
    zones?: string[];
}

interface IGardenResponse {
    error?: string;
}

interface IGardenStatusResponse extends IGardenResponse {
    config: IConfig;
    status: number;
    isRunning: boolean;
}

@Component({
    selector: 'app-garden',
    templateUrl: './garden.html',
    styleUrls: ['./garden.css']
})
export class GardenComponent implements OnInit {
    public loaded!: boolean;
    public message!: string;
    public error!: string;
    public config!: IConfig;
    public status1!: string;
    public status2!: string;
    public isRunning!: boolean;

    public readonly res: { [key: string]: string };
    public readonly format: (str: string, args?: any) => string;

    constructor(private readonly http: HttpClient) {
        this.res = res as unknown as { [key: string]: string };
        this.format = format;
    }

    public ngOnInit() {
        this.status1 = res["Device_StatusLoading"];
        this.loaded = false;
        this.loadConfigAndStatus();
    }

    private loadConfigAndStatus() {
        // Fetch zones
        checkXhr(this.http.get<IGardenStatusResponse>(config.baseUrl + "/svc/gardenStatus")).then(resp => {
            switch (resp.status) {
                case 1: this.status1 = res["Device_StatusOnline"]; break;
                case 2: this.status1 = res["Device_StatusOffline"]; break;
                case 3: this.status1 = res["Device_StatusPartiallyOnline"]; break;
            }
            this.status2 = (!resp.config && res["Garden_MissingConf"]) || "";
            this.isRunning = resp.isRunning;

            this.config = resp.config || { };
        }, err => {
            this.error = format("Garden_ErrorConf", err.message);
        }).finally(() => {
            this.loaded = true;
        });
    }
}