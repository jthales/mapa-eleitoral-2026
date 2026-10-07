import { TestBed } from '@angular/core/testing';
import { registerLocaleData } from '@angular/common';
import pt from '@angular/common/locales/pt';
import { CandidateResultCardComponent } from './candidate-result-card';
import { candidateColors } from '../core/models/election';

registerLocaleData(pt);
describe('CandidateResultCardComponent', () => {
  const candidate = {
    id: 'a',
    number: '90',
    name: 'Candidato A',
    party: 'Fictício A',
    votes: 123456,
    percentage: 51.28,
    officialStatus: null,
  };
  it('formats votes and percent in pt-BR without declaring a winner', async () => {
    const fixture = TestBed.createComponent(CandidateResultCardComponent);
    fixture.componentRef.setInput('candidate', candidate);
    await fixture.whenStable();
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('123.456 votos');
    expect(text).toContain('51,28');
    expect(text).not.toMatch(/vencedor|eleito/i);
    expect(fixture.nativeElement.querySelector('.official-status')).toBeNull();
  });
  it('shows the photo and falls back to an initial on failure', async () => {
    const fixture = TestBed.createComponent(CandidateResultCardComponent);
    fixture.componentRef.setInput('candidate', {
      ...candidate,
      photoUrl: '/api/election/candidates/a/photo?round=1',
    });
    await fixture.whenStable();
    const img = fixture.nativeElement.querySelector('img');
    expect(img.getAttribute('src')).toBe('/api/election/candidates/a/photo?round=1');
    img.dispatchEvent(new Event('error'));
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('img')).toBeNull();
    expect(fixture.nativeElement.querySelector('.candidate-avatar').textContent.trim()).toBe('C');
    fixture.componentRef.setInput('candidate', {
      ...candidate,
      photoUrl: '/api/election/candidates/b/photo?round=1',
    });
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('img')).not.toBeNull();
  });
  it('shows a waiting message without fabricated votes for a candidate-only preview', async () => {
    const fixture = TestBed.createComponent(CandidateResultCardComponent);
    fixture.componentRef.setInput('candidate', {
      ...candidate,
      votes: null,
      percentage: null,
      officialStatus: '2º turno',
    });
    fixture.componentRef.setInput('firstRoundSummary', true);
    await fixture.whenStable();
    expect(fixture.nativeElement.textContent).toContain('Aguardando apuração do 2º turno');
    expect(fixture.nativeElement.querySelector('.candidate-result')).toBeNull();
    expect(fixture.nativeElement.querySelector('.candidate-track')).toBeNull();
    expect(fixture.nativeElement.querySelector('.runoff-badge')).not.toBeNull();
  });
  it('keeps a candidate color stable when vote order changes', () => {
    const other = { ...candidate, id: 'b', name: 'Candidato B' };
    expect(candidateColors([candidate, other])).toEqual(candidateColors([other, candidate]));
  });
  it('uses official status for the runoff badge and muted appearance, never vote share', async () => {
    const fixture = TestBed.createComponent(CandidateResultCardComponent);
    fixture.componentRef.setInput('firstRoundSummary', true);
    fixture.componentRef.setInput('candidate', { ...candidate, percentage: 90 });
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('.runoff-badge')).toBeNull();
    expect(fixture.nativeElement.querySelector('.candidate-muted')).toBeNull();
    fixture.componentRef.setInput('candidate', { ...candidate, officialStatus: '2º turno' });
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('.runoff-badge').textContent).toContain('2º turno');
    fixture.componentRef.setInput('candidate', { ...candidate, officialStatus: 'Não eleito' });
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('.runoff-badge')).toBeNull();
    expect(fixture.nativeElement.querySelector('.candidate-muted')).not.toBeNull();
    fixture.componentRef.setInput('firstRoundSummary', false);
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('.candidate-muted')).toBeNull();
  });
});
