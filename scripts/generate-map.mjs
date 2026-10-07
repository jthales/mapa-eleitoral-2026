import { readFile, writeFile, mkdir } from 'node:fs/promises';

// Deterministic equirectangular projection of locally preserved IBGE geometry.
const source = new URL('../frontend/public/maps/brazil-states.geojson', import.meta.url);
const geo = JSON.parse(await readFile(source, 'utf8'));
const ufs = {11:['RO','Rondônia'],12:['AC','Acre'],13:['AM','Amazonas'],14:['RR','Roraima'],15:['PA','Pará'],16:['AP','Amapá'],17:['TO','Tocantins'],21:['MA','Maranhão'],22:['PI','Piauí'],23:['CE','Ceará'],24:['RN','Rio Grande do Norte'],25:['PB','Paraíba'],26:['PE','Pernambuco'],27:['AL','Alagoas'],28:['SE','Sergipe'],29:['BA','Bahia'],31:['MG','Minas Gerais'],32:['ES','Espírito Santo'],33:['RJ','Rio de Janeiro'],35:['SP','São Paulo'],41:['PR','Paraná'],42:['SC','Santa Catarina'],43:['RS','Rio Grande do Sul'],50:['MS','Mato Grosso do Sul'],51:['MT','Mato Grosso'],52:['GO','Goiás'],53:['DF','Distrito Federal']};
const rings = feature => feature.geometry.type === 'Polygon' ? feature.geometry.coordinates : feature.geometry.coordinates.flat();
const points = geo.features.flatMap(f => rings(f).flat());
const xs = points.map(p => p[0]), ys = points.map(p => -p[1]);
const left = Math.min(...xs), top = Math.min(...ys);
const scale = Math.min(570/(Math.max(...xs)-left),540/(Math.max(...ys)-top));
const project = p => [+(25+(p[0]-left)*scale).toFixed(2),+(25+(-p[1]-top)*scale).toFixed(2)];
const center = ring => {
  let area=0, x=0, y=0;
  for(let i=0;i<ring.length-1;i++) { const a=ring[i],b=ring[i+1],cross=a[0]*b[1]-b[0]*a[1]; area+=cross; x+=(a[0]+b[0])*cross; y+=(a[1]+b[1])*cross; }
  return [x/(3*area),y/(3*area)];
};
const result = geo.features.map(f => {
  const [uf,name] = ufs[f.properties.codarea];
  const all = rings(f);
  const ring = [...all].sort((a,b)=>b.length-a.length)[0];
  const [x,y]=project(center(ring));
  return {uf,name,path:all.map(r=>r.map((p,i)=>(i?'L':'M')+project(p).join(',')).join('')+'Z').join(''),x,y};
});
if(result.length!==27 || new Set(result.map(s=>s.uf)).size!==27) throw new Error('Expected 27 unique UFs');
await mkdir(new URL('../frontend/public/maps/',import.meta.url),{recursive:true});
await writeFile(new URL('../frontend/public/maps/brazil-states.json',import.meta.url),JSON.stringify(result));
console.log('Generated 27 SVG state shapes from IBGE geometry.');
