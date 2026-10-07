import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ElectionService } from './election-service';

describe('ElectionService', () => {
  let http: HttpTestingController;
  let service: ElectionService;
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    service = TestBed.inject(ElectionService);
  });
  afterEach(() => http.verify());
  it('consults only our API for national and state data', () => {
    service.getElection().subscribe((result) => expect(result.source).toBe('mock'));
    http.expectOne('/api/election').flush({ source: 'mock' });
    service.getStates().subscribe((states) => expect(states.length).toBe(0));
    http.expectOne('/api/election/states').flush([]);
    service.getState('SP').subscribe();
    http.expectOne('/api/election/states/SP').flush({});
  });
  it('passes the selected round to every endpoint', () => {
    service.getElection(1).subscribe();
    http.expectOne('/api/election?round=1').flush({});
    service.getStates(1).subscribe();
    http.expectOne('/api/election/states?round=1').flush([]);
    service.getState('SP', 1).subscribe();
    http.expectOne('/api/election/states/SP?round=1').flush({});
  });
  it('propagates unavailability instead of fabricating empty election data', () => {
    let status = 0;
    service.getElection().subscribe({
      next: () => {
        throw new Error('unexpected result');
      },
      error: (e) => (status = e.status),
    });
    http
      .expectOne('/api/election')
      .flush({ title: 'Indisponível' }, { status: 503, statusText: 'Service Unavailable' });
    expect(status).toBe(503);
  });
});
