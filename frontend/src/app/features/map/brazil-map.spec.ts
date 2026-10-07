import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { BrazilMapComponent } from './brazil-map';

describe('BrazilMapComponent', () => {
  beforeEach(() =>
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }),
  );
  it('keeps a state without votes neutral and linked to its detail page', async () => {
    const fixture = TestBed.createComponent(BrazilMapComponent);
    fixture.componentRef.setInput('status', 'no-data');
    TestBed.inject(HttpTestingController)
      .expectOne('/maps/brazil-states.json')
      .flush([{ uf: 'SP', name: 'São Paulo', path: 'M0,0L10,0L10,10Z', x: 5, y: 5 }]);
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('path').getAttribute('fill')).toBe('#dce2e0');
    expect(fixture.nativeElement.querySelector('svg a').getAttribute('href')).toBe('/states/SP');
    expect(fixture.nativeElement.textContent).toContain('Aguardando dados eleitorais');
  });
  it('offers the state list as fallback if geometry fails to load', async () => {
    const fixture = TestBed.createComponent(BrazilMapComponent);
    TestBed.inject(HttpTestingController)
      .expectOne('/maps/brazil-states.json')
      .flush('', { status: 500, statusText: 'Error' });
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain(
      'lista abaixo',
    );
  });
});
