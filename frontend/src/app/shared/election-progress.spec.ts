import { TestBed } from '@angular/core/testing';
import { registerLocaleData } from '@angular/common';
import pt from '@angular/common/locales/pt';
import { ElectionProgressComponent } from './election-progress';

registerLocaleData(pt);
describe('ElectionProgressComponent', () => {
  it('exposes progress and section counts accessibly', async () => {
    const fixture = TestBed.createComponent(ElectionProgressComponent);
    fixture.componentRef.setInput('percentage', 89.36);
    fixture.componentRef.setInput('processed', 8936);
    fixture.componentRef.setInput('total', 10000);
    await fixture.whenStable();
    expect(
      fixture.nativeElement.querySelector('[role="progressbar"]').getAttribute('aria-valuenow'),
    ).toBe('89.36');
    expect(fixture.nativeElement.textContent).toContain('8.936 de 10.000');
  });
  it('renders an empty result without NaN or fictitious progress', async () => {
    const fixture = TestBed.createComponent(ElectionProgressComponent);
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('Aguardando dados das seções');
    expect(fixture.nativeElement.textContent).not.toContain('NaN');
  });
});
