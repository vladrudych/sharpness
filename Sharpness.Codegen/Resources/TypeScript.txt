import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

class ApiBuilder<TResult> {

    constructor(private http: HttpClient, private url: string, private method: string) { }

    private body: any;
    private queryParams: { [key: string]: string };
    private headerParams: { [key: string]: string };
    private formParams: { [key: string]: string };
    private fileParams: { [key: string]: Blob };

    public addQueryParam(key: string, value: any): ApiBuilder<TResult> {
        (this.queryParams || (this.queryParams = {}))[key] = String(value);
        return this;
    }

    public addFormParam(key: string, value: any): ApiBuilder<TResult> {
        (this.formParams || (this.formParams = {}))[key] = String(value);
        return this;
    }

    public addHeaderParam(key: string, value: any): ApiBuilder<TResult> {
        (this.headerParams || (this.headerParams = {}))[key] = String(value);
        return this;
    }

    public addFileParam(key: string, file: Blob): ApiBuilder<TResult> {
        (this.fileParams || (this.fileParams = {}))[key] = file;
        return this;
    }

    public addBodyParam(body: any): ApiBuilder<TResult> {
        this.body = body;
        return this;
    }

    public build(): Observable<TResult> {

        const options = { headers: new HttpHeaders(), params: new HttpParams() };

        options.headers.append('Accept', 'application/json');

        if (this.queryParams) {
            for (const prop of Object.keys(this.queryParams)) {
                options.params.append(prop, this.queryParams[prop]);
            }
        }

        if (['POST', 'PUT'].includes(this.method)) {

            let body = null;

            if (this.body) {
                body = this.body;
                options.headers.append('Content-Type', 'application/json');
            } else if (this.formParams || this.fileParams) {
                options.headers.append('Content-Type', 'multipart/form-data');
                if (this.formParams) {
                    body = new FormData();
                    for (const prop of Object.keys(this.formParams)) {
                        body.append(prop, this.formParams[prop]);
                    }
                }
                if (this.fileParams) {
                    body = body || new FormData();
                    for (const prop of Object.keys(this.fileParams)) {
                        body.append(prop, this.fileParams[prop]);
                    }
                }
            }

            switch (this.method) {
                case 'PUT':
                    return this.http.put<TResult>(this.url, body, options);
                default:
                    return this.http.post<TResult>(this.url, body, options);
            }
        }

        switch (this.method) {
            case 'DELETE':
                return this.http.delete<TResult>(this.url, options);
            default:
                return this.http.get<TResult>(this.url, options);
        }

    }

}

@Injectable()
export class ApiService {

    constructor(private http: HttpClient) { }

    public request<TResult>(url: string, method: string): ApiBuilder<TResult> {
        return new ApiBuilder<TResult>(this.http, url, method);
    }

}
