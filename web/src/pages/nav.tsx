import { useEffect, useLayoutEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';

export type Route = 'sim' | 'assets' | 'systems' | 'logic' | 'trades';

export const PAGES: Array<{ route: Route; hash: string; label: string; blurb: string }> = [
  { route: 'sim', hash: '#/', label: 'Simulation', blurb: '3D timelapse + live telemetry' },
  { route: 'assets', hash: '#/assets', label: 'Assets', blurb: 'Everything landed on the surface' },
  { route: 'systems', hash: '#/systems', label: 'Systems Map', blurb: 'How the subsystems connect' },
  { route: 'logic', hash: '#/logic', label: 'Control Logic', blurb: 'Every rule & threshold, observable' },
  { route: 'trades', hash: '#/trades', label: 'Trade Studies', blurb: 'Key decisions + recommendations' },
];

export function routeFromHash(): Route {
  const h = location.hash.replace(/^#\/?/, '');
  const found = PAGES.find((p) => p.route === h);
  return found ? found.route : 'sim';
}

export function useRoute(): Route {
  const [route, setRoute] = useState<Route>(routeFromHash());
  useEffect(() => {
    const on = () => setRoute(routeFromHash());
    window.addEventListener('hashchange', on);
    return () => window.removeEventListener('hashchange', on);
  }, []);
  return route;
}

export function navigate(route: Route): void {
  const p = PAGES.find((x) => x.route === route)!;
  location.hash = p.hash;
}

/** Compact page launcher used in the sim top bar (opens a small menu). */
export function PagesMenu({ compact }: { compact?: boolean }) {
  const [open, setOpen] = useState(false);
  const btnRef = useRef<HTMLButtonElement>(null);
  const [pos, setPos] = useState<{ top: number; right: number }>({ top: 0, right: 0 });

  // Anchor the popup to the button in viewport coordinates. The popup is rendered in a
  // portal on document.body so it escapes the top bar's backdrop-filter stacking context
  // (which otherwise traps a position:fixed child behind the bottom sheet on mobile).
  useLayoutEffect(() => {
    if (!open || !btnRef.current) return;
    const r = btnRef.current.getBoundingClientRect();
    setPos({ top: Math.round(r.bottom + 6), right: Math.round(window.innerWidth - r.right) });
  }, [open]);

  return (
    <div className="pages-menu">
      <button ref={btnRef} className={`ghost${compact ? ' m-icon' : ''}`} onClick={() => setOpen((v) => !v)} title="Pages">
        {compact ? '▤' : '▤ PAGES'}
      </button>
      {open &&
        createPortal(
          <>
            <div className="pages-scrim" onClick={() => setOpen(false)} />
            <div className="panel pages-pop" style={{ top: pos.top, right: pos.right }}>
              <div className="section-header">Pages</div>
              {PAGES.map((p) => (
                <button
                  key={p.route}
                  className={routeFromHash() === p.route ? 'current' : ''}
                  onClick={() => {
                    navigate(p.route);
                    setOpen(false);
                  }}
                >
                  {p.label}
                  <div className="desc">{p.blurb}</div>
                </button>
              ))}
            </div>
          </>,
          document.body,
        )}
    </div>
  );
}

/** Top nav bar for the document-style pages. */
export function DocNav({ route }: { route: Route }) {
  return (
    <div className="doc-nav">
      <button className="brand-link" onClick={() => navigate('sim')}>
        <span className="mark" />
        <span className="title">MARS HABITAT SIM</span>
      </button>
      <div className="doc-nav-links">
        {PAGES.map((p) => (
          <button key={p.route} className={route === p.route ? 'active' : ''} onClick={() => navigate(p.route)}>
            {p.label}
          </button>
        ))}
      </div>
    </div>
  );
}
