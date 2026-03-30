import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { MobilityService, RouteRequest, RouteResponse } from './mobility.service';

describe('MobilityService', () => {
  let service: MobilityService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), MobilityService],
    });

    service = TestBed.inject(MobilityService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getRoute sends POST to /api/mobility/route with the request body', () => {
    const request: RouteRequest = {
      origin: 'Montreal, QC',
      destination: 'Laval, QC',
      travelMode: 'car',
    };

    const mockResponse: RouteResponse = {
      success: true,
      distanceMeters: 15000,
      duration: '1200s',
      encodedPolyline: 'abcdef',
    };

    let actual: RouteResponse | undefined;
    service.getRoute(request).subscribe((res) => (actual = res));

    const req = httpTesting.expectOne('/api/mobility/route');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);

    req.flush(mockResponse);
    expect(actual).toEqual(mockResponse);
  });

  it('getRoute propagates HTTP errors', () => {
    const request: RouteRequest = {
      origin: 'A',
      destination: 'B',
      travelMode: 'bike',
    };

    let errorStatus: number | undefined;
    service.getRoute(request).subscribe({
      error: (err) => (errorStatus = err.status),
    });

    const req = httpTesting.expectOne('/api/mobility/route');
    req.flush({ success: false, message: 'Bad Request' }, { status: 400, statusText: 'Bad Request' });

    expect(errorStatus).toBe(400);
  });

  it('getMontrealAndLaval sends GET to /api/mobility/montreal-laval', () => {
    service.getMontrealAndLaval().subscribe();

    const req = httpTesting.expectOne('/api/mobility/montreal-laval');
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, count: 0, locations: [] });
  });
});
