import { Component, OnInit } from "@angular/core";
import { res, format } from "../services/resources";
import moment from "moment";
import { HttpClient } from "@angular/common/http";
import { checkXhr, config } from "../services/xhr";

moment.locale("it-IT");

interface IConfig {
    zones?: string[];
    programCycles?: IProgramCycle[];
    suspended: boolean;
}

interface IProgramCycle {
    name: string;
    startTime: string; // HH:mm:ss format
    disabled: boolean;
    minutes: number;
}

// interface INextCycle extends ICycle {
//     scheduledTime: string;
//     running: boolean;
// }

interface IGardenResponse {
    error?: string;
}

interface IGardenStatusResponse extends IGardenResponse {
    config: IConfig;
    status: number;
    //isRunning: boolean;
    // flowData: { 
    //     totalMc: number;
    //     flowLMin: number;
    // };
    //nextCycles: INextCycle[];
}

// interface IGardenStartStopResponse extends IGardenResponse {
//     error: string;
// }

// class ImmediateCycle {
//     constructor(zoneNames: string[], public time: string) {
//         this.zones = zoneNames.map((name, index) => ({ name, index }))
//     }

//     zones: { 
//         name: string;
//         enabled?: boolean;
//         index: number;
//     }[];
// }

@Component({
    selector: 'app-garden',
    templateUrl: './garden.html',
    styleUrls: ['./garden.css']
})
export class GardenComponent implements OnInit {
    public loaded!: boolean;
    public message!: string;
    public error!: string;
    private zoneNames: string[] = [];
//    public immediateCycle!: ImmediateCycle | null;
    public config!: IConfig;
    public status1!: string;
    public status2!: string;
    public flow!: { 
        totalMc: number;
        flowLMin: number;
    };
//    public nextCycles!: { name: string, scheduledTime: string, suspended: boolean, running: boolean }[];
//    public immediateStarted!: boolean;
    public canSuspend!: boolean;
    public canResume!: boolean;
    public editProgramMode!: boolean;
//    public isRunning!: boolean;
    // To anticipate login request at beginning of an operation flow
    private _hasPrivilege!: boolean;

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

    private async preCheckPrivilege(): Promise<void> {
        if (!this._hasPrivilege) {
            await checkXhr(this.http.get(config.baseUrl + "/svc/checkLogin", { responseType: "text" }));
            this._hasPrivilege = true;
        }
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
            // this.flow = resp.flowData;
            // this.isRunning = resp.isRunning;

            //let now = moment.now();
            // if (resp.nextCycles) {
            //     this.nextCycles = resp.nextCycles;
            //     this.nextCycles.forEach(cycle => {
            //         cycle.scheduledTime = cycle.scheduledTime && moment.duration(moment(cycle.scheduledTime).diff(now)).humanize(true)
            //     })
            // }

            this.config = resp.config || { };
            this.zoneNames = this.config.zones = this.config.zones || [];
            this.config.programCycles = this.config.programCycles || [];
            this.updateProgram();
        }, err => {
            this.error = format("Garden_ErrorConf", err.message);
        }).finally(() => {
            this.loaded = true;
        });
    }

    private updateProgram(): void {
        this.canResume = this.config.programCycles!.length > 0 && this.config.suspended;
        this.canSuspend = this.config.programCycles!.length > 0 && !this.config.suspended;
    }

    // public stop() {
    //     checkXhr(this.http.post<IGardenStartStopResponse>(config.baseUrl + "/svc/gardenStop", "")).then(() => {
    //         this.message = res["Garden_Stopped"];  
    //         this.immediateStarted = false;
    //     }, err => {
    //         this.error = format("Garden_StopError", err.message);
    //     });
    // }

    // public startImmediate() {
    //     var body = { zones: this.immediateCycle!.zones.filter(z => z.enabled).map(z => z.index), time: new Number(this.immediateCycle!.time) };
    //     checkXhr(this.http.post<IGardenStartStopResponse>(config.baseUrl + "/svc/gardenStart", body)).then(() => {
    //         this.message = res["Garden_StartedImmediate"];  
    //         this.immediateStarted = true;
    //         this.loadConfigAndStatus();
    //     }, err => {
    //         this.error = format("Garden_ImmediateError", err.message);
    //     });
    // }

    // public async addImmediateCycle() {
    //     await this.preCheckPrivilege();
    //     // Mutually exclusive
    //     this.clearProgram();
    //     this.immediateCycle = new ImmediateCycle(this.zoneNames, "5");
    // }

    // public clearImmediate(): void {
    //     this.immediateCycle = null;
    // }

    public resume(): void {
        this.config.suspended = false;
        this.saveProgram();
    }

    public suspend(): void {
        this.config.suspended = true;
        this.saveProgram();
    }

    public startEdit(): void {
        this.preCheckPrivilege().then(() => {
            this.editProgramMode = true;
            //this.clearImmediate();
        }, () => { });
    }

    public saveProgram(): Promise<void> {
        return checkXhr(this.http.put(config.baseUrl + "/svc/gardenCfg", this.config)).then(() => {
            this.loadConfigAndStatus();
        }, err => {
            this.error = format("Garden_ErrorSetConf", err.message);
        }).finally(() => {
            this.clearProgram();
        })
    }

    public clearProgram(): void {
        this.editProgramMode = false;
    }
}