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
  it('on touch screens a tap shows the state inline instead of opening its page', async () => {
    const original = globalThis.matchMedia;
    globalThis.matchMedia = ((query: string) => ({ matches: query === '(hover: none)' })) as never;
    try {
      const fixture = TestBed.createComponent(BrazilMapComponent);
      fixture.componentRef.setInput('national', { hasData: false, candidates: [] });
      fixture.componentRef.setInput('states', [
        { uf: 'SP', name: 'São Paulo', hasData: false, candidates: [] },
      ]);
      TestBed.inject(HttpTestingController)
        .expectOne('/maps/brazil-states.json')
        .flush([{ uf: 'SP', name: 'São Paulo', path: 'M0,0L10,0L10,10Z', x: 5, y: 5 }]);
      fixture.componentRef.setInput('status', 'loaded');
      await fixture.whenStable();
      const link: HTMLElement = fixture.nativeElement.querySelector('svg a');
      expect(link.getAttribute('href')).toBeNull();
      link.dispatchEvent(new MouseEvent('click', { bubbles: true }));
      link.dispatchEvent(new MouseEvent('mouseleave'));
      await fixture.whenStable();
      expect(fixture.nativeElement.querySelector('.map-info h3').textContent).toContain(
        'São Paulo',
      );
      document.body.dispatchEvent(new MouseEvent('click', { bubbles: true }));
      await fixture.whenStable();
      expect(fixture.nativeElement.querySelector('.map-info h3').textContent).toContain('Brasil');
    } finally {
      globalThis.matchMedia = original;
    }
  });
});
