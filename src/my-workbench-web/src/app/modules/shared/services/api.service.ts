import { HttpClient, HttpHeaders, HttpParams, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, Observable, throwError } from 'rxjs';

/**
 * API Error Response model for structured error handling
 */
export interface ApiErrorResponse {
  statusCode: number;
  message: string;
  details?: string;
  errors?: Record<string, string[]>;
  timestamp?: string;
  path?: string;
  traceId?: string;
  isClientError: boolean;
  isServerError: boolean;
  isNetworkError: boolean;
}

/**
 * Base API Service class for all HTTP operations
 * 
 * Provides common methods for GET, POST, PUT, PATCH, and DELETE operations.
 * Can be extended by module-specific services to handle specialized endpoints.
 * 
 * Usage:
 * ```
 * export class MyApiService extends ApiService {
 *   constructor(http: HttpClient) {
 *     super(http);
 *   }
 *   
 *   getUsers() {
 *     return this.get<User[]>('/api/users');
 *   }
 * }
 * ```
 */
@Injectable({
  providedIn: 'root'
})
export class ApiService {
  protected readonly baseUrl: string = '';

  constructor(protected http: HttpClient) {}

  /**
   * Performs a GET request
   * @param endpoint The API endpoint (e.g., '/api/users')
   * @param params Optional query parameters
   * @returns Observable with the response of type T
   */
  protected get<T>(
    endpoint: string,
    params?: Record<string, string | number | boolean | null | undefined>
  ): Observable<T> {
    return this.http.get<T>(`${this.baseUrl}${endpoint}`, {
      headers: this.defaultHeaders(),
      params: this.buildParams(params)
    }).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Performs a POST request
   * @param endpoint The API endpoint (e.g., '/api/users')
   * @param body The request body
   * @param params Optional query parameters
   * @returns Observable with the response of type T
   */
  protected post<T>(
    endpoint: string,
    body: any,
    params?: Record<string, string | number | boolean | null | undefined>
  ): Observable<T> {
    return this.http.post<T>(`${this.baseUrl}${endpoint}`, body, {
      headers: this.defaultHeaders(),
      params: this.buildParams(params)
    }).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Performs a PUT request
   * @param endpoint The API endpoint (e.g., '/api/users/123')
   * @param body The request body
   * @param params Optional query parameters
   * @returns Observable with the response of type T
   */
  protected put<T>(
    endpoint: string,
    body: any,
    params?: Record<string, string | number | boolean | null | undefined>
  ): Observable<T> {
    return this.http.put<T>(`${this.baseUrl}${endpoint}`, body, {
      headers: this.defaultHeaders(),
      params: this.buildParams(params)
    }).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Performs a PATCH request
   * @param endpoint The API endpoint (e.g., '/api/users/123')
   * @param body The request body with partial updates
   * @param params Optional query parameters
   * @returns Observable with the response of type T
   */
  protected patch<T>(
    endpoint: string,
    body: any,
    params?: Record<string, string | number | boolean | null | undefined>
  ): Observable<T> {
    return this.http.patch<T>(`${this.baseUrl}${endpoint}`, body, {
      headers: this.defaultHeaders(),
      params: this.buildParams(params)
    }).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Performs a DELETE request
   * @param endpoint The API endpoint (e.g., '/api/users/123')
   * @param params Optional query parameters
   * @returns Observable with the response of type T
   */
  protected delete<T>(
    endpoint: string,
    params?: Record<string, string | number | boolean | null | undefined>
  ): Observable<T> {
    return this.http.delete<T>(`${this.baseUrl}${endpoint}`, {
      headers: this.defaultHeaders(),
      params: this.buildParams(params)
    }).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Performs a GET request for file downloads
   * @param endpoint The API endpoint
   * @param params Optional query parameters
   * @returns Observable with Blob response
   */
  protected getFile(
    endpoint: string,
    params?: Record<string, string | number | boolean | null | undefined>
  ): Observable<Blob> {
    return this.http.get(`${this.baseUrl}${endpoint}`, {
      responseType: 'blob',
      params: this.buildParams(params)
    }).pipe(
      catchError(this.handleError)
    );
  }

  /**
   * Performs a POST request for file uploads
   * @param endpoint The API endpoint
   * @param formData FormData containing files and other data
   * @param params Optional query parameters
   * @returns Observable with the response of type T
   */
  protected postFormData<T>(
    endpoint: string,
    formData: FormData,
    params?: Record<string, string | number | boolean | null | undefined>
  ): Observable<T> {
    return this.http.post<T>(`${this.baseUrl}${endpoint}`, formData, {
      params: this.buildParams(params)
    }).pipe(
      catchError(this.handleError)
    );
  }

  // ---------- Helper Methods ----------

  /**
   * Builds HttpParams from a record object
   * Filters out null and undefined values
   */
  protected buildParams(
    params?: Record<string, string | number | boolean | null | undefined>
  ): HttpParams | undefined {
    if (!params || Object.keys(params).length === 0) {
      return undefined;
    }

    let httpParams = new HttpParams();

    Object.entries(params).forEach(([key, value]) => {
      if (value !== null && value !== undefined) {
        httpParams = httpParams.set(key, String(value));
      }
    });

    return httpParams.keys().length > 0 ? httpParams : undefined;
  }

  /**
   * Returns default HTTP headers for requests
   */
  protected defaultHeaders(): HttpHeaders {
    return new HttpHeaders({
      'Content-Type': 'application/json'
    });
  }

  /**
   * Centralized error handling with detailed error logging and categorization
   * Captures and categorizes different error types for better debugging
   */
  protected handleError(error: any): Observable<never> {
    const apiError = this.parseApiError(error);
    this.logError(apiError);
    return throwError(() => apiError);
  }

  /**
   * Parses HTTP error response and creates a structured ApiErrorResponse
   * @param error The HTTP error response
   * @returns Structured ApiErrorResponse with categorized error information
   */
  private parseApiError(error: any): ApiErrorResponse {
    const errorResponse: ApiErrorResponse = {
      statusCode: 0,
      message: 'Unknown error occurred',
      isClientError: false,
      isServerError: false,
      isNetworkError: false
    };

    // Network error (no internet, CORS, etc.)
    if (error instanceof ErrorEvent) {
      errorResponse.isNetworkError = true;
      errorResponse.statusCode = 0;
      errorResponse.message = error.error?.message || 'Network error occurred';
      errorResponse.details = `Network communication failed: ${error.message}`;
      return errorResponse;
    }

    // HTTP error response
    if (error instanceof HttpErrorResponse) {
      errorResponse.statusCode = error.status;

      // Categorize error by status code
      if (error.status >= 400 && error.status < 500) {
        errorResponse.isClientError = true;
      } else if (error.status >= 500) {
        errorResponse.isServerError = true;
      } else if (error.status === 0) {
        // Status 0 typically means network error or CORS issue
        errorResponse.isNetworkError = true;
        errorResponse.message = 'Network error: Unable to connect to the server';
        errorResponse.details = 'Check your internet connection and CORS configuration';
        return errorResponse;
      }

      // Try to extract error details from response body
      if (error.error) {
        if (typeof error.error === 'string') {
          errorResponse.message = error.error;
        } else if (typeof error.error === 'object') {
          // Handle structured error responses from API
          errorResponse.message = error.error.message || error.error.title || error.statusText || 'API Error';
          errorResponse.details = error.error.detail || error.error.description;
          errorResponse.errors = error.error.errors; // Validation errors
          errorResponse.traceId = error.error.traceId;
          errorResponse.path = error.error.instance || error.url;
        }
      } else {
        // Fallback to HTTP status text
        errorResponse.message = error.statusText || `HTTP Error ${error.status}`;
      }

      errorResponse.timestamp = new Date().toISOString();
      if (!errorResponse.path && error.url) {
        errorResponse.path = error.url;
      }
    } else {
      // Unknown error type
      errorResponse.message = error.message || 'An unexpected error occurred';
      errorResponse.details = JSON.stringify(error);
    }

    return errorResponse;
  }

  /**
   * Logs structured error information for debugging
   * @param error The ApiErrorResponse to log
   */
  private logError(error: ApiErrorResponse): void {
    const timestamp = new Date().toISOString();
    const errorSummary = `[${timestamp}] API Error (${error.statusCode})`;

    // Log error with appropriate level
    const logData = {
      statusCode: error.statusCode,
      message: error.message,
      details: error.details,
      path: error.path,
      traceId: error.traceId,
      timestamp: error.timestamp,
      errorType: error.isNetworkError ? 'Network' : 
                 error.isClientError ? 'Client' : 
                 error.isServerError ? 'Server' : 
                 'Unknown'
    };

    // Log to console with grouping for better visibility
    if (error.isNetworkError) {
      console.error(`❌ ${errorSummary} - NETWORK ERROR`);
    } else if (error.isClientError) {
      console.warn(`⚠️ ${errorSummary} - CLIENT ERROR`);
    } else if (error.isServerError) {
      console.error(`❌ ${errorSummary} - SERVER ERROR`);
    }

    console.table(logData);

    // Log validation errors if present
    if (error.errors && Object.keys(error.errors).length > 0) {
      console.group('Validation Errors');
      Object.entries(error.errors).forEach(([field, messages]) => {
        console.error(`  ${field}:`, messages);
      });
      console.groupEnd();
    }

    // Log additional context
    if (error.details) {
      console.error('Details:', error.details);
    }

    // In production, you might want to send this to a logging service
    this.sendErrorToLoggingService(error);
  }

  /**
   * Sends error to a remote logging service (optional)
   * Override this method in derived classes or use a logging interceptor
   * @param error The ApiErrorResponse to send
   */
  protected sendErrorToLoggingService(error: ApiErrorResponse): void {
    // This can be overridden by services that need to send errors to a backend logging service
    // Example: this.http.post('/api/logs', error).subscribe();
  }
}
