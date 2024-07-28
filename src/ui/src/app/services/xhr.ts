import { HttpErrorResponse } from "@angular/common/http";
import { Observable, catchError, of } from "rxjs";
import { Injectable } from "@angular/core";

export interface IModuleConfig {
    baseUrl?: string;
}

export const config: IModuleConfig = {
    baseUrl: ""
};

export const checkXhr = <T>(observable: Observable<T>): Promise<T> => {
    return new Promise((resolve, reject) => {
        observable.pipe(catchError((err: HttpErrorResponse) => {
            const header = err.statusText;
            const body = err.error;
            if (header !== body) {
                reject(new Error(`${header || ''}: ${body || ''}`))
            } else {
                reject(new Error(header || body));
            }
            return of({ } as T);
        })).subscribe(data => {
            if ((data as T & { error?: string })?.error) {
                reject(new Error((data as T & { error?: string }).error));
            } else if (!data) {
                reject(new Error("Server down"));
            } else {
                resolve(data);
            }
        });
    });
};


@Injectable({ 
    providedIn: 'root'
})
export class XhrService {
    public get baseUrl(): string {
        return config.baseUrl || "";
    }

    public check<T>(observable: Observable<T>): Promise<T> {
        return checkXhr(observable);
    };
}
